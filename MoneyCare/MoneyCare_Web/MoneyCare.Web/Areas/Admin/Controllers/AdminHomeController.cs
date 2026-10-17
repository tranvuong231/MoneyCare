using Microsoft.AspNetCore.Mvc;
using MoneyCare.Areas.Admin.ViewModels;
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Areas.Admin.Controllers;

public class AdminHomeController : AdminBaseController
{
    private readonly MoneyCareApiClient _api;

    public AdminHomeController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var d = await _api.GetAsync<AdminDashboardDto>("/api/admin/dashboard") ?? new();

        var vm = new AdminDashboardViewModel
        {
            TongNguoiDung = d.TongNguoiDung,
            TongNguoiDungPremium = d.TongNguoiDungPremium,
            NguoiDungMoiThangNay = d.NguoiDungMoiThangNay,
            TongGiaoDich = d.TongGiaoDich,
            TongHoGiaDinh = d.TongHoGiaDinh,
            TongDoanhThu = d.TongDoanhThu,
            DonChoXacNhan = d.DonChoXacNhan,
            NhanThang = d.NhanThang,
            DoanhThuTheoThang = d.DoanhThuTheoThang,
            NguoiDungMoiTheoThang = d.NguoiDungMoiTheoThang
        };

        // Đơn đăng ký gần đây + người dùng mới nhất: lấy từ 2 danh sách đã có sẵn, cắt lấy 8 dòng đầu
        // (nhẹ hơn nhiều so với việc thêm 2 endpoint API chỉ để phục vụ đúng nhu cầu hiển thị này).
        var donGanDay = await _api.GetAsync<List<AdminPremiumOrderDto>>("/api/admin/premium-orders") ?? new();
        vm.DonGanDay = donGanDay.Take(8).Select(o => new PremiumOrder
        {
            Id = o.Id,
            MaGiaoDich = o.MaGiaoDich,
            SoTien = o.SoTien,
            SoThangDangKy = o.SoThangDangKy,
            DaXacNhan = o.DaXacNhan,
            NgayTao = o.NgayTao,
            User = new ApplicationUser { Email = o.EmailNguoiDung }
        }).ToList();

        var users = await _api.GetAsync<List<AdminUserDto>>("/api/admin/users") ?? new();
        vm.NguoiDungMoiNhat = users.Take(8).Select(u => new ApplicationUser
        {
            Id = u.Id,
            HoTen = u.HoTen,
            Email = u.Email,
            UserName = u.UserName,
            NgayTao = u.NgayTao,
            IsPremium = u.IsPremium,
            PremiumHetHan = u.PremiumConHieuLuc ? DateTime.MaxValue : null
        }).ToList();

        return View(vm);
    }
}
