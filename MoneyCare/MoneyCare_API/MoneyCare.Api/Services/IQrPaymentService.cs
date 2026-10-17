// Giao diện định nghĩa các chức năng thanh toán bằng mã QR.
namespace MoneyCare.Services;

public interface IQrPaymentService
{
    /// <summary>
    /// Sinh ảnh QR code (PNG bytes) theo chuẩn VietQR (EMVCo) để chuyển khoản ngân hàng.
    /// </summary>
    byte[] TaoAnhVietQR(string soTaiKhoan, string tenNganHangBIN, decimal soTien, string noiDung);

    /// <summary>
    /// Sinh ảnh QR (PNG bytes) từ 1 chuỗi payload EMVCo CÓ SẴN (VD: field "qr_code" ZaloPay trả về
    /// sau khi tạo đơn hàng qua API v2/create) - dùng chung 1 chỗ vẽ QR cho cả VietQR ngân hàng lẫn ZaloPay.
    /// </summary>
    byte[] TaoAnhTuChuoi(string noiDungPayload);
}
