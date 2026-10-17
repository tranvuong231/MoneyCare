using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/transactions")]
public class TransactionsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public TransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> DanhSach(LoaiGiaoDich? loai, int? categoryId, DateTime? tuNgay, DateTime? denNgay)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.User)
            .Where(t => t.UserId == UserId)
            .AsQueryable();

        if (loai.HasValue) query = query.Where(t => t.Loai == loai.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        if (tuNgay.HasValue) query = query.Where(t => t.NgayGiaoDich >= tuNgay.Value.Date);
        // So sánh "< ngày kế tiếp" thay vì "<= denNgay" vì giao dịch đồng bộ tự động (ZaloPay/Ngân
        // hàng/MoMo) và giao dịch quét hóa đơn OCR được lưu KÈM GIỜ:PHÚT:GIÂY thực tế phát sinh
        // (khác giao dịch nhập tay luôn là 00:00:00) - nếu so sánh "<=" thì mọi giao dịch loại này
        // xảy ra đúng ngày denNgay (có giờ > 00:00:00) sẽ bị loại khỏi kết quả dù vẫn được lưu đầy đủ.
        if (denNgay.HasValue) query = query.Where(t => t.NgayGiaoDich < denNgay.Value.Date.AddDays(1));

        var ketQua = await query
            .OrderByDescending(t => t.ThoiGianTao)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(new TransactionDanhSachResponse
        {
            DanhSach = ketQua.Select(TransactionDto.TuModel).ToList(),
            TongThu = ketQua.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
            TongChi = ketQua.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Tao(TransactionInputDto dto)
    {
        var danhMucHopLe = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.UserId == UserId);
        if (!danhMucHopLe) return Loi("Danh mục không hợp lệ");

        var gd = new Transaction
        {
            SoTien = dto.SoTien,
            Loai = dto.Loai,
            NgayGiaoDich = dto.NgayGiaoDich,
            GhiChu = dto.GhiChu,
            CategoryId = dto.CategoryId,
            ChiaSeGiaDinh = dto.ChiaSeGiaDinh,
            UserId = UserId
        };
        _context.Transactions.Add(gd);
        await _context.SaveChangesAsync();

        gd.Category = await _context.Categories.FindAsync(dto.CategoryId);
        return Ok(TransactionDto.TuModel(gd));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Sua(int id, TransactionInputDto dto)
    {
        var gd = await _context.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (gd == null) return NotFound();

        var danhMucHopLe = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.UserId == UserId);
        if (!danhMucHopLe) return Loi("Danh mục không hợp lệ");

        gd.SoTien = dto.SoTien;
        gd.Loai = dto.Loai;
        gd.NgayGiaoDich = dto.NgayGiaoDich;
        gd.GhiChu = dto.GhiChu;
        gd.CategoryId = dto.CategoryId;
        gd.ChiaSeGiaDinh = dto.ChiaSeGiaDinh;
        await _context.SaveChangesAsync();

        gd.Category = await _context.Categories.FindAsync(dto.CategoryId);
        return Ok(TransactionDto.TuModel(gd));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Xoa(int id)
    {
        var gd = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (gd == null) return NotFound();

        _context.Transactions.Remove(gd);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
