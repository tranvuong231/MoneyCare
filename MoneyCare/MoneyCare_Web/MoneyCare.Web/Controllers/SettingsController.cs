using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public SettingsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var dto = await _api.GetAsync<UserDto>("/api/auth/toi");
        return View(UserMapper.TuDto(dto!, _api.BaseUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatGiaoDien(bool giaoDienToi, IFormFile? anhNen, bool xoaAnhNen = false)
    {
        try
        {
            if (anhNen != null && anhNen.Length > 0 && !xoaAnhNen)
            {
                using var stream = anhNen.OpenReadStream();
                await _api.PostFileAsync<Dictionary<string, string>>("/api/settings/doi-anh-nen", "anhNen", stream, anhNen.FileName);
            }

            await _api.PutAsync("/api/settings/giao-dien", new CapNhatGiaoDienRequest { GiaoDienToi = giaoDienToi, XoaAnhNen = xoaAnhNen });
            TempData["ThongBao"] = "Đã lưu thay đổi giao diện!";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatThongBao(bool batBaoCaoTaiChinh, bool batNhacNhoNhapLieu, TanSuatBaoCao tanSuatBaoCao, string gioBaoCao)
    {
        await _api.PutAsync("/api/settings/thong-bao", new CapNhatThongBaoRequest
        {
            BatBaoCaoTaiChinh = batBaoCaoTaiChinh,
            BatNhacNhoNhapLieu = batNhacNhoNhapLieu,
            TanSuatBaoCao = tanSuatBaoCao,
            GioBaoCao = gioBaoCao
        });
        TempData["ThongBao"] = "Đã lưu cài đặt thông báo!";
        return RedirectToAction(nameof(Index));
    }
}
