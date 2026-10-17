// Provider THẬT cho ZaloPay (môi trường sandbox/UAT) - khác với NganHangProviderStub/MoMoProviderStub
// (vẫn để nguyên "chưa hỗ trợ" theo đúng yêu cầu), provider này gọi API ZaloPay thật thông qua
// IZaloPayService (xem Services/ZaloPayService.cs).
//
// LƯU Ý QUAN TRỌNG: ZaloPay là 1 CỔNG THANH TOÁN (payment gateway), KHÔNG cung cấp API kiểu
// "Open Banking" để đọc toàn bộ lịch sử giao dịch ví của người dùng. Vì vậy "liên kết tài khoản"
// ở đây được thiết kế thực tế là: người dùng quét mã QR ZaloPay UAT thanh toán 1 khoản xác thực
// nhỏ (1.000đ) để CHỨNG MINH họ đang sở hữu 1 tài khoản ZaloPay UAT hợp lệ -> hệ thống ghi nhận
// liên kết đã xác thực. Sau đó, MỌI giao dịch thanh toán qua ZaloPay UAT thực hiện trong app
// (hiện tại là thanh toán nâng cấp Premium) sẽ tự động được đồng bộ làm Transaction.
using MoneyCare.Data;
using MoneyCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MoneyCare.Services;

public class ZaloPayProvider : IFinancialLinkProvider
{
    private readonly IZaloPayService _zaloPayService;
    private readonly ApplicationDbContext _context;

    public ZaloPayProvider(IZaloPayService zaloPayService, ApplicationDbContext context)
    {
        _zaloPayService = zaloPayService;
        _context = context;
    }

    public NhaCungCap NhaCungCap => NhaCungCap.ZaloPay;

    /// <summary>
    /// Dùng cho luồng web (MVC) - tạo đơn xác thực 1.000đ và trả về order_url ZaloPay để
    /// điều hướng người dùng sang thanh toán/quét mã. Ứng dụng Flutter dùng endpoint
    /// riêng /api/zalopay/lien-ket/tao-don (trả kèm ảnh QR) để trải nghiệm mượt hơn - xem ZaloPayApiController.
    /// </summary>
    public async Task<string> LayUrlXacThucAsync(string userId, string urlCallback)
    {
        var donHang = await _zaloPayService.TaoDonHangAsync(
            userId,
            soTien: 1000,
            moTa: "Xac thuc lien ket ZaloPay UAT - MoneyCare",
            mucDich: MucDichZaloPay.XacThucLienKet);

        if (string.IsNullOrWhiteSpace(donHang.OrderUrl))
        {
            throw new InvalidOperationException("ZaloPay không trả về order_url - kiểm tra lại cấu hình AppId/Key1/Key2.");
        }

        return donHang.OrderUrl;
    }

    /// <summary>
    /// "code" ở đây chính là AppTransId của đơn xác thực đã tạo ở LayUrlXacThucAsync.
    /// Chủ động truy vấn (poll) trạng thái mới nhất từ ZaloPay trước khi trả kết quả,
    /// phòng trường hợp callback chưa kịp gọi về (chưa cấu hình ngrok).
    /// </summary>
    public async Task<KetQuaXacThucLienKet> XuLyCallbackAsync(string code, string? state)
    {
        var order = await _zaloPayService.TruyVanTrangThaiAsync(code);

        if (order == null)
        {
            return new KetQuaXacThucLienKet { ThanhCong = false, ThongBaoLoi = "Không tìm thấy đơn xác thực ZaloPay." };
        }

        if (order.TrangThai == TrangThaiZaloPay.ThanhCong)
        {
            return new KetQuaXacThucLienKet
            {
                ThanhCong = true,
                SoTaiKhoanDayDu = "ZaloPay UAT",
                TenChuTaiKhoan = "Đã xác thực qua thanh toán ZaloPay UAT"
            };
        }

        return new KetQuaXacThucLienKet
        {
            ThanhCong = false,
            ThongBaoLoi = order.TrangThai == TrangThaiZaloPay.ThatBai
                ? "Thanh toán xác thực ZaloPay thất bại hoặc đã bị hủy."
                : "Chưa nhận được thanh toán xác thực - hãy quét mã QR bằng app ZaloPay (môi trường UAT) rồi thử lại."
        };
    }

    /// <summary>
    /// Lưới an toàn cho nút "Đồng bộ ngay": đa số trường hợp giao dịch ZaloPay UAT đã được tự động
    /// đồng bộ NGAY khi xác nhận thanh toán thành công (xem ZaloPayService.DongBoGiaoDichAsync).
    /// Hàm này còn xử lý thêm 2 trường hợp:
    ///   1) Đơn còn "Chờ thanh toán" trong hệ thống (VD người dùng đóng app giữa chừng, không đợi
    ///      polling xác nhận xong) -> chủ động hỏi lại ZaloPay 1 lần nữa xem đã thanh toán chưa.
    ///   2) Đơn đã "Thành công" nhưng vì lý do nào đó chưa kịp đánh dấu đã đồng bộ.
    /// Đồng bộ CẢ đơn xác thực liên kết (1.000đ) LẪN đơn nâng cấp Premium - không giới hạn theo mốc
    /// thời gian "tuThoiGian" vì mỗi đơn ZaloPay đã có cờ DaDongBoGiaoDich riêng để chống trùng lặp.
    /// </summary>
    public async Task<List<GiaoDichDongBo>> LayGiaoDichMoiAsync(FinancialLink lienKet, DateTime tuThoiGian)
    {
        // Bước 1: chủ động hỏi lại ZaloPay cho các đơn còn "Chờ thanh toán" gần đây (giới hạn 3 ngày
        // để tránh dò lại các đơn quá cũ/đã bỏ dở) - nếu đã thanh toán xong, hàm này sẽ tự đồng bộ luôn.
        var donChoXuLy = await _context.ZaloPayOrders
            .Where(o => o.UserId == lienKet.UserId
                        && o.TrangThai == TrangThaiZaloPay.ChoThanhToan
                        && o.NgayTao >= DateTime.Now.AddDays(-3))
            .Select(o => o.AppTransId)
            .ToListAsync();

        foreach (var appTransId in donChoXuLy)
        {
            await _zaloPayService.TruyVanTrangThaiAsync(appTransId);
        }

        // Bước 2: lấy các đơn đã thành công nhưng chưa được ghi nhận thành Transaction
        var donChuaDongBo = await _context.ZaloPayOrders
            .Where(o => o.UserId == lienKet.UserId
                        && o.TrangThai == TrangThaiZaloPay.ThanhCong
                        && !o.DaDongBoGiaoDich)
            .ToListAsync();

        var ketQua = donChuaDongBo.Select(o => new GiaoDichDongBo
        {
            MaGiaoDichNgoai = o.AppTransId,
            SoTien = o.SoTien,
            Loai = LoaiGiaoDich.Chi,
            ThoiGian = o.NgayThanhToan!.Value,
            MoTa = $"[Đồng bộ ZaloPay UAT] {o.MoTa}"
        }).ToList();
        // Lưu ý: đơn nào vừa được xác nhận thành công ở Bước 1 (repoll) sẽ được ZaloPayService tự
        // đồng bộ NGAY bên trong (đã lưu Transaction + đánh dấu DaDongBoGiaoDich=true) nên sẽ KHÔNG
        // xuất hiện lại ở đây nữa - tránh insert Transaction trùng lặp. Vì vậy số "giao dịch mới" báo
        // ở màn hình có thể thấp hơn thực tế 1-2 đơn ngay sau khi bấm "Đồng bộ ngay" lần đầu, nhưng
        // giao dịch vẫn CHẮC CHẮN đã có trong danh sách Giao dịch - không bị mất.

        foreach (var don in donChuaDongBo)
        {
            don.DaDongBoGiaoDich = true;
        }
        if (donChuaDongBo.Count > 0)
        {
            lienKet.DongBoLanCuoi = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return ketQua;
    }

    /// <summary>ZaloPay UAT không có khái niệm "thu hồi quyền" (không phải OAuth) - chỉ đánh dấu ngắt liên kết phía MoneyCare.</summary>
    public Task HuyQuyenTruyCapAsync(FinancialLink lienKet) => Task.CompletedTask;
}
