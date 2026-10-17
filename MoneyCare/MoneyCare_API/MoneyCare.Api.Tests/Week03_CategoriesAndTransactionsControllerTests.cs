// Tuần 3: quản lý danh mục thu/chi (CategoriesController) và quản lý giao dịch thu/chi: thêm, sửa,
// xóa, lọc theo loại/danh mục/thời gian (TransactionsController). Test dùng EF Core InMemory, giả lập
// người dùng đăng nhập qua TestHelpers.DangNhapGia (đọc UserId từ JWT như BaseApiController thật).
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Controllers;
using MoneyCare.Api.Dtos;
using MoneyCare.Models;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week03_CategoriesAndTransactionsControllerTests
{
    private const string UserId = "user-003";

    [Fact]
    public async Task Categories_Xoa_BiChan_KhiDanhMucDangDuocGiaoDichSuDung()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMuc = new Category { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        db.Categories.Add(danhMuc);
        await db.SaveChangesAsync();
        db.Transactions.Add(new Transaction { SoTien = 50000, Loai = LoaiGiaoDich.Chi, CategoryId = danhMuc.Id, UserId = UserId });
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Xoa(danhMuc.Id);

        var objectResult = Assert.IsType<ObjectResult>(ketQua);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task Categories_Xoa_ThanhCong_KhiKhongCoGiaoDichNaoDangDung()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMuc = new Category { TenDanhMuc = "Lương", Loai = LoaiGiaoDich.Thu, UserId = UserId };
        db.Categories.Add(danhMuc);
        await db.SaveChangesAsync();

        var controller = new CategoriesController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Xoa(danhMuc.Id);

        Assert.IsType<OkResult>(ketQua);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Transactions_DanhSach_ChiLocDungTheoLoaiVaDanhMuc()
    {
        await using var db = TestHelpers.TaoDbContext();
        // TransactionsController.DanhSach() có Include(t => t.User): UserId là FK BẮT BUỘC (không
        // nullable) tới ApplicationUser, nên với EF Core InMemory phải có sẵn dòng User tương ứng thì
        // Include mới trả về đúng kết quả (nếu không, InMemory coi như "vi phạm khóa ngoại" và loại
        // bỏ hẳn dòng Transaction đó khỏi kết quả, dù dữ liệu Transaction thực sự đã được lưu).
        db.Users.AddRange(
            new ApplicationUser { Id = UserId, UserName = UserId },
            new ApplicationUser { Id = "nguoi-khac", UserName = "nguoi-khac" });
        var anUong = new Category { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        var luong = new Category { TenDanhMuc = "Lương", Loai = LoaiGiaoDich.Thu, UserId = UserId };
        db.Categories.AddRange(anUong, luong);
        await db.SaveChangesAsync();

        db.Transactions.AddRange(
            new Transaction { SoTien = 100000, Loai = LoaiGiaoDich.Chi, CategoryId = anUong.Id, UserId = UserId, NgayGiaoDich = DateTime.Today },
            new Transaction { SoTien = 10000000, Loai = LoaiGiaoDich.Thu, CategoryId = luong.Id, UserId = UserId, NgayGiaoDich = DateTime.Today },
            new Transaction { SoTien = 20000, Loai = LoaiGiaoDich.Chi, CategoryId = anUong.Id, UserId = "nguoi-khac", NgayGiaoDich = DateTime.Today });
        await db.SaveChangesAsync();

        var controller = new TransactionsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.DanhSach(LoaiGiaoDich.Chi, anUong.Id, null, null);

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var response = Assert.IsType<TransactionDanhSachResponse>(ok.Value);
        Assert.Single(response.DanhSach);
        Assert.Equal(100000, response.TongChi);
        Assert.Equal(0, response.TongThu);
    }

    [Fact]
    public async Task Transactions_Tao_TuChoi_KhiDanhMucKhongThuocVeNguoiDung()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMucNguoiKhac = new Category { TenDanhMuc = "Của người khác", Loai = LoaiGiaoDich.Chi, UserId = "nguoi-khac" };
        db.Categories.Add(danhMucNguoiKhac);
        await db.SaveChangesAsync();

        var controller = new TransactionsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Tao(new TransactionInputDto
        {
            SoTien = 50000,
            Loai = LoaiGiaoDich.Chi,
            NgayGiaoDich = DateTime.Today,
            CategoryId = danhMucNguoiKhac.Id
        });

        var objectResult = Assert.IsType<ObjectResult>(ketQua);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task Transactions_Tao_ThanhCong_LuuDungThongTin()
    {
        await using var db = TestHelpers.TaoDbContext();
        var danhMuc = new Category { TenDanhMuc = "Di chuyển", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        db.Categories.Add(danhMuc);
        await db.SaveChangesAsync();

        var controller = new TransactionsController(db);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Tao(new TransactionInputDto
        {
            SoTien = 30000,
            Loai = LoaiGiaoDich.Chi,
            NgayGiaoDich = DateTime.Today,
            CategoryId = danhMuc.Id,
            GhiChu = "Đổ xăng"
        });

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var dto = Assert.IsType<TransactionDto>(ok.Value);
        Assert.Equal(30000, dto.SoTien);
        Assert.Single(db.Transactions);
    }
}
