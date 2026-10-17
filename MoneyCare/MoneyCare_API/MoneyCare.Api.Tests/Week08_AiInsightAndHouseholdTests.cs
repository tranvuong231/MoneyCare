// Tuần 8: "AI phân tích tài chính" thực chất là thuật toán thống kê/hồi quy tuyến tính chạy offline
// trên dữ liệu giao dịch (AiInsightService), và quản lý tài chính gia đình - tạo/tham gia bằng mã mời,
// chia sẻ giao dịch trong hộ (HouseholdController). HouseholdController cần UserManager<ApplicationUser>
// thật (dựng qua Identity + EF InMemory, xem TestHelpers.TaoUserManager) vì gọi UserManager.FindByIdAsync.
using MoneyCare.Api.Controllers;
using MoneyCare.Models;
using MoneyCare.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week08_AiInsightAndHouseholdTests
{
    private const string UserId = "user-008";

    // ==== AiInsightService ====

    [Fact]
    public async Task PhanTichAsync_ChuaCoGiaoDichChi_TraVeCanhBaoChuaDuDuLieu()
    {
        await using var db = TestHelpers.TaoDbContext();
        var service = new AiInsightService(db);

        var ketQua = await service.PhanTichAsync(UserId);

        var canhBao = Assert.Single(ketQua.ThoiQuenChiTieu);
        Assert.Equal("Chưa đủ dữ liệu", canhBao.TieuDe);
    }

    [Fact]
    public async Task PhanTichAsync_PhatHienDanhMucChiNhieuNhat_ThangNay()
    {
        await using var db = TestHelpers.TaoDbContext();
        var anUong = new Category { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        var diChuyen = new Category { TenDanhMuc = "Di chuyển", Loai = LoaiGiaoDich.Chi, UserId = UserId };
        db.Categories.AddRange(anUong, diChuyen);
        await db.SaveChangesAsync();

        var dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        db.Transactions.AddRange(
            new Transaction { SoTien = 4000000, Loai = LoaiGiaoDich.Chi, CategoryId = anUong.Id, UserId = UserId, NgayGiaoDich = dauThang.AddDays(2) },
            new Transaction { SoTien = 1000000, Loai = LoaiGiaoDich.Chi, CategoryId = diChuyen.Id, UserId = UserId, NgayGiaoDich = dauThang.AddDays(3) });
        await db.SaveChangesAsync();

        var service = new AiInsightService(db);
        var ketQua = await service.PhanTichAsync(UserId);

        Assert.Contains(ketQua.ThoiQuenChiTieu, i => i.TieuDe.Contains("Ăn uống"));
    }

    [Fact]
    public async Task PhanTichAsync_TraVeDu6ThangThucTeCongThemMocDuBaoThangKeTiep()
    {
        // TinhXuHuongVaDuBao sinh 6 tháng thực tế (i=5..0) RỒI NỐI THÊM 1 mốc "dự báo" cho tháng kế
        // tiếp (hồi quy tuyến tính) -> cả NhanThang lẫn ChiTieuThucTe đều có 7 phần tử (không phải 6),
        // riêng phần tử cuối của ChiTieuThucTe chỉ là số 0 giữ chỗ cho 2 mảng luôn cùng độ dài.
        await using var db = TestHelpers.TaoDbContext();
        var service = new AiInsightService(db);

        var ketQua = await service.PhanTichAsync(UserId);

        Assert.Equal(7, ketQua.NhanThang.Count);
        Assert.Equal(7, ketQua.ChiTieuThucTe.Count);
        Assert.Contains("dự báo", ketQua.NhanThang.Last());
    }

    [Fact]
    public async Task PhanTichAsync_DuDoanSoDuHienTai_BangThuTruDiChi()
    {
        await using var db = TestHelpers.TaoDbContext();
        var luong = new Category { TenDanhMuc = "Lương", Loai = LoaiGiaoDich.Thu, UserId = UserId };
        db.Categories.Add(luong);
        await db.SaveChangesAsync();

        var dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        db.Transactions.Add(new Transaction { SoTien = 10000000, Loai = LoaiGiaoDich.Thu, CategoryId = luong.Id, UserId = UserId, NgayGiaoDich = dauThang.AddDays(1) });
        await db.SaveChangesAsync();

        var service = new AiInsightService(db);
        var ketQua = await service.PhanTichAsync(UserId);

        Assert.Equal(10000000m, ketQua.DuDoan.SoDuHienTai);
    }

    // ==== HouseholdController ====

    [Fact]
    public async Task Tao_TaoHoGiaDinhMoi_GanNguoiTaoLaChuHo()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        await userManager.CreateAsync(new ApplicationUser { Id = UserId, UserName = "chuho@moneycare.vn", Email = "chuho@moneycare.vn" });

        var controller = new HouseholdController(db, userManager);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Tao("Gia đình Trần Quốc Vương");

        Assert.IsType<OkObjectResult>(ketQua);
        var hoGiaDinh = Assert.Single(db.Households);
        Assert.Equal(UserId, hoGiaDinh.ChuHoId);
        Assert.False(string.IsNullOrWhiteSpace(hoGiaDinh.MaMoi));
    }

    [Fact]
    public async Task ThamGia_ThatBai_KhiMaMoiKhongTonTai()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        await userManager.CreateAsync(new ApplicationUser { Id = UserId, UserName = "thanhvien@moneycare.vn", Email = "thanhvien@moneycare.vn" });

        var controller = new HouseholdController(db, userManager);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.ThamGia("MAMOI-KHONG-TON-TAI");

        var objectResult = Assert.IsType<ObjectResult>(ketQua);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task ThamGia_ThanhCong_GanUserVaoDungHoGiaDinh()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        var chuHo = new ApplicationUser { Id = "user-008-chuho", UserName = "chuho2@moneycare.vn", Email = "chuho2@moneycare.vn" };
        var thanhVien = new ApplicationUser { Id = UserId, UserName = "thanhvien2@moneycare.vn", Email = "thanhvien2@moneycare.vn" };
        await userManager.CreateAsync(chuHo);
        await userManager.CreateAsync(thanhVien);

        var hoGiaDinh = new Household { TenHoGiaDinh = "Gia đình A", ChuHoId = chuHo.Id, MaMoi = "ABC12345" };
        db.Households.Add(hoGiaDinh);
        await db.SaveChangesAsync();

        var controller = new HouseholdController(db, userManager);
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.ThamGia("ABC12345");

        Assert.IsType<OkObjectResult>(ketQua);
        var thanhVienSauKhiThamGia = await db.Users.FindAsync(UserId);
        Assert.Equal(hoGiaDinh.Id, thanhVienSauKhiThamGia!.HouseholdId);
    }
}
