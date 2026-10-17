using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly MoneyCareApiClient _api;

    public CategoriesController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var ds = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
        return View(ds.Select(CategoryMapper.TuDto).ToList());
    }

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        ModelState.Remove(nameof(Category.UserId));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync<CategoryDto>("/api/categories", new CategoryDto { TenDanhMuc = model.TenDanhMuc, Loai = model.Loai, MauSac = model.MauSac });
            TempData["ThongBao"] = "Thêm danh mục thành công!";
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
        var ds = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
        var dto = ds.FirstOrDefault(c => c.Id == id);
        if (dto == null) return NotFound();
        return View(CategoryMapper.TuDto(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return NotFound();
        ModelState.Remove(nameof(Category.UserId));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PutAsync<CategoryDto>($"/api/categories/{id}", new CategoryDto { TenDanhMuc = model.TenDanhMuc, Loai = model.Loai, MauSac = model.MauSac });
            TempData["ThongBao"] = "Cập nhật danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var ds = await _api.GetAsync<List<CategoryDto>>("/api/categories") ?? new();
        var dto = ds.FirstOrDefault(c => c.Id == id);
        if (dto == null) return NotFound();
        return View(CategoryMapper.TuDto(dto));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _api.DeleteAsync($"/api/categories/{id}");
            TempData["ThongBao"] = "Xóa danh mục thành công!";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
