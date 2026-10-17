using Microsoft.AspNetCore.Mvc;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/payment")]
public class PaymentController : BaseApiController
{
    private readonly IQrPaymentService _qrPaymentService;

    public PaymentController(IQrPaymentService qrPaymentService)
    {
        _qrPaymentService = qrPaymentService;
    }

    // Trả về ảnh QR VietQR trực tiếp (dùng trong <img>/Image.memory bên client)
    // noiDung để nullable + có giá trị mặc định "" vì đây là trường KHÔNG bắt buộc trên form (Web) -
    // nếu để string thường (non-nullable), [ApiController] sẽ tự áp validation [Required], mà
    // [Required] mặc định coi cả chuỗi rỗng là lỗi (AllowEmptyStrings=false) => 400 khi người dùng
    // để trống ô "Nội dung chuyển khoản", làm ảnh QR bị vỡ.
    [HttpGet("anh-qr")]
    public IActionResult AnhQr(string soTaiKhoan, string binNganHang, decimal soTien, string? noiDung = "")
    {
        var bytes = _qrPaymentService.TaoAnhVietQR(soTaiKhoan, binNganHang, soTien, noiDung ?? "");
        return File(bytes, "image/png");
    }
}
