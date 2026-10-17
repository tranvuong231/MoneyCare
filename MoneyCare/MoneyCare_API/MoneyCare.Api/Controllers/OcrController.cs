using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Filters;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/ocr")]
[RequirePremiumApi]
public class OcrController : BaseApiController
{
    private readonly IOcrService _ocrService;
    private readonly ILlmService _llmService;
    private readonly ApplicationDbContext _context;

    public OcrController(IOcrService ocrService, ILlmService llmService, ApplicationDbContext context)
    {
        _ocrService = ocrService;
        _llmService = llmService;
        _context = context;
    }

    [HttpPost("quet")]
    public async Task<IActionResult> Quet(IFormFile anhHoaDon)
    {
        if (anhHoaDon == null || anhHoaDon.Length == 0) return Loi("Vui lòng chọn ảnh hóa đơn");

        try
        {
            using var stream = anhHoaDon.OpenReadStream();
            var ketQua = _ocrService.QuetHoaDon(stream);

            var tatCaDanhMuc = await _context.Categories.Where(c => c.UserId == UserId)
                .OrderBy(c => c.Loai).ThenBy(c => c.TenDanhMuc).ToListAsync();

            await OcrAiHelper.LamGiauKetQuaBangAiAsync(_llmService, ketQua, tatCaDanhMuc);

            return Ok(new
            {
                vanBanGoc = ketQua.VanBanGoc,
                soTienNhanDien = ketQua.SoTienNhanDien,
                ngayNhanDien = ketQua.NgayNhanDien,
                tenCuaHang = ketQua.TenCuaHang,
                ghiChuGoiY = OcrAiHelper.TaoGhiChuMacDinh(ketQua),
                danhMucGoiYId = ketQua.DanhMucGoiYId,
                danhMucGoiYTen = ketQua.DanhMucGoiYTen,
                loaiGoiY = (int)ketQua.LoaiGoiY,
                nguonPhanTich = ketQua.NguonPhanTich
            });
        }
        catch (InvalidOperationException ex)
        {
            return Loi(ex.Message);
        }
    }

    [HttpPost("xac-nhan-luu")]
    public async Task<IActionResult> XacNhanLuu(Dtos.TransactionInputDto dto)
    {
        var danhMucHopLe = await _context.Categories.AnyAsync(
            c => c.Id == dto.CategoryId && c.UserId == UserId && c.Loai == dto.Loai);
        if (!danhMucHopLe) return Loi("Danh mục không hợp lệ hoặc không khớp với loại thu/chi đã chọn");

        _context.Transactions.Add(new Transaction
        {
            SoTien = dto.SoTien,
            Loai = dto.Loai,
            NgayGiaoDich = dto.NgayGiaoDich,
            GhiChu = dto.GhiChu,
            CategoryId = dto.CategoryId,
            UserId = UserId,
            ThoiGianTao = DateTime.Now,
            Nguon = NguonGiaoDich.QuetOcr
        });
        await _context.SaveChangesAsync();
        return Ok();
    }
}
