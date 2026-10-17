using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Areas.Admin.Controllers;

public class PremiumOrdersController : AdminBaseController
{
    private readonly MoneyCareApiClient _api;

    public PremiumOrdersController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(bool? chuaXacNhan, string? tuKhoa)
    {
        var query = new List<string>();
        if (chuaXacNhan == true) query.Add("chuaXacNhan=true");
        if (!string.IsNullOrWhiteSpace(tuKhoa)) query.Add($"tuKhoa={Uri.EscapeDataString(tuKhoa)}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        var ds = await _api.GetAsync<List<AdminPremiumOrderDto>>($"/api/admin/premium-orders{queryString}") ?? new();
        ViewBag.ChuaXacNhan = chuaXacNhan;
        ViewBag.TuKhoa = tuKhoa;

        return View(ds.Select(o => new PremiumOrder
        {
            Id = o.Id,
            MaGiaoDich = o.MaGiaoDich,
            SoTien = o.SoTien,
            SoThangDangKy = o.SoThangDangKy,
            DaXacNhan = o.DaXacNhan,
            NgayTao = o.NgayTao,
            User = new ApplicationUser { Email = o.EmailNguoiDung }
        }).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        await _api.DeleteAsync($"/api/admin/premium-orders/{id}");
        TempData["ThongBao"] = $"Đã xóa đơn #{id}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id)
    {
        await _api.PostAsync($"/api/admin/premium-orders/{id}/xac-nhan");
        TempData["ThongBao"] = $"Đã xác nhận đơn #{id} và kích hoạt Premium.";
        return RedirectToAction(nameof(Index));
    }
}
