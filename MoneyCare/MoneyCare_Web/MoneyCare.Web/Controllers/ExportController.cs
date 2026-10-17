using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly MoneyCareApiClient _api;
    private readonly ICurrentUserService _currentUser;

    public ExportController(MoneyCareApiClient api, ICurrentUserService currentUser)
    {
        _api = api;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _currentUser.GetAppUserAsync();
        if (user?.PremiumConHieuLuc != true)
        {
            TempData["Loi"] = "Tính năng này chỉ dành cho tài khoản Premium. Hãy nâng cấp để mở khóa!";
            return RedirectToAction("Pricing", "Premium");
        }

        var homNay = DateTime.Today;
        ViewBag.TuNgay = new DateTime(homNay.Year, homNay.Month, 1).ToString("yyyy-MM-dd");
        ViewBag.DenNgay = homNay.ToString("yyyy-MM-dd");
        return View();
    }

    public async Task<IActionResult> Excel(DateTime tuNgay, DateTime denNgay)
    {
        try
        {
            var (bytes, contentType, tenFile) = await _api.GetBytesAsync($"/api/export/excel?tuNgay={tuNgay:O}&denNgay={denNgay:O}");
            return File(bytes, contentType, tenFile ?? $"BaoCaoThuChi_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx");
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction("Index", "Premium");
        }
    }

    public async Task<IActionResult> Pdf(DateTime tuNgay, DateTime denNgay)
    {
        try
        {
            var (bytes, contentType, tenFile) = await _api.GetBytesAsync($"/api/export/pdf?tuNgay={tuNgay:O}&denNgay={denNgay:O}");
            return File(bytes, contentType, tenFile ?? $"BaoCaoThuChi_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.pdf");
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction("Index", "Premium");
        }
    }
}
