using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class HouseholdController : Controller
{
    private readonly MoneyCareApiClient _api;

    public HouseholdController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        HouseholdDto? dto;
        try
        {
            dto = await _api.GetAsync<HouseholdDto>("/api/household");
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction("Index", "Premium");
        }

        if (dto == null) return View("ChuaCoHoGiaDinh");

        ViewBag.LaChuHo = dto.LaChuHo;

        var vm = new HouseholdViewModel
        {
            HoGiaDinh = new Household { Id = dto.Id, TenHoGiaDinh = dto.TenHoGiaDinh, MaMoi = dto.MaMoi, ChuHo = new ApplicationUser { HoTen = dto.TenChuHo } },
            TongThuGiaDinh = dto.TongThuGiaDinh,
            TongChiGiaDinh = dto.TongChiGiaDinh,
            GiaoDichGanDay = dto.GiaoDichGanDay.Select(TransactionMapper.TuDto).ToList(),
            ThanhVienChiTieu = dto.ThanhVienChiTieu.Select(t => new ThanhVienChiTieu { TenThanhVien = t.TenThanhVien, TongThu = t.TongThu, TongChi = t.TongChi }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string tenHoGiaDinh)
    {
        try
        {
            var res = await _api.PostAsync<ThongBaoResponse>("/api/household/tao", tenHoGiaDinh);
            TempData["ThongBao"] = res?.ThongBao;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

    [HttpGet]
    public IActionResult ThamGia() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThamGia(string maMoi)
    {
        try
        {
            var res = await _api.PostAsync<ThongBaoResponse>("/api/household/tham-gia", maMoi);
            TempData["ThongBao"] = res?.ThongBao;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RoiHo()
    {
        try
        {
            await _api.PostAsync("/api/household/roi-ho");
            TempData["ThongBao"] = "Bạn đã rời hộ gia đình";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaHoGiaDinh()
    {
        try
        {
            await _api.DeleteAsync("/api/household");
            TempData["ThongBao"] = "Đã xóa hộ gia đình";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
