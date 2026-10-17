using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Areas.Admin.Controllers;

public class HouseholdsController : AdminBaseController
{
    private readonly MoneyCareApiClient _api;

    public HouseholdsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? tuKhoa)
    {
        var query = string.IsNullOrWhiteSpace(tuKhoa) ? "" : $"?tuKhoa={Uri.EscapeDataString(tuKhoa)}";
        var ds = await _api.GetAsync<List<AdminHouseholdDto>>($"/api/admin/households{query}") ?? new();
        ViewBag.TuKhoa = tuKhoa;

        return View(ds.Select(h => new Household
        {
            Id = h.Id,
            TenHoGiaDinh = h.TenHoGiaDinh,
            MaMoi = h.MaMoi,
            ChuHo = new ApplicationUser { HoTen = h.TenChuHo },
            NgayTao = h.NgayTao,
            ThanhVien = Enumerable.Range(0, h.SoThanhVien).Select(_ => new ApplicationUser()).ToList()
        }).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        await _api.DeleteAsync($"/api/admin/households/{id}");
        TempData["ThongBao"] = "Đã xóa hộ gia đình.";
        return RedirectToAction(nameof(Index));
    }
}
