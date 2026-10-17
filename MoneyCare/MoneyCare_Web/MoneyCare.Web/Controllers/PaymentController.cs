using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly MoneyCareApiClient _api;

    public PaymentController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaoQr(string soTaiKhoan, string binNganHang, decimal soTien, string noiDung)
    {
        if (string.IsNullOrWhiteSpace(soTaiKhoan) || string.IsNullOrWhiteSpace(binNganHang))
        {
            TempData["Loi"] = "Vui lòng nhập đầy đủ số tài khoản và ngân hàng";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.SoTaiKhoan = soTaiKhoan;
        ViewBag.BinNganHang = binNganHang;
        ViewBag.SoTien = soTien;
        ViewBag.NoiDung = noiDung;
        ViewBag.DaTaoQr = true;
        return View("Index");
    }

    // Trả về ảnh QR trực tiếp (dùng trong <img src="/Payment/AnhQr?...">) - Web chỉ chuyển tiếp sang API
    public async Task<IActionResult> AnhQr(string? soTaiKhoan, string? binNganHang, decimal soTien, string? noiDung)
    {
        // Các tham số có thể null khi form để trống (vd không nhập Nội dung chuyển khoản) - Uri.EscapeDataString
        // ném ArgumentNullException nếu truyền null thẳng vào, nên luôn quy về chuỗi rỗng trước.
        var duongDan = $"/api/payment/anh-qr?soTaiKhoan={Uri.EscapeDataString(soTaiKhoan ?? "")}" +
            $"&binNganHang={Uri.EscapeDataString(binNganHang ?? "")}&soTien={soTien}&noiDung={Uri.EscapeDataString(noiDung ?? "")}";
        var (bytes, contentType, _) = await _api.GetBytesAsync(duongDan);
        return File(bytes, contentType);
    }
}
