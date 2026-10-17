// Tuần 5: xuất báo cáo Excel (ClosedXML)/PDF (QuestPDF) (ExportService) và quản lý hóa đơn, nhắc nhở
// thanh toán định kỳ, tự động đồng bộ 1-1 với 1 giao dịch Chi khi đánh dấu "Đã thanh toán" (BillsController).
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Controllers;
using MoneyCare.Api.Dtos;
using MoneyCare.Models;
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week05_ExportAndBillsTests
{
    private const string UserId = "user-005";

    // ==== ExportService ====

    [Fact]
    public void XuatExcel_TraVeFileXlsxHopLe_KhongRong()
    {
        var service = new ExportService();
        var giaoDichs = new List<Transaction>
        {
            new() { SoTien = 100000, Loai = LoaiGiaoDich.Thu, NgayGiaoDich = DateTime.Today, Category = new Category { TenDanhMuc = "Lương" } },
            new() { SoTien = 40000, Loai = LoaiGiaoDich.Chi, NgayGiaoDich = DateTime.Today, Category = new Category { TenDanhMuc = "Ăn uống" } }
        };

        var bytes = service.XuatExcel(giaoDichs, "Trần Quốc Vương", DateTime.Today.AddDays(-7), DateTime.Today);

        Assert.NotEmpty(bytes);
        // File .xlsx là 1 gói ZIP -> 2 byte đầu luôn là "PK" (0x50 0x4B).
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void XuatPdf_TraVeFilePdfHopLe_KhongRong()
    {
        var service = new ExportService();
        var giaoDichs = new List<Transaction>
        {
            new() { SoTien = 200000, Loai = LoaiGiaoDich.Chi, NgayGiaoDich = DateTime.Today, Category = new Category { TenDanhMuc = "Di chuyển" } }
        };

        var bytes = service.XuatPdf(giaoDichs, "Nguyễn Anh Vũ", DateTime.Today.AddDays(-30), DateTime.Today);

        Assert.NotEmpty(bytes);
        // File PDF luôn bắt đầu bằng chữ ký "%PDF".
        var chuKy = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        Assert.Equal("%PDF", chuKy);
    }

    // ==== Bill.SoNgayConLai ====

    [Theory]
    [InlineData(3, false)]
    [InlineData(-2, true)]
    public void Bill_SoNgayConLai_AmNghiaLaQuaHan(int soNgayLech, bool phaiQuaHan)
    {
        var bill = new Bill { NgayDenHan = DateTime.Today.AddDays(soNgayLech) };

        Assert.Equal(phaiQuaHan, bill.SoNgayConLai < 0);
    }

    // ==== BillsController: tự động đồng bộ giao dịch khi đánh dấu đã thanh toán ====

    [Fact]
    public async Task DanhDauThanhToan_TuDongTaoGiaoDichChi_KhiDanhDauDaThanhToan()
    {
        await using var db = TestHelpers.TaoDbContext();
        var bill = new Bill { TenHoaDon = "Tiền điện", SoTien = 350000, NgayDenHan = DateTime.Today.AddDays(2), UserId = UserId, DaThanhToan = false };
        db.Bills.Add(bill);
        await db.SaveChangesAsync();

        var controller = new BillsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        await controller.DanhDauThanhToan(bill.Id);

        var giaoDichSinhRa = Assert.Single(db.Transactions);
        Assert.Equal(bill.Id, giaoDichSinhRa.BillId);
        Assert.Equal(LoaiGiaoDich.Chi, giaoDichSinhRa.Loai);
        Assert.Equal(NguonGiaoDich.HoaDon, giaoDichSinhRa.Nguon);
        Assert.Equal(350000, giaoDichSinhRa.SoTien);
    }

    [Fact]
    public async Task DanhDauThanhToan_XoaGiaoDichLienKet_KhiBoDanhDauDaThanhToan()
    {
        await using var db = TestHelpers.TaoDbContext();
        var bill = new Bill { TenHoaDon = "Internet", SoTien = 220000, NgayDenHan = DateTime.Today.AddDays(5), UserId = UserId, DaThanhToan = true };
        db.Bills.Add(bill);
        await db.SaveChangesAsync();
        db.Transactions.Add(new Transaction
        {
            SoTien = 220000, Loai = LoaiGiaoDich.Chi, UserId = UserId, BillId = bill.Id, Nguon = NguonGiaoDich.HoaDon, CategoryId = 0
        });
        await db.SaveChangesAsync();

        var controller = new BillsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        // Bấm lần nữa để BỎ đánh dấu đã thanh toán
        await controller.DanhDauThanhToan(bill.Id);

        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task DanhSach_LocTrangThaiQuaHan_ChiTraVeHoaDonChuaThanhToanVaDaQuaHan()
    {
        await using var db = TestHelpers.TaoDbContext();
        db.Bills.AddRange(
            new Bill { TenHoaDon = "Đã quá hạn", SoTien = 100000, NgayDenHan = DateTime.Today.AddDays(-3), DaThanhToan = false, UserId = UserId },
            new Bill { TenHoaDon = "Chưa đến hạn", SoTien = 100000, NgayDenHan = DateTime.Today.AddDays(3), DaThanhToan = false, UserId = UserId },
            new Bill { TenHoaDon = "Đã thanh toán dù quá hạn", SoTien = 100000, NgayDenHan = DateTime.Today.AddDays(-3), DaThanhToan = true, UserId = UserId });
        await db.SaveChangesAsync();

        var controller = new BillsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.DanhSach(null, "QuaHan", null, null);

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var ds = Assert.IsAssignableFrom<IEnumerable<BillDto>>(ok.Value);
        Assert.Single(ds);
        Assert.Equal("Đã quá hạn", ds.First().TenHoaDon);
    }
}
