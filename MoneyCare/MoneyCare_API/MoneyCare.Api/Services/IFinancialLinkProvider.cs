// Giao diện cung cấp các liên kết hoặc nguồn dữ liệu tài chính.
using MoneyCare.Models;

namespace MoneyCare.Services;

/// <summary>
/// Kết quả sau khi người dùng hoàn tất xác thực OAuth với ngân hàng/ví điện tử.
/// </summary>
public class KetQuaXacThucLienKet
{
    public bool ThanhCong { get; set; }
    public string? ThongBaoLoi { get; set; }

    // Thông tin tài khoản lấy được từ nhà cung cấp sau khi xác thực thành công
    public string? SoTaiKhoanDayDu { get; set; }
    public string? TenChuTaiKhoan { get; set; }

    // Token dùng để gọi API lấy sao kê sau này (cần MÃ HÓA trước khi lưu vào DB trong thực tế)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenHetHan { get; set; }
}

/// <summary>
/// Một giao dịch lấy được từ sao kê của ngân hàng/ví điện tử, chưa được lưu vào hệ thống.
/// </summary>
public class GiaoDichDongBo
{
    public string MaGiaoDichNgoai { get; set; } = string.Empty; // Mã giao dịch phía ngân hàng/ví, dùng để chống trùng lặp
    public decimal SoTien { get; set; }
    public LoaiGiaoDich Loai { get; set; }
    public DateTime ThoiGian { get; set; }
    public string? MoTa { get; set; }
}

/// <summary>
/// "Hợp đồng" chuẩn mà MỌI nhà cung cấp (Ngân hàng, MoMo, ZaloPay...) phải tuân theo.
/// Đây là điểm mở rộng duy nhất bạn cần implement khi có API Key chính thức —
/// phần còn lại của ứng dụng (IntegrationsController, giao diện, lưu DB...) đã dựng sẵn
/// và sẽ tự động hoạt động với bất kỳ provider nào implement đúng interface này.
/// </summary>
public interface IFinancialLinkProvider
{
    /// <summary>Nhà cung cấp mà class này phụ trách xử lý.</summary>
    NhaCungCap NhaCungCap { get; }

    /// <summary>
    /// Sinh URL điều hướng người dùng sang trang xác thực OAuth2 của ngân hàng/ví.
    /// VD thực tế: MoMo Business API, ZaloPay Merchant OAuth, hoặc chuẩn Open Banking của ngân hàng.
    /// </summary>
    Task<string> LayUrlXacThucAsync(string userId, string urlCallback);

    /// <summary>
    /// Xử lý callback trả về sau khi người dùng xác thực xong bên phía ngân hàng/ví
    /// (thường nhận "code" qua query string) để đổi lấy access token.
    /// </summary>
    Task<KetQuaXacThucLienKet> XuLyCallbackAsync(string code, string? state);

    /// <summary>
    /// Gọi API chính thức để lấy các giao dịch mới phát sinh kể từ lần đồng bộ trước.
    /// </summary>
    Task<List<GiaoDichDongBo>> LayGiaoDichMoiAsync(FinancialLink lienKet, DateTime tuThoiGian);

    /// <summary>Thu hồi quyền truy cập khi người dùng hủy liên kết.</summary>
    Task HuyQuyenTruyCapAsync(FinancialLink lienKet);
}

/// <summary>
/// Điều phối: dựa vào NhaCungCap để chọn đúng provider tương ứng.
/// Đăng ký các provider trong Program.cs, ví dụ:
///   builder.Services.AddScoped&lt;IFinancialLinkProvider, NganHangProviderStub&gt;();
///   builder.Services.AddScoped&lt;IFinancialLinkProvider, MoMoProviderStub&gt;();
///   builder.Services.AddScoped&lt;IFinancialLinkProvider, ZaloPayProvider&gt;();
///   builder.Services.AddScoped&lt;FinancialLinkProviderFactory&gt;();
/// </summary>
public class FinancialLinkProviderFactory
{
    private readonly IEnumerable<IFinancialLinkProvider> _providers;

    public FinancialLinkProviderFactory(IEnumerable<IFinancialLinkProvider> providers)
    {
        _providers = providers;
    }

    public IFinancialLinkProvider LayProvider(NhaCungCap nhaCungCap)
    {
        var provider = _providers.FirstOrDefault(p => p.NhaCungCap == nhaCungCap);
        if (provider == null)
        {
            throw new NotSupportedException($"Chưa có provider nào xử lý nhà cung cấp: {nhaCungCap}");
        }
        return provider;
    }
}

// ================================================================================
// CÁC PROVIDER STUB DƯỚI ĐÂY LÀ MẪU (CHƯA GỌI API THẬT).
// Khi có API Key/hợp đồng đối tác chính thức, thay phần throw NotImplementedException
// bằng lệnh gọi HttpClient/SDK chính thức của từng nhà cung cấp.
// ================================================================================

/// <summary>
/// STUB cho kết nối Open Banking API của ngân hàng.
/// Thực tế: mỗi ngân hàng có API riêng (VD: Open Banking chuẩn do NAPAS/Ngân hàng Nhà nước quy định,
/// hoặc API riêng của từng ngân hàng như VCB, MB, Techcombank...). Cần đăng ký làm đối tác kỹ thuật.
/// </summary>
public class NganHangProviderStub : IFinancialLinkProvider
{
    public NhaCungCap NhaCungCap => NhaCungCap.NganHang;

    public Task<string> LayUrlXacThucAsync(string userId, string urlCallback)
    {
        // TODO: thay bằng URL OAuth2 thật của Open Banking API, ví dụ:
        // return $"https://openbanking.example-bank.vn/oauth/authorize?client_id={ClientId}" +
        //        $"&redirect_uri={Uri.EscapeDataString(urlCallback)}&state={userId}&scope=accounts,transactions";
        throw new NotImplementedException(
            "Chưa cấu hình API Open Banking thật. Cần ClientId/ClientSecret được ngân hàng cấp sau khi ký hợp đồng đối tác.");
    }

    public Task<KetQuaXacThucLienKet> XuLyCallbackAsync(string code, string? state)
    {
        // TODO: gọi POST tới endpoint đổi "code" lấy access_token/refresh_token của ngân hàng
        throw new NotImplementedException("Chưa implement đổi authorization code lấy access token.");
    }

    public Task<List<GiaoDichDongBo>> LayGiaoDichMoiAsync(FinancialLink lienKet, DateTime tuThoiGian)
    {
        // TODO: gọi GET /accounts/{id}/transactions?from=... kèm Bearer access_token đã lưu (giải mã trước khi dùng)
        throw new NotImplementedException("Chưa implement lấy sao kê giao dịch từ Open Banking API.");
    }

    public Task HuyQuyenTruyCapAsync(FinancialLink lienKet)
    {
        // TODO: gọi API revoke token của ngân hàng (nếu có)
        return Task.CompletedTask;
    }
}

/// <summary>
/// STUB cho kết nối MoMo Business API.
/// Thực tế: cần đăng ký tài khoản MoMo for Business, được cấp Partner Code + Access Key + Secret Key,
/// tuân theo chuẩn ký chữ ký số (HMAC SHA256) cho từng request. Tài liệu: business.momo.vn
/// </summary>
public class MoMoProviderStub : IFinancialLinkProvider
{
    public NhaCungCap NhaCungCap => NhaCungCap.MoMo;

    public Task<string> LayUrlXacThucAsync(string userId, string urlCallback)
    {
        throw new NotImplementedException(
            "Chưa cấu hình MoMo Business API. Cần Partner Code/Access Key/Secret Key từ business.momo.vn");
    }

    public Task<KetQuaXacThucLienKet> XuLyCallbackAsync(string code, string? state)
    {
        throw new NotImplementedException("Chưa implement xác thực liên kết ví MoMo.");
    }

    public Task<List<GiaoDichDongBo>> LayGiaoDichMoiAsync(FinancialLink lienKet, DateTime tuThoiGian)
    {
        // TODO: gọi API truy vấn lịch sử giao dịch MoMo, ký request bằng HMAC SHA256 theo tài liệu MoMo
        throw new NotImplementedException("Chưa implement lấy lịch sử giao dịch từ MoMo API.");
    }

    public Task HuyQuyenTruyCapAsync(FinancialLink lienKet) => Task.CompletedTask;
}
