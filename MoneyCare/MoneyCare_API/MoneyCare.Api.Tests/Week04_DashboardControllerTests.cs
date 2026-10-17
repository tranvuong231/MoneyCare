// Tuần 4: Dashboard tổng quan tài chính (DashboardController) - gộp 8 truy vấn (tổng thu/chi tháng
// này, chi theo danh mục, xu hướng 6 tháng, giao dịch gần đây, hóa đơn sắp đến hạn) thành 1 API.
// DashboardController trả về 1 anonymous object -> test đọc giá trị bằng Reflection.
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Controllers;
using MoneyCare.Models;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week04_DashboardControllerTests
{
    private const string UserId = "user-004";

    private static T DocTruong<T>(object obj, string tenTruong)
    {
        var prop = obj.GetType().GetProperty(tenTruong)
            ?? throw new InvalidOperationException($"Không tìm thấy trường {tenTruong}");
        return (T)prop.GetValue(obj)!;
    }

    [Fact]
    public async Task Index_TinhDungTongThuVaTongChi_TrongThangHienTai()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMuc = new Category { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        db.Categories.Add(danhMuc);
        await db.SaveChangesAsync();

        var homNay = DateTime.Today;
        db.Transactions.AddRange(
            new Transaction { SoTien = 15000000, Loai = LoaiGiaoDich.Thu, CategoryId = danhMuc.Id, UserId = UserId, NgayGiaoDich = homNay },
            new Transaction { SoTien = 2000000, Loai = LoaiGiaoDich.Chi, CategoryId = danhMuc.Id, UserId = UserId, NgayGiaoDich = homNay },
            new Transaction { SoTien = 500000, Loai = LoaiGiaoDich.Chi, CategoryId = danhMuc.Id, UserId = UserId, NgayGiaoDich = homNay.AddMonths(-2) });
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Index();

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        Assert.Equal(15000000m, DocTruong<decimal>(ok.Value!, "tongThu"));
        Assert.Equal(2000000m, DocTruong<decimal>(ok.Value!, "tongChi"));
    }

    [Fact]
    public async Task Index_ChiHienThiHoaDonChuaThanhToanVaSapDenHan()
    {
        await using var db = TestHelpers.TaoDbContext();
        db.Bills.AddRange(
            new Bill { TenHoaDon = "Tiền điện", SoTien = 300000, NgayDenHan = DateTime.Today.AddDays(3), DaThanhToan = false, UserId = UserId },
            new Bill { TenHoaDon = "Tiền nước", SoTien = 100000, NgayDenHan = DateTime.Today.AddDays(20), DaThanhToan = false, UserId = UserId },
            new Bill { TenHoaDon = "Internet", SoTien = 200000, NgayDenHan = DateTime.Today.AddDays(1), DaThanhToan = true, UserId = UserId });
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Index();

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var hoaDonSapToiHan = DocTruong<System.Collections.IEnumerable>(ok.Value!, "hoaDonSapToiHan");
        Assert.Single(hoaDonSapToiHan.Cast<object>());
    }

    [Fact]
    public async Task Index_ChiTinhGiaoDichCuaDungNguoiDungDangNhap()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMuc = new Category { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        db.Categories.Add(danhMuc);
        await db.SaveChangesAsync();

        db.Transactions.Add(new Transaction
        {
            SoTien = 999000, Loai = LoaiGiaoDich.Thu, CategoryId = danhMuc.Id, UserId = "nguoi-khac", NgayGiaoDich = DateTime.Today
        });
        await db.SaveChangesAsync();

        var controller = new DashboardController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Index();

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        Assert.Equal(0m, DocTruong<decimal>(ok.Value!, "tongThu"));
    }
}
