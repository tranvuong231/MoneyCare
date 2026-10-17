// Gọi API thật của ZaloPay (môi trường sandbox/UAT - xem appsettings.json mục "ZaloPay") để:
//  1) Tạo đơn hàng + sinh mã QR ZaloPay THẬT (dùng app ZaloPay Sandbox quét & thanh toán được).
//  2) Xác thực callback ZaloPay gọi về khi thanh toán thành công (kiểm tra chữ ký "mac").
//  3) Chủ động truy vấn trạng thái đơn hàng (polling) - dự phòng khi không nhận được callback
//     (VD: máy chạy backend không có URL public / chưa cấu hình ngrok).
//
// Tài liệu chính thức: https://docs.zalopay.vn (mục "Payment Gateway" / "Zalopay QR multi-function").
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Services;

/// <summary>Kết quả sau khi tạo đơn hàng ZaloPay thành công - đủ dữ liệu để hiển thị QR cho người dùng quét.</summary>
public class ZaloPayDonHangKetQua
{
    public string AppTransId { get; set; } = string.Empty;
    public string OrderUrl { get; set; } = string.Empty;

    // Chuỗi payload EMVCo (VietQR đa năng của ZaloPay) - dùng để tự vẽ ảnh QR (IQrPaymentService.TaoAnhTuChuoi)
    public string QrCode { get; set; } = string.Empty;
}

public interface IZaloPayService
{
    /// <summary>Tạo 1 đơn hàng ZaloPay UAT mới (dùng cho liên kết tài khoản HOẶC thanh toán Premium).</summary>
    Task<ZaloPayDonHangKetQua> TaoDonHangAsync(string userId, decimal soTien, string moTa, MucDichZaloPay mucDich, int? premiumOrderId = null);

    /// <summary>
    /// Xử lý dữ liệu callback ZaloPay Server POST tới (đã xác thực chữ ký "mac"). Trả về đúng JSON
    /// ZaloPay yêu cầu phản hồi lại (return_code=1 -> ZaloPay coi là đã nhận, không gọi lại nữa).
    /// </summary>
    Task<string> XuLyCallbackAsync(string requestBody);

    /// <summary>Chủ động hỏi ZaloPay trạng thái 1 đơn hàng (dùng khi Flutter/web polling chờ thanh toán).</summary>
    Task<ZaloPayOrder?> TruyVanTrangThaiAsync(string appTransId);
}

public class ZaloPayService : IZaloPayService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ZaloPayService> _logger;

    public ZaloPayService(
        HttpClient http,
        IConfiguration config,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<ZaloPayService> logger)
    {
        _http = http;
        _config = config;
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    private long AppId => _config.GetValue<long>("ZaloPay:AppId");
    private string Key1 => _config["ZaloPay:Key1"] ?? "";
    private string Key2 => _config["ZaloPay:Key2"] ?? "";
    private string CreateEndpoint => _config["ZaloPay:CreateEndpoint"] ?? "https://sb-openapi.zalopay.vn/v2/create";
    private string QueryEndpoint => _config["ZaloPay:QueryEndpoint"] ?? "https://sb-openapi.zalopay.vn/v2/query";
    private string? CallbackUrl => _config["ZaloPay:CallbackUrl"];

    public async Task<ZaloPayDonHangKetQua> TaoDonHangAsync(string userId, decimal soTien, string moTa, MucDichZaloPay mucDich, int? premiumOrderId = null)
    {
        var appTransId = SinhAppTransId();
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var appUser = "moneycare_" + userId[..Math.Min(10, userId.Length)];
        var amount = (long)soTien;
        const string item = "[]";
        var embedData = string.IsNullOrWhiteSpace(CallbackUrl) ? "{}" : $"{{\"redirecturl\":\"{CallbackUrl}\"}}";

        // Thứ tự bắt buộc theo tài liệu ZaloPay: appid|appuser|apptime KHÔNG - đúng chuẩn là:
        // app_id|app_trans_id|app_user|amount|app_time|embed_data|item
        var hmacInput = $"{AppId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}";
        var mac = TinhHmacSha256(Key1, hmacInput);

        var form = new Dictionary<string, string>
        {
            ["app_id"] = AppId.ToString(),
            ["app_trans_id"] = appTransId,
            ["app_user"] = appUser,
            ["app_time"] = appTime.ToString(),
            ["amount"] = amount.ToString(),
            ["item"] = item,
            ["embed_data"] = embedData,
            ["description"] = moTa,
            ["bank_code"] = "zalopayapp", // chỉ hiện QR ZaloPay (đúng yêu cầu "quét bằng ZaloPay UAT")
            ["mac"] = mac
        };
        if (!string.IsNullOrWhiteSpace(CallbackUrl))
        {
            form["callback_url"] = CallbackUrl;
        }

        using var res = await _http.PostAsync(CreateEndpoint, new FormUrlEncodedContent(form));
        var resJson = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resJson);
        var root = doc.RootElement;

        var returnCode = root.TryGetProperty("return_code", out var rc) ? rc.GetInt32() : 0;
        if (returnCode != 1)
        {
            var loiChiTiet = root.TryGetProperty("return_message", out var rm) ? rm.GetString() : "Không rõ lỗi";
            _logger.LogWarning("ZaloPay tạo đơn thất bại: {Loi} - phản hồi thô: {Raw}", loiChiTiet, resJson);
            throw new InvalidOperationException($"Không tạo được đơn hàng ZaloPay: {loiChiTiet}");
        }

        var orderUrl = root.TryGetProperty("order_url", out var ou) ? ou.GetString() ?? "" : "";
        var qrCode = root.TryGetProperty("qr_code", out var qc) ? qc.GetString() ?? "" : "";

        _context.ZaloPayOrders.Add(new ZaloPayOrder
        {
            AppTransId = appTransId,
            SoTien = soTien,
            MoTa = moTa,
            MucDich = mucDich,
            TrangThai = TrangThaiZaloPay.ChoThanhToan,
            PremiumOrderId = premiumOrderId,
            QrCodeRaw = qrCode,
            OrderUrl = orderUrl,
            UserId = userId
        });
        await _context.SaveChangesAsync();

        return new ZaloPayDonHangKetQua { AppTransId = appTransId, OrderUrl = orderUrl, QrCode = qrCode };
    }

    public async Task<string> XuLyCallbackAsync(string requestBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestBody);
            var root = doc.RootElement;
            var dataJson = root.GetProperty("data").GetString() ?? "";
            var macNhanDuoc = root.GetProperty("mac").GetString() ?? "";

            var macTinhDuoc = TinhHmacSha256(Key2, dataJson);
            if (!string.Equals(macTinhDuoc, macNhanDuoc, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ZaloPay callback: chữ ký (mac) không khớp - có thể request giả mạo.");
                return JsonSerializer.Serialize(new { return_code = -1, return_message = "mac not equal" });
            }

            using var dataDoc = JsonDocument.Parse(dataJson);
            var dataRoot = dataDoc.RootElement;
            var appTransId = dataRoot.GetProperty("app_trans_id").GetString() ?? "";
            long? zpTransId = dataRoot.TryGetProperty("zp_trans_id", out var zp) ? zp.GetInt64() : null;

            var order = await _context.ZaloPayOrders.FirstOrDefaultAsync(o => o.AppTransId == appTransId);
            if (order == null)
            {
                _logger.LogWarning("ZaloPay callback: không tìm thấy đơn hàng {AppTransId} trong hệ thống.", appTransId);
                // Vẫn báo "đã nhận" cho ZaloPay để tránh bị gọi lại liên tục
                return JsonSerializer.Serialize(new { return_code = 1, return_message = "success" });
            }

            await XuLyThanhToanThanhCongAsync(order, zpTransId);
            return JsonSerializer.Serialize(new { return_code = 1, return_message = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xử lý callback ZaloPay");
            return JsonSerializer.Serialize(new { return_code = 0, return_message = "exception" });
        }
    }

    public async Task<ZaloPayOrder?> TruyVanTrangThaiAsync(string appTransId)
    {
        var order = await _context.ZaloPayOrders.FirstOrDefaultAsync(o => o.AppTransId == appTransId);
        if (order == null) return null;
        if (order.TrangThai != TrangThaiZaloPay.ChoThanhToan) return order; // đã xử lý rồi, khỏi hỏi lại ZaloPay

        var hmacInput = $"{AppId}|{appTransId}|{Key1}";
        var mac = TinhHmacSha256(Key1, hmacInput);

        var form = new Dictionary<string, string>
        {
            ["app_id"] = AppId.ToString(),
            ["app_trans_id"] = appTransId,
            ["mac"] = mac
        };

        using var res = await _http.PostAsync(QueryEndpoint, new FormUrlEncodedContent(form));
        var resJson = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resJson);
        var root = doc.RootElement;

        // return_code = 1: giao dịch thành công. return_code = 2: thất bại. return_code = 3: đang xử lý/chưa thanh toán.
        var returnCode = root.TryGetProperty("return_code", out var rc) ? rc.GetInt32() : 0;
        if (returnCode == 1)
        {
            long? zpTransId = root.TryGetProperty("zp_trans_id", out var zp) ? zp.GetInt64() : null;
            await XuLyThanhToanThanhCongAsync(order, zpTransId);
        }
        else if (returnCode == 2)
        {
            order.TrangThai = TrangThaiZaloPay.ThatBai;
            await _context.SaveChangesAsync();
        }
        // returnCode == 3 (hoặc khác): vẫn đang chờ - không đổi gì, để lần polling sau hỏi lại

        return order;
    }

    /// <summary>Logic dùng chung khi ZaloPay xác nhận thanh toán thành công (từ callback HOẶC từ truy vấn).</summary>
    private async Task XuLyThanhToanThanhCongAsync(ZaloPayOrder order, long? zpTransId)
    {
        if (order.TrangThai == TrangThaiZaloPay.ThanhCong) return; // đã xử lý trước đó - tránh cộng dồn/kích hoạt trùng

        order.TrangThai = TrangThaiZaloPay.ThanhCong;
        order.ZpTransId = zpTransId;
        order.NgayThanhToan = DateTime.Now;

        if (order.MucDich == MucDichZaloPay.XacThucLienKet)
        {
            await XuLyLienKetTaiKhoanAsync(order);
        }
        else if (order.MucDich == MucDichZaloPay.NangCapPremium)
        {
            await XuLyNangCapPremiumAsync(order);
        }

        await _context.SaveChangesAsync();
    }

    private async Task XuLyLienKetTaiKhoanAsync(ZaloPayOrder order)
    {
        var lienKetSan = await _context.FinancialLinks
            .FirstOrDefaultAsync(f => f.UserId == order.UserId && f.NhaCungCap == NhaCungCap.ZaloPay);

        if (lienKetSan == null)
        {
            _context.FinancialLinks.Add(new FinancialLink
            {
                NhaCungCap = NhaCungCap.ZaloPay,
                TenGoiNho = "ZaloPay (UAT)",
                SoTaiKhoanAn = "UAT-" + order.AppTransId[^Math.Min(6, order.AppTransId.Length)..],
                UserId = order.UserId,
                DaKetNoi = true,
                NgayKetNoi = DateTime.Now,
                DongBoLanCuoi = DateTime.Now,
                GhiChu = "Đã xác thực quyền sở hữu qua thanh toán ZaloPay UAT thành công."
            });
        }
        else
        {
            lienKetSan.DaKetNoi = true;
            lienKetSan.NgayKetNoi ??= DateTime.Now;
            lienKetSan.DongBoLanCuoi = DateTime.Now;
        }

        // Khoản xác thực 1.000đ CŨNG là 1 giao dịch ZaloPay UAT thật -> đồng bộ luôn thành Transaction,
        // để người dùng thấy ngay kết quả "đồng bộ tự động" thay vì phải đợi lần thanh toán Premium sau đó.
        await DongBoGiaoDichAsync(order);

        await ThongBaoHelper.TaoAsync(_context, order.UserId, "Liên kết ZaloPay thành công",
            "Đã xác thực và liên kết tài khoản ZaloPay (UAT) vào MoneyCare.", LoaiThongBao.HeThong, "/integrations");
    }

    private async Task XuLyNangCapPremiumAsync(ZaloPayOrder order)
    {
        var user = await _userManager.FindByIdAsync(order.UserId);
        if (user == null) return;

        if (order.PremiumOrderId != null)
        {
            var donPremium = await _context.PremiumOrders.FindAsync(order.PremiumOrderId.Value);
            if (donPremium != null && !donPremium.DaXacNhan)
            {
                donPremium.DaXacNhan = true;
                donPremium.NgayXacNhan = DateTime.Now;

                var hetHanHienTai = user.PremiumConHieuLuc ? user.PremiumHetHan!.Value : DateTime.Today;
                user.IsPremium = true;
                user.PremiumHetHan = hetHanHienTai.AddMonths(donPremium.SoThangDangKy);
                await _userManager.UpdateAsync(user);

                await ThongBaoHelper.TaoAsync(_context, user.Id, "🎉 Premium đã kích hoạt qua ZaloPay",
                    $"Thanh toán {order.SoTien:N0}đ qua ZaloPay UAT thành công. Premium có hiệu lực đến {user.PremiumHetHan:dd/MM/yyyy}.",
                    LoaiThongBao.Premium, "/premium");
            }
        }

        // Tự động đồng bộ giao dịch thanh toán này vào Transactions (yêu cầu "đồng bộ tự động trên app/web")
        await DongBoGiaoDichAsync(order);
    }

    /// <summary>
    /// Ghi nhận 1 đơn ZaloPay UAT đã thanh toán thành công thành 1 Transaction (Chi) - dùng chung cho
    /// CẢ đơn xác thực liên kết LẪN đơn nâng cấp Premium, để "mọi giao dịch thanh toán qua ZaloPay UAT"
    /// đều tự động xuất hiện trong Giao dịch trên cả app lẫn web, đúng yêu cầu ban đầu.
    /// </summary>
    private async Task DongBoGiaoDichAsync(ZaloPayOrder order)
    {
        if (order.DaDongBoGiaoDich) return;

        var danhMuc = await _context.Categories
            .Where(c => c.UserId == order.UserId && c.Loai == LoaiGiaoDich.Chi)
            .OrderBy(c => c.TenDanhMuc)
            .FirstOrDefaultAsync();

        // Phòng trường hợp hy hữu người dùng đã xóa hết danh mục Chi - tự tạo 1 danh mục dự phòng,
        // để việc đồng bộ KHÔNG BAO GIỜ âm thầm bị bỏ qua chỉ vì thiếu danh mục.
        if (danhMuc == null)
        {
            danhMuc = new Category { TenDanhMuc = "Khác", Loai = LoaiGiaoDich.Chi, MauSac = "#6c757d", UserId = order.UserId };
            _context.Categories.Add(danhMuc);
            await _context.SaveChangesAsync();
        }

        _context.Transactions.Add(new Transaction
        {
            SoTien = order.SoTien,
            Loai = LoaiGiaoDich.Chi,
            NgayGiaoDich = order.NgayThanhToan ?? DateTime.Now,
            ThoiGianTao = DateTime.Now,
            GhiChu = $"[Đồng bộ ZaloPay UAT] {order.MoTa}",
            CategoryId = danhMuc.Id,
            UserId = order.UserId,
            Nguon = NguonGiaoDich.ZaloPay,
            TrangThaiDongBo = TrangThaiDongBoGiaoDich.ThanhCong
        });
        order.DaDongBoGiaoDich = true;
    }

    private static string SinhAppTransId()
    {
        // Định dạng ZaloPay yêu cầu: yyMMdd_<duy nhất> - dùng số ms hiện tại + số ngẫu nhiên cho chắc chắn không trùng
        var ngay = DateTime.Now.ToString("yyMMdd");
        var duyNhat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1_000_000;
        var ngauNhien = Random.Shared.Next(100, 999);
        return $"{ngay}_{duyNhat}{ngauNhien}";
    }

    private static string TinhHmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
