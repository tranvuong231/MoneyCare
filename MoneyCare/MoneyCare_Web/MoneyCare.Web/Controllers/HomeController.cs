using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

public class HomeController : Controller
{
    private readonly MoneyCareApiClient _api;
    private readonly IConfiguration _config;

    public HomeController(MoneyCareApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            ViewData["Title"] = "MoneyCare - Quản lý tài chính cá nhân thông minh";
            ViewBag.SupportEmail = _config["SupportEmail"];
            return View("Landing");
        }

        var d = await _api.GetAsync<DashboardApiDto>("/api/dashboard") ?? new();

        var vm = new DashboardViewModel
        {
            TongThu = d.TongThu,
            TongChi = d.TongChi,
            TenDanhMucChi = d.TenDanhMucChi,
            SoTienTheoDanhMucChi = d.SoTienTheoDanhMucChi,
            MauSacDanhMucChi = d.MauSacDanhMucChi,
            NhanThang = d.NhanThang,
            ThuTheoThang = d.ThuTheoThang,
            ChiTheoThang = d.ChiTheoThang,
            GiaoDichGanDay = d.GiaoDichGanDay.Select(TransactionMapper.TuDto).ToList(),
            HoaDonSapToiHan = d.HoaDonSapToiHan.Select(BillMapper.TuDto).ToList()
        };

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private class DashboardApiDto
    {
        public decimal TongThu { get; set; }
        public decimal TongChi { get; set; }
        public List<string> TenDanhMucChi { get; set; } = new();
        public List<decimal> SoTienTheoDanhMucChi { get; set; } = new();
        public List<string> MauSacDanhMucChi { get; set; } = new();
        public List<string> NhanThang { get; set; } = new();
        public List<decimal> ThuTheoThang { get; set; } = new();
        public List<decimal> ChiTheoThang { get; set; } = new();
        public List<TransactionDto> GiaoDichGanDay { get; set; } = new();
        public List<BillDto> HoaDonSapToiHan { get; set; } = new();
    }
}
