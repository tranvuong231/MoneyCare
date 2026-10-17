using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/zalopay")]
public class ZaloPayController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IZaloPayService _zaloPayService;
    private readonly IQrPaymentService _qrPaymentService;

    public ZaloPayController(ApplicationDbContext context, IZaloPayService zaloPayService, IQrPaymentService qrPaymentService)
    {
        _context = context;
        _zaloPayService = zaloPayService;
        _qrPaymentService = qrPaymentService;
    }

    // Tạo đơn xác thực 1.000đ để liên kết tài khoản ZaloPay UAT (tính năng Premium, giống các nhà cung cấp khác)
    [HttpPost("lien-ket/tao-don")]
    public async Task<IActionResult> TaoDonLienKet()
    {
        var user = await HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>().FindByIdAsync(UserId);
        if (user == null || !user.PremiumConHieuLuc)
            return Loi("Tính năng này chỉ dành cho tài khoản Premium. Hãy nâng cấp để mở khóa!", 402);

        var don = await _zaloPayService.TaoDonHangAsync(UserId, 1000, "Xac thuc lien ket ZaloPay UAT - MoneyCare", MucDichZaloPay.XacThucLienKet);
        return Ok(new ZaloPayDonHangDto
        {
            AppTransId = don.AppTransId,
            SoTien = 1000,
            MoTa = "Xác thực liên kết ZaloPay UAT",
            UrlAnhQr = $"/zalopay/don/{don.AppTransId}/anh-qr",
            OrderUrl = don.OrderUrl
        });
    }

    // Tạo đơn thanh toán ZaloPay UAT để nâng cấp Premium - KHÔNG cần đang là Premium
    [HttpPost("premium/tao-don")]
    public async Task<IActionResult> TaoDonPremium(int soThang = 1)
    {
        var giaThang = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<decimal>("PremiumPayment:GiaThang");
        var soTien = giaThang * soThang;
        var maGiaoDich = "PREMIUM" + UserId.Substring(0, Math.Min(6, UserId.Length)).ToUpper() + DateTime.Now.ToString("HHmmss");

        var donPremium = new PremiumOrder { UserId = UserId, MaGiaoDich = maGiaoDich, SoTien = soTien, SoThangDangKy = soThang };
        _context.PremiumOrders.Add(donPremium);
        await _context.SaveChangesAsync();

        try
        {
            var donZaloPay = await _zaloPayService.TaoDonHangAsync(
                UserId, soTien, $"Nang cap Premium {soThang} thang - MoneyCare (don #{donPremium.Id})",
                MucDichZaloPay.NangCapPremium, donPremium.Id);

            return Ok(new ZaloPayDonHangDto
            {
                AppTransId = donZaloPay.AppTransId,
                SoTien = soTien,
                MoTa = $"Nâng cấp Premium {soThang} tháng",
                UrlAnhQr = $"/zalopay/don/{donZaloPay.AppTransId}/anh-qr",
                OrderUrl = donZaloPay.OrderUrl,
                PremiumOrderId = donPremium.Id,
                SoThangDangKy = soThang
            });
        }
        catch (InvalidOperationException ex)
        {
            return Loi(ex.Message);
        }
    }

    [HttpGet("don/{appTransId}/trang-thai")]
    public async Task<IActionResult> TrangThai(string appTransId)
    {
        var don = await _context.ZaloPayOrders.FirstOrDefaultAsync(o => o.AppTransId == appTransId && o.UserId == UserId);
        if (don == null) return NotFound();

        if (don.TrangThai == TrangThaiZaloPay.ChoThanhToan)
        {
            don = await _zaloPayService.TruyVanTrangThaiAsync(appTransId) ?? don;
        }

        var user = await HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>().FindByIdAsync(UserId);

        return Ok(new ZaloPayTrangThaiDto
        {
            AppTransId = don.AppTransId,
            TrangThai = don.TrangThai.ToString(),
            SoTien = don.SoTien,
            IsPremium = user?.IsPremium ?? false,
            PremiumHetHan = user?.PremiumHetHan
        });
    }

    [HttpGet("don/{appTransId}/anh-qr")]
    public async Task<IActionResult> AnhQr(string appTransId)
    {
        var don = await _context.ZaloPayOrders.FirstOrDefaultAsync(o => o.AppTransId == appTransId && o.UserId == UserId);
        if (don == null || string.IsNullOrWhiteSpace(don.QrCodeRaw)) return NotFound();

        var bytes = _qrPaymentService.TaoAnhTuChuoi(don.QrCodeRaw);
        return File(bytes, "image/png");
    }

    // Endpoint callback ZaloPay Server gọi trực tiếp về (không qua JWT của người dùng) - chỉ dùng
    // khi bạn cấu hình CallbackUrl công khai (vd qua ngrok). Không bắt buộc vì hệ thống đã tự polling.
    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var ketQua = await _zaloPayService.XuLyCallbackAsync(body);
        return Content(ketQua, "application/json");
    }
}
