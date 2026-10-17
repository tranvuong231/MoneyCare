using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Areas.Admin.Controllers;

public class UsersController : AdminBaseController
{
    private readonly MoneyCareApiClient _api;

    public UsersController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? tuKhoa)
    {
        var query = string.IsNullOrWhiteSpace(tuKhoa) ? "" : $"?tuKhoa={Uri.EscapeDataString(tuKhoa)}";
        var ds = await _api.GetAsync<List<AdminUserDto>>($"/api/admin/users{query}") ?? new();
        ViewBag.TuKhoa = tuKhoa;
        return View(ds.Select(AnhXa).ToList());
    }

    public async Task<IActionResult> Details(string id)
    {
        var d = await _api.GetAsync<AdminUserDetailDto>($"/api/admin/users/{id}");
        if (d == null) return NotFound();

        ViewBag.TongThu = d.TongThu;
        ViewBag.TongChi = d.TongChi;
        ViewBag.SoGiaoDich = d.SoGiaoDich;
        ViewBag.LaAdmin = d.LaAdmin;
        ViewBag.BiKhoa = d.BiKhoa;
        ViewBag.SoDonPremium = d.SoDonPremium;

        return View(AnhXa(d));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatPremium(string id, int soThang)
    {
        await _api.PostAsync($"/api/admin/users/{id}/premium", new CapNhatPremiumRequest { SoThang = soThang });
        TempData["ThongBao"] = $"Đã gia hạn Premium thêm {soThang} tháng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyPremium(string id)
    {
        await _api.DeleteAsync($"/api/admin/users/{id}/premium");
        TempData["ThongBao"] = "Đã hủy Premium.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaTaiKhoan(string id)
    {
        await _api.PostAsync($"/api/admin/users/{id}/khoa");
        TempData["ThongBao"] = "Đã khóa tài khoản.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoKhoaTaiKhoan(string id)
    {
        await _api.PostAsync($"/api/admin/users/{id}/mo-khoa");
        TempData["ThongBao"] = "Đã mở khóa tài khoản.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapQuyenAdmin(string id)
    {
        await _api.PostAsync($"/api/admin/users/{id}/cap-quyen-admin");
        TempData["ThongBao"] = "Đã cấp quyền Admin.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuHoiQuyenAdmin(string id)
    {
        try
        {
            await _api.PostAsync($"/api/admin/users/{id}/thu-hoi-quyen-admin");
            TempData["ThongBao"] = "Đã thu hồi quyền Admin.";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapXacMinh(string id)
    {
        await _api.PostAsync($"/api/admin/users/{id}/xac-minh");
        TempData["ThongBao"] = "Đã cấp tích xanh.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyXacMinh(string id)
    {
        await _api.DeleteAsync($"/api/admin/users/{id}/xac-minh");
        TempData["ThongBao"] = "Đã hủy tích xanh.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string id)
    {
        try
        {
            await _api.DeleteAsync($"/api/admin/users/{id}");
            TempData["ThongBao"] = "Đã xóa tài khoản khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private static ApplicationUser AnhXa(AdminUserDto d) => new()
    {
        Id = d.Id,
        HoTen = d.HoTen,
        Email = d.Email,
        UserName = d.UserName,
        NgayTao = d.NgayTao,
        IsPremium = d.IsPremium,
        PremiumHetHan = d.PremiumConHieuLuc ? DateTime.MaxValue : null,
        DaXacMinhDanhTinh = d.DaXacMinhDanhTinh
    };
}
