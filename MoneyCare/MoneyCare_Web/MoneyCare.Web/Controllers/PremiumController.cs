using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

public class PremiumController : Controller
{
    private readonly MoneyCareApiClient _api;

    public PremiumController(MoneyCareApiClient api)
    {
        _api = api;
    }

    // Một số nơi (menu cũ, link gõ tay...) có thể trỏ thẳng tới /Premium không kèm action - chuyển
    // hướng sang đúng trang bảng giá thay vì để 404.
    [AllowAnonymous]
    public IActionResult Index() => RedirectToAction(nameof(Pricing));

    [AllowAnonymous]
    public IActionResult Pricing()
    {
        if (Request.Cookies.TryGetValue("ThongBaoPremium", out var thongBao))
        {
            TempData["Loi"] = Uri.UnescapeDataString(thongBao);
            Response.Cookies.Delete("ThongBaoPremium");
        }
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Upgrade(int soThang = 1)
    {
        try
        {
            var don = await _api.PostAsync<PremiumOrderDto>($"/api/premium/nang-cap?soThang={soThang}");

            ViewBag.SoTaiKhoan = don!.SoTaiKhoan;
            ViewBag.BinNganHang = don.BinNganHang;
            ViewBag.TenNganHangHienThi = don.TenNganHangHienThi;
            ViewBag.DonId = don.Id;

            return View(new PremiumOrder { Id = don.Id, MaGiaoDich = don.MaGiaoDich, SoTien = don.SoTien, SoThangDangKy = don.SoThangDangKy });
        }
        catch (ApiException ex)
        {
            TempData["ThongBao"] = ex.Message;
            return RedirectToAction(nameof(Pricing));
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThanhToan(int donId)
    {
        try
        {
            var res = await _api.PostAsync<ThongBaoResponse>($"/api/premium/xac-nhan-thanh-toan/{donId}");
            TempData["ThongBao"] = "🎉 " + res?.ThongBao;
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction("Index", "Home");
    }

    // ==== Thanh toán bằng QR ZaloPay THẬT (môi trường UAT/sandbox) ====

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoDonZaloPay(int donId, int soThangDangKy = 1)
    {
        try
        {
            var don = await _api.PostAsync<ZaloPayDonHangDto>($"/api/zalopay/premium/tao-don?soThang={soThangDangKy}");
            return Json(new { thanhCong = true, appTransId = don!.AppTransId });
        }
        catch (ApiException ex)
        {
            return Json(new { thanhCong = false, loi = ex.Message });
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> AnhQrZaloPay(string appTransId)
    {
        var (bytes, contentType, _) = await _api.GetBytesAsync($"/api/zalopay/don/{appTransId}/anh-qr");
        return File(bytes, contentType);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> KiemTraTrangThaiZaloPay(string appTransId)
    {
        var trangThai = await _api.GetAsync<ZaloPayTrangThaiDto>($"/api/zalopay/don/{appTransId}/trang-thai");
        return Json(new { trangThai = trangThai?.TrangThai });
    }
}
