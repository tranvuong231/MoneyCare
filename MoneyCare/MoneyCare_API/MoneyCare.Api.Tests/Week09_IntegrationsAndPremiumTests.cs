// Tuần 9: khung tích hợp Ngân hàng/MoMo/ZaloPay demo (IntegrationsController - che số tài khoản khi
// hiển thị) và luồng nâng cấp Premium bằng chuyển khoản thủ công (PremiumController - tạo đơn, xác
// nhận thanh toán, cộng dồn hạn sử dụng nếu đang còn Premium).
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MoneyCare.Api.Controllers;
using MoneyCare.Api.Dtos;
using MoneyCare.Models;
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week09_IntegrationsAndPremiumTests
{
    private const string UserId = "user-009";

    // Tao()/DanhSach() không thực sự gọi tới provider nào (chỉ DongBoNgay() mới cần) nên chỉ cần
    // truyền 2 provider stub không phụ thuộc mạng ngoài, tránh phải dựng IZaloPayService (gọi API thật).
    private static FinancialLinkProviderFactory TaoProviderFactory() =>
        new(new List<IFinancialLinkProvider> { new NganHangProviderStub(), new MoMoProviderStub() });

    private static IConfiguration TaoConfigPremium() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PremiumPayment:GiaThang"] = "29000",
            ["PremiumPayment:SoTaiKhoan"] = "0071001234567",
            ["PremiumPayment:BinNganHang"] = "970436",
            ["PremiumPayment:TenNganHangHienThi"] = "Vietcombank"
        }).Build();

    // ==== IntegrationsController ====

    [Fact]
    public async Task Tao_CheSoTaiKhoan_ChiHienThi4SoCuoi()
    {
        await using var db = TestHelpers.TaoDbContext();
        var controller = new IntegrationsController(db, TaoProviderFactory());
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Tao(new FinancialLinkInputDto
        {
            NhaCungCap = NhaCungCap.NganHang,
            TenGoiNho = "TK chính",
            SoTaiKhoanDayDu = "0071001234567"
        });

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var dto = Assert.IsType<FinancialLinkDto>(ok.Value);
        Assert.Equal("**** 4567", dto.SoTaiKhoanAn);
    }

    [Fact]
    public async Task Tao_CheSoTaiKhoan_TraVeSaoSao_KhiSoQuaNgan()
    {
        await using var db = TestHelpers.TaoDbContext();
        var controller = new IntegrationsController(db, TaoProviderFactory());
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.Tao(new FinancialLinkInputDto
        {
            NhaCungCap = NhaCungCap.MoMo,
            TenGoiNho = "Ví MoMo",
            SoTaiKhoanDayDu = "12"
        });

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var dto = Assert.IsType<FinancialLinkDto>(ok.Value);
        Assert.Equal("****", dto.SoTaiKhoanAn);
    }

    [Fact]
    public async Task DanhSach_ChiTraVeLienKetCuaDungNguoiDung()
    {
        await using var db = TestHelpers.TaoDbContext();
        var controllerTao = new IntegrationsController(db, TaoProviderFactory());
        TestHelpers.DangNhapGia(controllerTao, UserId);
        await controllerTao.Tao(new FinancialLinkInputDto { NhaCungCap = NhaCungCap.ZaloPay, TenGoiNho = "Ví của tôi", SoTaiKhoanDayDu = "0912345678" });

        var controllerNguoiKhac = new IntegrationsController(db, TaoProviderFactory());
        TestHelpers.DangNhapGia(controllerNguoiKhac, "nguoi-khac");
        await controllerNguoiKhac.Tao(new FinancialLinkInputDto { NhaCungCap = NhaCungCap.MoMo, TenGoiNho = "Ví người khác", SoTaiKhoanDayDu = "0987654321" });

        var ketQua = await controllerTao.DanhSach();

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var ds = Assert.IsAssignableFrom<IEnumerable<FinancialLinkDto>>(ok.Value);
        Assert.Single(ds);
    }

    // ==== PremiumController ====

    [Fact]
    public async Task DangKyNangCap_TuChoi_KhiDaLaPremium()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        await userManager.CreateAsync(new ApplicationUser
        {
            Id = UserId, UserName = "premium@moneycare.vn", Email = "premium@moneycare.vn",
            IsPremium = true, PremiumHetHan = DateTime.Today.AddDays(10)
        });

        var controller = new PremiumController(db, userManager, TaoConfigPremium());
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.DangKyNangCap(1);

        var objectResult = Assert.IsType<ObjectResult>(ketQua);
        Assert.Equal(400, objectResult.StatusCode);
    }

    [Fact]
    public async Task DangKyNangCap_TaoDonThanhCong_TinhDungSoTienTheoSoThang()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        await userManager.CreateAsync(new ApplicationUser { Id = UserId, UserName = "chuathanoi@moneycare.vn", Email = "chuathanoi@moneycare.vn" });

        var controller = new PremiumController(db, userManager, TaoConfigPremium());
        TestHelpers.DangNhapGia(controller, UserId);

        var ketQua = await controller.DangKyNangCap(3);

        var ok = Assert.IsType<OkObjectResult>(ketQua);
        var dto = Assert.IsType<PremiumOrderDto>(ok.Value);
        Assert.Equal(87000, dto.SoTien);
        Assert.False(dto.DaXacNhan);
    }

    [Fact]
    public async Task XacNhanThanhToan_KichHoatPremium_KhiChuaTungMua()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        var user = new ApplicationUser { Id = UserId, UserName = "moi@moneycare.vn", Email = "moi@moneycare.vn" };
        await userManager.CreateAsync(user);

        var controller = new PremiumController(db, userManager, TaoConfigPremium());
        TestHelpers.DangNhapGia(controller, UserId);
        var donKetQua = await controller.DangKyNangCap(1);
        var donDto = Assert.IsType<PremiumOrderDto>(Assert.IsType<OkObjectResult>(donKetQua).Value);

        await controller.XacNhanThanhToan(donDto.Id);

        var userSauKhiXacNhan = await userManager.FindByIdAsync(UserId);
        Assert.True(userSauKhiXacNhan!.IsPremium);
        Assert.True(userSauKhiXacNhan.PremiumHetHan > DateTime.Today.AddDays(29));
    }

    [Fact]
    public async Task XacNhanThanhToan_CongDonHanSuDung_KhiDangConPremium()
    {
        await using var db = TestHelpers.TaoDbContext();
        var userManager = TestHelpers.TaoUserManager(db);
        var hanHienTai = DateTime.Today.AddDays(10);
        var user = new ApplicationUser
        {
            Id = UserId, UserName = "giahan@moneycare.vn", Email = "giahan@moneycare.vn",
            IsPremium = true, PremiumHetHan = hanHienTai
        };
        await userManager.CreateAsync(user);

        var don = new PremiumOrder { UserId = UserId, MaGiaoDich = "PREMIUMTEST01", SoTien = 29000, SoThangDangKy = 1 };
        db.PremiumOrders.Add(don);
        await db.SaveChangesAsync();

        var controller = new PremiumController(db, userManager, TaoConfigPremium());
        TestHelpers.DangNhapGia(controller, UserId);

        await controller.XacNhanThanhToan(don.Id);

        var userSauKhiXacNhan = await userManager.FindByIdAsync(UserId);
        // Hạn mới PHẢI được cộng dồn từ hạn CŨ (10 ngày nữa + 1 tháng), không phải tính lại từ hôm nay.
        Assert.Equal(hanHienTai.AddMonths(1).Date, userSauKhiXacNhan!.PremiumHetHan!.Value.Date);
    }
}
