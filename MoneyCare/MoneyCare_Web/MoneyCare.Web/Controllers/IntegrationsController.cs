using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

/// <summary>
/// Ghi chú so với bản gốc: luồng OAuth redirect-with-code cho Ngân hàng/MoMo (chưa có hợp đồng đối
/// tác thật trong đồ án) đã được đơn giản hóa - bấm "Kết nối" sẽ vào thẳng form nhập tay demo, thay
/// vì thử redirect trước rồi mới rơi về form (nghiệp vụ thật của việc này giờ nằm ở MoneyCare_API).
/// </summary>
[Authorize]
public class IntegrationsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public IntegrationsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var ds = await _api.GetAsync<List<FinancialLinkDto>>("/api/integrations") ?? new();
            return View(ds.Select(FinancialLinkMapper.TuDto).ToList());
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction("Index", "Premium");
        }
    }

    [HttpGet]
    public IActionResult Create(NhaCungCap nhaCungCap)
    {
        if (nhaCungCap == NhaCungCap.ZaloPay) return RedirectToAction(nameof(LienKetZaloPay));
        return View("Create", new FinancialLink { NhaCungCap = nhaCungCap });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialLink model, string soTaiKhoanDayDu)
    {
        ModelState.Remove(nameof(FinancialLink.UserId));
        ModelState.Remove(nameof(FinancialLink.SoTaiKhoanAn));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync<FinancialLinkDto>("/api/integrations", new FinancialLinkInputDto
            {
                NhaCungCap = model.NhaCungCap,
                TenGoiNho = model.TenGoiNho,
                SoTaiKhoanDayDu = soTaiKhoanDayDu,
                GhiChu = model.GhiChu
            });
            TempData["ThongBao"] = "Đã lưu thông tin liên kết (chế độ demo).";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // ==== Liên kết ZaloPay bằng QR + polling ====

    [HttpGet]
    public async Task<IActionResult> LienKetZaloPay()
    {
        var don = await _api.PostAsync<ZaloPayDonHangDto>("/api/zalopay/lien-ket/tao-don");
        ViewBag.AppTransId = don?.AppTransId;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> AnhQrLienKetZaloPay(string appTransId)
    {
        var (bytes, contentType, _) = await _api.GetBytesAsync($"/api/zalopay/don/{appTransId}/anh-qr");
        return File(bytes, contentType);
    }

    [HttpGet]
    public async Task<IActionResult> KiemTraLienKetZaloPay(string appTransId)
    {
        var trangThai = await _api.GetAsync<ZaloPayTrangThaiDto>($"/api/zalopay/don/{appTransId}/trang-thai");
        return Json(new { trangThai = trangThai?.TrangThai });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DongBoNgay(int id)
    {
        try
        {
            var res = await _api.PostAsync<ThongBaoResponse>($"/api/integrations/{id}/dong-bo");
            TempData["ThongBao"] = res?.ThongBao;
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyKetNoi(int id)
    {
        await _api.DeleteAsync($"/api/integrations/{id}");
        TempData["ThongBao"] = "Đã hủy liên kết";
        return RedirectToAction(nameof(Index));
    }
}
