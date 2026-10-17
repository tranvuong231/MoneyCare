using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Api.Filters;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/integrations")]
[RequirePremiumApi]
public class IntegrationsController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly FinancialLinkProviderFactory _providerFactory;

    public IntegrationsController(ApplicationDbContext context, FinancialLinkProviderFactory providerFactory)
    {
        _context = context;
        _providerFactory = providerFactory;
    }

    [HttpGet]
    public async Task<IActionResult> DanhSach()
    {
        var ds = await _context.FinancialLinks.Where(f => f.UserId == UserId)
            .OrderByDescending(f => f.DaKetNoi).ToListAsync();
        return Ok(ds.Select(FinancialLinkDto.TuModel));
    }

    // Lưu liên kết ở chế độ DEMO (nhập tay) - dùng khi provider thật (Ngân hàng/MoMo) chưa có API Key.
    // ZaloPay dùng luồng QR riêng, xem ZaloPayController.
    [HttpPost]
    public async Task<IActionResult> Tao(FinancialLinkInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenGoiNho)) return Loi("Vui lòng nhập tên gợi nhớ");

        var lienKet = new FinancialLink
        {
            NhaCungCap = dto.NhaCungCap,
            TenGoiNho = dto.TenGoiNho,
            SoTaiKhoanAn = ChePhanSo(dto.SoTaiKhoanDayDu),
            GhiChu = dto.GhiChu,
            UserId = UserId,
            DaKetNoi = true,
            NgayKetNoi = DateTime.Now,
            DongBoLanCuoi = DateTime.Now
        };
        _context.FinancialLinks.Add(lienKet);
        await _context.SaveChangesAsync();
        return Ok(FinancialLinkDto.TuModel(lienKet));
    }

    [HttpPost("{id}/dong-bo")]
    public async Task<IActionResult> DongBoNgay(int id)
    {
        var lienKet = await _context.FinancialLinks.FirstOrDefaultAsync(f => f.Id == id && f.UserId == UserId);
        if (lienKet == null) return NotFound();

        var provider = _providerFactory.LayProvider(lienKet.NhaCungCap);
        try
        {
            var giaoDichMoi = await provider.LayGiaoDichMoiAsync(lienKet, lienKet.DongBoLanCuoi ?? lienKet.NgayKetNoi ?? DateTime.Now.AddDays(-30));

            var danhMucMacDinh = await _context.Categories
                .Where(c => c.UserId == UserId && c.Loai == LoaiGiaoDich.Chi)
                .OrderBy(c => c.TenDanhMuc)
                .FirstOrDefaultAsync();

            if (danhMucMacDinh == null && giaoDichMoi.Count > 0)
            {
                danhMucMacDinh = new Category { TenDanhMuc = "Khác", Loai = LoaiGiaoDich.Chi, MauSac = "#6c757d", UserId = UserId };
                _context.Categories.Add(danhMucMacDinh);
                await _context.SaveChangesAsync();
            }

            var nguon = Enum.Parse<NguonGiaoDich>(lienKet.NhaCungCap.ToString());

            foreach (var gd in giaoDichMoi)
            {
                _context.Transactions.Add(new Transaction
                {
                    SoTien = gd.SoTien,
                    Loai = gd.Loai,
                    NgayGiaoDich = gd.ThoiGian,
                    ThoiGianTao = DateTime.Now,
                    GhiChu = $"[Đồng bộ tự động] {gd.MoTa}",
                    CategoryId = danhMucMacDinh!.Id,
                    UserId = UserId,
                    Nguon = nguon,
                    TrangThaiDongBo = TrangThaiDongBoGiaoDich.ThanhCong
                });
            }

            lienKet.DongBoLanCuoi = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new ThongBaoResponse { ThongBao = $"Đã đồng bộ {giaoDichMoi.Count} giao dịch mới!" });
        }
        catch (NotImplementedException ex)
        {
            return Loi($"Chưa thể đồng bộ tự động: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Huy(int id)
    {
        var lienKet = await _context.FinancialLinks.FirstOrDefaultAsync(f => f.Id == id && f.UserId == UserId);
        if (lienKet == null) return NotFound();

        var provider = _providerFactory.LayProvider(lienKet.NhaCungCap);
        try { await provider.HuyQuyenTruyCapAsync(lienKet); } catch (NotImplementedException) { /* bỏ qua ở chế độ demo */ }

        _context.FinancialLinks.Remove(lienKet);
        await _context.SaveChangesAsync();
        return Ok();
    }

    private static string ChePhanSo(string soDayDu)
    {
        if (string.IsNullOrWhiteSpace(soDayDu) || soDayDu.Length <= 4) return "****";
        return "**** " + soDayDu[^4..];
    }
}
