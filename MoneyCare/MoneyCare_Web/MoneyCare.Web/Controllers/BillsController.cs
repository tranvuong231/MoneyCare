using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class BillsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public BillsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? timKiem, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(timKiem)) query.Add($"timKiem={Uri.EscapeDataString(timKiem)}");
        if (!string.IsNullOrWhiteSpace(trangThai)) query.Add($"trangThai={Uri.EscapeDataString(trangThai)}");
        if (tuNgay.HasValue) query.Add($"tuNgay={tuNgay.Value:O}");
        if (denNgay.HasValue) query.Add($"denNgay={denNgay.Value:O}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        var ds = await _api.GetAsync<List<BillDto>>($"/api/bills{queryString}") ?? new();

        ViewBag.TimKiem = timKiem;
        ViewBag.TrangThai = trangThai;
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

        return View(ds.Select(BillMapper.TuDto).ToList());
    }

    public IActionResult Create() => View(new Bill { NgayDenHan = DateTime.Today.AddDays(7) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Bill model)
    {
        ModelState.Remove(nameof(Bill.UserId));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync<BillDto>("/api/bills", new BillInputDto
            {
                TenHoaDon = model.TenHoaDon,
                SoTien = model.SoTien,
                NgayDenHan = model.NgayDenHan,
                DaThanhToan = model.DaThanhToan,
                GhiChu = model.GhiChu
            });
            TempData["ThongBao"] = "Thêm hóa đơn thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var ds = await _api.GetAsync<List<BillDto>>("/api/bills") ?? new();
        var dto = ds.FirstOrDefault(b => b.Id == id);
        if (dto == null) return NotFound();
        return View(BillMapper.TuDto(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Bill model)
    {
        if (id != model.Id) return NotFound();
        ModelState.Remove(nameof(Bill.UserId));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PutAsync<BillDto>($"/api/bills/{id}", new BillInputDto
            {
                TenHoaDon = model.TenHoaDon,
                SoTien = model.SoTien,
                NgayDenHan = model.NgayDenHan,
                DaThanhToan = model.DaThanhToan,
                GhiChu = model.GhiChu
            });
            TempData["ThongBao"] = "Cập nhật hóa đơn thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauThanhToan(int id)
    {
        await _api.PostAsync<BillDto>($"/api/bills/{id}/danh-dau-thanh-toan");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var ds = await _api.GetAsync<List<BillDto>>("/api/bills") ?? new();
        var dto = ds.FirstOrDefault(b => b.Id == id);
        if (dto == null) return NotFound();
        return View(BillMapper.TuDto(dto));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _api.DeleteAsync($"/api/bills/{id}");
        TempData["ThongBao"] = "Xóa hóa đơn thành công!";
        return RedirectToAction(nameof(Index));
    }
}
