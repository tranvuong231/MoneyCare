using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/categories")]
public class CategoriesController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> DanhSach(LoaiGiaoDich? loai)
    {
        var query = _context.Categories.Where(c => c.UserId == UserId).AsQueryable();
        if (loai.HasValue) query = query.Where(c => c.Loai == loai.Value);

        var ds = await query.OrderBy(c => c.Loai).ThenBy(c => c.TenDanhMuc).ToListAsync();
        return Ok(ds.Select(c => new CategoryDto { Id = c.Id, TenDanhMuc = c.TenDanhMuc, Loai = c.Loai, MauSac = c.MauSac }));
    }

    [HttpPost]
    public async Task<IActionResult> Tao(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenDanhMuc)) return Loi("Vui lòng nhập tên danh mục");

        var danhMuc = new Category { TenDanhMuc = dto.TenDanhMuc, Loai = dto.Loai, MauSac = dto.MauSac, UserId = UserId };
        _context.Categories.Add(danhMuc);
        await _context.SaveChangesAsync();

        return Ok(new CategoryDto { Id = danhMuc.Id, TenDanhMuc = danhMuc.TenDanhMuc, Loai = danhMuc.Loai, MauSac = danhMuc.MauSac });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Sua(int id, CategoryDto dto)
    {
        var danhMuc = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (danhMuc == null) return NotFound();

        danhMuc.TenDanhMuc = dto.TenDanhMuc;
        danhMuc.Loai = dto.Loai;
        danhMuc.MauSac = dto.MauSac;
        await _context.SaveChangesAsync();

        return Ok(new CategoryDto { Id = danhMuc.Id, TenDanhMuc = danhMuc.TenDanhMuc, Loai = danhMuc.Loai, MauSac = danhMuc.MauSac });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Xoa(int id)
    {
        var danhMuc = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (danhMuc == null) return NotFound();

        var dangDuocDung = await _context.Transactions.AnyAsync(t => t.CategoryId == id);
        if (dangDuocDung) return Loi("Không thể xóa danh mục đang được sử dụng bởi giao dịch!");

        _context.Categories.Remove(danhMuc);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
