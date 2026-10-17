using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public TransactionsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(LoaiGiaoDich? loai, int? categoryId, DateTime? tuNgay, DateTime? denNgay)
    {
        var query = new List<string>();
        if (loai.HasValue) query.Add($"loai={(int)loai.Value}");
        if (categoryId.HasValue) query.Add($"categoryId={categoryId.Value}");
        if (tuNgay.HasValue) query.Add($"tuNgay={tuNgay.Value:O}");
        if (denNgay.HasValue) query.Add($"denNgay={denNgay.Value:O}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

        var res = await _api.GetAsync<TransactionDanhSachResponse>($"/api/transactions{queryString}") ?? new();
        var danhMucs = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();

        // Truyền cả danh sách Category gốc (còn giữ Loai) để View tự lọc Danh mục theo Loại đã chọn
        // bằng JS (data-loai) - dùng SelectList sẽ mất field Loai, không lọc client-side được.
        ViewBag.DanhMucs = danhMucs.Select(CategoryMapper.TuDto).ToList();
        ViewBag.Loai = loai;
        ViewBag.CategoryId = categoryId;
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
        ViewBag.TongThu = res.TongThu;
        ViewBag.TongChi = res.TongChi;

        return View(res.DanhSach.Select(TransactionMapper.TuDto).ToList());
    }

    public async Task<IActionResult> Create()
    {
        await NapDanhSachDanhMuc();
        return View(new Transaction { NgayGiaoDich = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction model)
    {
        ModelState.Remove(nameof(Transaction.UserId));
        if (!ModelState.IsValid) { await NapDanhSachDanhMuc(); return View(model); }

        try
        {
            await _api.PostAsync<TransactionDto>("/api/transactions", new TransactionInputDto
            {
                SoTien = model.SoTien,
                Loai = model.Loai,
                NgayGiaoDich = model.NgayGiaoDich,
                GhiChu = model.GhiChu,
                CategoryId = model.CategoryId,
                ChiaSeGiaDinh = model.ChiaSeGiaDinh
            });
            TempData["ThongBao"] = "Thêm giao dịch thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await NapDanhSachDanhMuc();
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var res = await _api.GetAsync<TransactionDanhSachResponse>("/api/transactions");
        var dto = res?.DanhSach.FirstOrDefault(t => t.Id == id);
        if (dto == null) return NotFound();
        await NapDanhSachDanhMuc();
        return View(TransactionMapper.TuDto(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Transaction model)
    {
        if (id != model.Id) return NotFound();
        ModelState.Remove(nameof(Transaction.UserId));
        if (!ModelState.IsValid) { await NapDanhSachDanhMuc(); return View(model); }

        try
        {
            await _api.PutAsync<TransactionDto>($"/api/transactions/{id}", new TransactionInputDto
            {
                SoTien = model.SoTien,
                Loai = model.Loai,
                NgayGiaoDich = model.NgayGiaoDich,
                GhiChu = model.GhiChu,
                CategoryId = model.CategoryId,
                ChiaSeGiaDinh = model.ChiaSeGiaDinh
            });
            TempData["ThongBao"] = "Cập nhật giao dịch thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await NapDanhSachDanhMuc();
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var res = await _api.GetAsync<TransactionDanhSachResponse>("/api/transactions");
        var dto = res?.DanhSach.FirstOrDefault(t => t.Id == id);
        if (dto == null) return NotFound();
        return View(TransactionMapper.TuDto(dto));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _api.DeleteAsync($"/api/transactions/{id}");
        TempData["ThongBao"] = "Xóa giao dịch thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task NapDanhSachDanhMuc()
    {
        var danhMucs = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
        ViewBag.DanhMucs = danhMucs.Select(CategoryMapper.TuDto).ToList();
    }
}
