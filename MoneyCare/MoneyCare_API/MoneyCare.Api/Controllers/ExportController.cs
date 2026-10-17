using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Filters;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/export")]
[RequirePremiumApi]
public class ExportController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IExportService _exportService;

    public ExportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IExportService exportService)
    {
        _context = context;
        _userManager = userManager;
        _exportService = exportService;
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(DateTime tuNgay, DateTime denNgay)
    {
        var (giaoDichs, user) = await LayDuLieuAsync(tuNgay, denNgay);
        var bytes = _exportService.XuatExcel(giaoDichs, user.HoTen ?? user.UserName ?? "", tuNgay, denNgay);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoThuChi_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx");
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(DateTime tuNgay, DateTime denNgay)
    {
        var (giaoDichs, user) = await LayDuLieuAsync(tuNgay, denNgay);
        var bytes = _exportService.XuatPdf(giaoDichs, user.HoTen ?? user.UserName ?? "", tuNgay, denNgay);
        return File(bytes, "application/pdf", $"BaoCaoThuChi_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.pdf");
    }

    private async Task<(List<Transaction>, ApplicationUser)> LayDuLieuAsync(DateTime tuNgay, DateTime denNgay)
    {
        var user = (await _userManager.FindByIdAsync(UserId))!;
        var giaoDichs = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == UserId && t.NgayGiaoDich >= tuNgay.Date && t.NgayGiaoDich < denNgay.Date.AddDays(1))
            .OrderBy(t => t.NgayGiaoDich)
            .ToListAsync();
        return (giaoDichs, user);
    }
}
