using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;

namespace MoneyCare.Api.Controllers;

[Route("api/thong-bao")]
public class ThongBaoController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public ThongBaoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> DanhSach()
    {
        var ds = await _context.ThongBaos.Where(t => t.UserId == UserId)
            .OrderByDescending(t => t.NgayTao).Take(100).ToListAsync();

        return Ok(new ThongBaoDanhSachResponse
        {
            DanhSach = ds.Select(Dtos.ThongBaoDto.TuModel).ToList(),
            SoChuaXem = ds.Count(t => !t.DaXem)
        });
    }

    [HttpGet("so-chua-xem")]
    public async Task<IActionResult> SoChuaXem()
    {
        var soChuaXem = await _context.ThongBaos.CountAsync(t => t.UserId == UserId && !t.DaXem);
        return Ok(new { soChuaXem });
    }

    [HttpPost("{id}/danh-dau-da-xem")]
    public async Task<IActionResult> DanhDauDaXem(int id)
    {
        var tb = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (tb == null) return NotFound();
        tb.DaXem = true;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/danh-dau-chua-xem")]
    public async Task<IActionResult> DanhDauChuaXem(int id)
    {
        var tb = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (tb == null) return NotFound();
        tb.DaXem = false;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("danh-dau-tat-ca-da-xem")]
    public async Task<IActionResult> DanhDauTatCaDaXem()
    {
        var chuaXem = await _context.ThongBaos.Where(t => t.UserId == UserId && !t.DaXem).ToListAsync();
        foreach (var tb in chuaXem) tb.DaXem = true;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Xoa(int id)
    {
        var tb = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (tb == null) return NotFound();
        _context.ThongBaos.Remove(tb);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
