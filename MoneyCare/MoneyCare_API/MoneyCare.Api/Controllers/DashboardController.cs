using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

// Endpoint tổng hợp RIÊNG cho trang chủ Dashboard của MoneyCare_Web - gộp sẵn 8 truy vấn thành 1 lần
// gọi API duy nhất thay vì Web phải tự gọi /transactions nhiều lần rồi tự tổng hợp.
[Route("api/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var homNay = DateTime.Today;
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
        var cuoiThang = dauThang.AddMonths(1).AddDays(-1);

        var giaoDichThangNay = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId && t.NgayGiaoDich >= dauThang && t.NgayGiaoDich < cuoiThang.AddDays(1))
            .ToListAsync();

        var chiTheoDanhMuc = giaoDichThangNay
            .Where(t => t.Loai == LoaiGiaoDich.Chi && t.Category != null)
            .GroupBy(t => t.Category)
            .Select(g => new { g.Key!.TenDanhMuc, Tong = g.Sum(t => t.SoTien), g.Key.MauSac })
            .OrderByDescending(x => x.Tong).ToList();

        var nhanThang = new List<string>();
        var thuTheoThang = new List<decimal>();
        var chiTheoThang = new List<decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var thang = homNay.AddMonths(-i);
            var dau = new DateTime(thang.Year, thang.Month, 1);
            var cuoi = dau.AddMonths(1).AddDays(-1);

            var gd = await _context.Transactions
                .Where(t => t.UserId == UserId && t.NgayGiaoDich >= dau && t.NgayGiaoDich < cuoi.AddDays(1))
                .ToListAsync();

            nhanThang.Add($"Th{thang.Month}/{thang.Year % 100}");
            thuTheoThang.Add(gd.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien));
            chiTheoThang.Add(gd.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien));
        }

        var giaoDichGanDay = await _context.Transactions
            .Include(t => t.Category).Include(t => t.User)
            .Where(t => t.UserId == UserId)
            .OrderByDescending(t => t.ThoiGianTao).ThenByDescending(t => t.Id)
            .Take(5).ToListAsync();

        var hoaDonSapToiHan = await _context.Bills
            .Where(b => b.UserId == UserId && !b.DaThanhToan && b.NgayDenHan <= DateTime.Today.AddDays(7))
            .OrderBy(b => b.NgayDenHan).ToListAsync();

        return Ok(new
        {
            tongThu = giaoDichThangNay.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
            tongChi = giaoDichThangNay.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien),
            tenDanhMucChi = chiTheoDanhMuc.Select(x => x.TenDanhMuc),
            soTienTheoDanhMucChi = chiTheoDanhMuc.Select(x => x.Tong),
            mauSacDanhMucChi = chiTheoDanhMuc.Select(x => x.MauSac),
            nhanThang,
            thuTheoThang,
            chiTheoThang,
            giaoDichGanDay = giaoDichGanDay.Select(Dtos.TransactionDto.TuModel),
            hoaDonSapToiHan = hoaDonSapToiHan.Select(Dtos.BillDto.TuModel)
        });
    }
}
