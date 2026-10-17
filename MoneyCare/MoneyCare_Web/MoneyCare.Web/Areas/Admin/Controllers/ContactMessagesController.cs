using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Areas.Admin.Controllers;

public class ContactMessagesController : AdminBaseController
{
    private readonly MoneyCareApiClient _api;

    public ContactMessagesController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? tuKhoa)
    {
        var query = string.IsNullOrWhiteSpace(tuKhoa) ? "" : $"?tuKhoa={Uri.EscapeDataString(tuKhoa)}";
        var ds = await _api.GetAsync<List<AdminContactMessageDto>>($"/api/admin/contact-messages{query}") ?? new();
        ViewBag.TuKhoa = tuKhoa;

        return View(ds.Select(t => new ContactMessage
        {
            Id = t.Id,
            HoTen = t.HoTen,
            Email = t.Email,
            NoiDung = t.NoiDung,
            NgayGui = t.NgayGui,
            DaXem = t.DaXem
        }).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauDaXem(int id)
    {
        await _api.PostAsync($"/api/admin/contact-messages/{id}/danh-dau-da-xem");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        await _api.DeleteAsync($"/api/admin/contact-messages/{id}");
        TempData["ThongBao"] = "Đã xóa tin nhắn";
        return RedirectToAction(nameof(Index));
    }
}
