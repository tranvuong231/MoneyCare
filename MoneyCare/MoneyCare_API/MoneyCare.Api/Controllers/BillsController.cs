using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/bills")]
public class BillsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public BillsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> DanhSach(string? timKiem, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
    {
        var query = _context.Bills.Where(b => b.UserId == UserId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(timKiem))
        {
            query = query.Where(b => b.TenHoaDon.Contains(timKiem) ||
                                      (b.GhiChu != null && b.GhiChu.Contains(timKiem)));
        }

        if (tuNgay.HasValue) query = query.Where(b => b.NgayDenHan >= tuNgay.Value);
        if (denNgay.HasValue) query = query.Where(b => b.NgayDenHan <= denNgay.Value);

        var ds = await query.OrderBy(b => b.DaThanhToan).ThenBy(b => b.NgayDenHan).ToListAsync();

        // Lọc trạng thái sau khi tải về (vì "Quá hạn"/"Chờ thanh toán" phụ thuộc SoNgayConLai - tính
        // theo DateTime.Today lúc chạy, không phải cột lưu sẵn trong DB nên không lọc được ở tầng SQL).
        ds = trangThai switch
        {
            "ChoThanhToan" => ds.Where(b => !b.DaThanhToan && b.SoNgayConLai >= 0).ToList(),
            "QuaHan" => ds.Where(b => !b.DaThanhToan && b.SoNgayConLai < 0).ToList(),
            "DaThanhToan" => ds.Where(b => b.DaThanhToan).ToList(),
            _ => ds
        };

        return Ok(ds.Select(BillDto.TuModel));
    }

    [HttpPost]
    public async Task<IActionResult> Tao(BillInputDto dto)
    {
        var hd = new Bill
        {
            TenHoaDon = dto.TenHoaDon,
            SoTien = dto.SoTien,
            NgayDenHan = dto.NgayDenHan,
            DaThanhToan = dto.DaThanhToan,
            GhiChu = dto.GhiChu,
            UserId = UserId
        };
        _context.Bills.Add(hd);
        await _context.SaveChangesAsync();

        await DongBoGiaoDichTuHoaDonAsync(hd);
        await _context.SaveChangesAsync();

        return Ok(BillDto.TuModel(hd));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Sua(int id, BillInputDto dto)
    {
        var hd = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
        if (hd == null) return NotFound();

        hd.TenHoaDon = dto.TenHoaDon;
        hd.SoTien = dto.SoTien;
        hd.NgayDenHan = dto.NgayDenHan;
        hd.DaThanhToan = dto.DaThanhToan;
        hd.GhiChu = dto.GhiChu;

        await DongBoGiaoDichTuHoaDonAsync(hd);
        await _context.SaveChangesAsync();
        return Ok(BillDto.TuModel(hd));
    }

    [HttpPost("{id}/danh-dau-thanh-toan")]
    public async Task<IActionResult> DanhDauThanhToan(int id)
    {
        var hd = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
        if (hd == null) return NotFound();

        hd.DaThanhToan = !hd.DaThanhToan;
        await DongBoGiaoDichTuHoaDonAsync(hd);
        await _context.SaveChangesAsync();
        return Ok(BillDto.TuModel(hd));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Xoa(int id)
    {
        var hd = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
        if (hd == null) return NotFound();

        var giaoDichLienKet = await _context.Transactions.Where(t => t.BillId == id).ToListAsync();
        foreach (var gd in giaoDichLienKet) gd.BillId = null;

        _context.Bills.Remove(hd);
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Giữ đồng bộ 1-1 giữa Hóa đơn và Giao dịch Chi tự động sinh ra từ nó (xem chi tiết
    /// trong bản gốc web BillsController.cs) - hàm này KHÔNG tự SaveChanges.</summary>
    private async Task DongBoGiaoDichTuHoaDonAsync(Bill hoaDon)
    {
        var giaoDichLienKet = await _context.Transactions
            .FirstOrDefaultAsync(t => t.BillId == hoaDon.Id && t.Nguon == NguonGiaoDich.HoaDon);

        if (!hoaDon.DaThanhToan)
        {
            if (giaoDichLienKet != null) _context.Transactions.Remove(giaoDichLienKet);
            return;
        }

        if (giaoDichLienKet != null)
        {
            giaoDichLienKet.SoTien = hoaDon.SoTien;
            giaoDichLienKet.GhiChu = $"[Hóa đơn] {hoaDon.TenHoaDon}" + (string.IsNullOrWhiteSpace(hoaDon.GhiChu) ? "" : $" - {hoaDon.GhiChu}");
            return;
        }

        var danhMuc = await LayDanhMucHoaDonAsync(hoaDon.UserId);

        _context.Transactions.Add(new Transaction
        {
            SoTien = hoaDon.SoTien,
            Loai = LoaiGiaoDich.Chi,
            NgayGiaoDich = DateTime.Today,
            ThoiGianTao = DateTime.Now,
            GhiChu = $"[Hóa đơn] {hoaDon.TenHoaDon}" + (string.IsNullOrWhiteSpace(hoaDon.GhiChu) ? "" : $" - {hoaDon.GhiChu}"),
            CategoryId = danhMuc.Id,
            UserId = hoaDon.UserId,
            BillId = hoaDon.Id,
            Nguon = NguonGiaoDich.HoaDon,
            TrangThaiDongBo = TrangThaiDongBoGiaoDich.ThanhCong
        });
    }

    private async Task<Category> LayDanhMucHoaDonAsync(string userId)
    {
        var danhMucChi = await _context.Categories
            .Where(c => c.UserId == userId && c.Loai == LoaiGiaoDich.Chi)
            .ToListAsync();

        var uuTien = danhMucChi.FirstOrDefault(c =>
            c.TenDanhMuc.Contains("hóa đơn", StringComparison.OrdinalIgnoreCase) ||
            c.TenDanhMuc.Contains("tiện ích", StringComparison.OrdinalIgnoreCase));
        if (uuTien != null) return uuTien;

        var dauTien = danhMucChi.OrderBy(c => c.TenDanhMuc).FirstOrDefault();
        if (dauTien != null) return dauTien;

        var danhMucMoi = new Category { TenDanhMuc = "Hóa đơn", Loai = LoaiGiaoDich.Chi, MauSac = "#6c757d", UserId = userId };
        _context.Categories.Add(danhMucMoi);
        await _context.SaveChangesAsync();
        return danhMucMoi;
    }
}
