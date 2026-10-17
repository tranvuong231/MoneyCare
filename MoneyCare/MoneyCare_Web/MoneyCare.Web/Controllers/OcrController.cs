using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Services;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class OcrController : Controller
{
    private readonly MoneyCareApiClient _api;
    private readonly ICurrentUserService _currentUser;

    public OcrController(MoneyCareApiClient api, ICurrentUserService currentUser)
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
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quet(IFormFile anhHoaDon)
    {
        if (anhHoaDon == null || anhHoaDon.Length == 0)
        {
            TempData["Loi"] = "Vui lòng chọn ảnh hóa đơn";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = anhHoaDon.OpenReadStream();
            var ketQuaJson = await _api.PostFileAsync<Dictionary<string, System.Text.Json.JsonElement>>(
                "/api/ocr/quet", "anhHoaDon", stream, anhHoaDon.FileName);

            var ketQua = new KetQuaOcrHoaDon
            {
                VanBanGoc = LayChuoi(ketQuaJson, "vanBanGoc") ?? "",
                SoTienNhanDien = LaySoThapPhan(ketQuaJson, "soTienNhanDien"),
                NgayNhanDien = LayNgay(ketQuaJson, "ngayNhanDien"),
                TenCuaHang = LayChuoi(ketQuaJson, "tenCuaHang"),
                GhiChuGoiY = LayChuoi(ketQuaJson, "ghiChuGoiY"),
                DanhMucGoiYId = (int?)LaySoThapPhan(ketQuaJson, "danhMucGoiYId"),
                DanhMucGoiYTen = LayChuoi(ketQuaJson, "danhMucGoiYTen"),
                LoaiGoiY = (LoaiGiaoDich)(int)(LaySoThapPhan(ketQuaJson, "loaiGoiY") ?? 1),
                NguonPhanTich = LayChuoi(ketQuaJson, "nguonPhanTich") ?? "QuyTac"
            };

            var danhMucs = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
            ViewBag.DanhMucs = danhMucs.Select(CategoryMapper.TuDto).ToList();
            ViewBag.KetQuaOcr = ketQua;

            return View("KetQua", new Transaction
            {
                Loai = ketQua.LoaiGoiY,
                SoTien = ketQua.SoTienNhanDien ?? 0,
                NgayGiaoDich = ketQua.NgayNhanDien ?? DateTime.Today,
                CategoryId = ketQua.DanhMucGoiYId ?? 0,
                GhiChu = ketQua.GhiChuGoiY
            });
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanLuu(Transaction model)
    {
        ModelState.Remove(nameof(Transaction.UserId));
        if (!ModelState.IsValid)
        {
            var danhMucs = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
            ViewBag.DanhMucs = danhMucs.Select(CategoryMapper.TuDto).ToList();
            return View("KetQua", model);
        }

        try
        {
            await _api.PostAsync("/api/ocr/xac-nhan-luu", new TransactionInputDto
            {
                SoTien = model.SoTien,
                Loai = model.Loai,
                NgayGiaoDich = model.NgayGiaoDich,
                GhiChu = model.GhiChu,
                CategoryId = model.CategoryId
            });
            TempData["ThongBao"] = "Đã lưu giao dịch từ hóa đơn quét!";
            return RedirectToAction("Index", "Transactions");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var danhMucs = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
            ViewBag.DanhMucs = danhMucs.Select(CategoryMapper.TuDto).ToList();
            return View("KetQua", model);
        }
    }

    private static string? LayChuoi(Dictionary<string, System.Text.Json.JsonElement>? d, string key) =>
        d != null && d.TryGetValue(key, out var v) && v.ValueKind != System.Text.Json.JsonValueKind.Null ? v.GetString() : null;

    private static decimal? LaySoThapPhan(Dictionary<string, System.Text.Json.JsonElement>? d, string key) =>
        d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number ? v.GetDecimal() : null;

    private static DateTime? LayNgay(Dictionary<string, System.Text.Json.JsonElement>? d, string key) =>
        d != null && d.TryGetValue(key, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetDateTime() : null;
}
