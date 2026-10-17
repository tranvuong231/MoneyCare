using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public NotificationsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var res = await _api.GetAsync<ThongBaoDanhSachResponse>("/api/thong-bao") ?? new();
        return View(res.DanhSach.Select(ThongBaoMapper.TuDto).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauDaXem(int id)
    {
        await _api.PostAsync($"/api/thong-bao/{id}/danh-dau-da-xem");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauChuaXem(int id)
    {
        await _api.PostAsync($"/api/thong-bao/{id}/danh-dau-chua-xem");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauTatCaDaXem()
    {
        await _api.PostAsync("/api/thong-bao/danh-dau-tat-ca-da-xem");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        await _api.DeleteAsync($"/api/thong-bao/{id}");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> DiToi(int id)
    {
        var res = await _api.GetAsync<ThongBaoDanhSachResponse>("/api/thong-bao");
        var tb = res?.DanhSach.FirstOrDefault(t => t.Id == id);
        if (tb == null) return RedirectToAction(nameof(Index));

        if (!tb.DaXem) await _api.PostAsync($"/api/thong-bao/{id}/danh-dau-da-xem");

        var duongDan = (tb.DuongDanDieuHuong ?? "").Trim('/').ToLowerInvariant();
        return duongDan switch
        {
            "bills" => RedirectToAction("Index", "Bills"),
            "insights" => RedirectToAction("Index", "Insights"),
            "reports" => RedirectToAction("Index", "Reports"),
            "transactions" => RedirectToAction("Index", "Transactions"),
            "household" => RedirectToAction("Index", "Household"),
            "premium" => RedirectToAction("Pricing", "Premium"),
            "profile" => RedirectToAction("Index", "Profile"),
            "settings" => RedirectToAction("Index", "Settings"),
            _ => RedirectToAction(nameof(Index))
        };
    }
}
