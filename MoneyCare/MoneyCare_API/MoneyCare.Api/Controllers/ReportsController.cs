using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/reports")]
public class ReportsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> LayBaoCao(DateTime? tuNgay, DateTime? denNgay)
    {
        var homNay = DateTime.Today;
        var tu = tuNgay ?? new DateTime(homNay.Year, homNay.Month, 1);
        var den = denNgay ?? tu.AddMonths(1).AddDays(-1);

        var giaoDichs = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId && t.NgayGiaoDich >= tu.Date && t.NgayGiaoDich < den.Date.AddDays(1))
            .ToListAsync();

        var dto = new ReportDto
        {
            TuNgay = tu,
            DenNgay = den,
            TongThu = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
            TongChi = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien)
        };

        dto.DanhMucChi = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Chi && t.Category != null)
            .GroupBy(t => t.Category)
            .Select(g => new ReportCategorySliceDto { Ten = g.Key!.TenDanhMuc, SoTien = g.Sum(t => t.SoTien), MauSac = g.Key.MauSac })
            .OrderByDescending(x => x.SoTien).ToList();

        dto.DanhMucThu = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Thu && t.Category != null)
            .GroupBy(t => t.Category)
            .Select(g => new ReportCategorySliceDto { Ten = g.Key!.TenDanhMuc, SoTien = g.Sum(t => t.SoTien), MauSac = g.Key.MauSac })
            .OrderByDescending(x => x.SoTien).ToList();

        return Ok(dto);
    }
}
