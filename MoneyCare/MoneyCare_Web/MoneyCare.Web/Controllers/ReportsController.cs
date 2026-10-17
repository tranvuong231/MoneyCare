using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public ReportsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
    {
        var homNay = DateTime.Today;
        var tu = tuNgay ?? new DateTime(homNay.Year, homNay.Month, 1);
        var den = denNgay ?? tu.AddMonths(1).AddDays(-1);

        var dto = await _api.GetAsync<ReportDto>($"/api/reports?tuNgay={tu:O}&denNgay={den:O}") ?? new();

        var vm = new ReportViewModel
        {
            TuNgay = dto.TuNgay,
            DenNgay = dto.DenNgay,
            TongThu = dto.TongThu,
            TongChi = dto.TongChi,
            TenDanhMucChi = dto.DanhMucChi.Select(x => x.Ten).ToList(),
            SoTienDanhMucChi = dto.DanhMucChi.Select(x => x.SoTien).ToList(),
            MauSacDanhMucChi = dto.DanhMucChi.Select(x => x.MauSac).ToList(),
            TenDanhMucThu = dto.DanhMucThu.Select(x => x.Ten).ToList(),
            SoTienDanhMucThu = dto.DanhMucThu.Select(x => x.SoTien).ToList(),
            MauSacDanhMucThu = dto.DanhMucThu.Select(x => x.MauSac).ToList()
        };

        return View(vm);
    }
}
