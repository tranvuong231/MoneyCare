// Tuần 1: khởi tạo solution, thiết kế Model (ApplicationUser, Transaction, Category, Household, Bill...)
// và cấu hình project kiểm thử tự động này. Test dưới đây kiểm tra logic nghiệp vụ thuần trên Model
// ApplicationUser được thiết kế từ tuần 1: tự động bật "tích xanh" và kiểm tra Premium còn hiệu lực.
using MoneyCare.Models;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week01_ApplicationUserTests
{
    [Fact]
    public void CapNhatTichXanh_BatTichXanh_KhiCaEmailVaSdtDaXacNhan()
    {
        var user = new ApplicationUser { EmailConfirmed = true, PhoneNumberConfirmed = true };

        user.CapNhatTichXanh();

        Assert.True(user.DaXacMinhDanhTinh);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void CapNhatTichXanh_KhongBatTichXanh_KhiThieuMotTrongHaiDieuKien(bool email, bool sdt)
    {
        var user = new ApplicationUser { EmailConfirmed = email, PhoneNumberConfirmed = sdt };

        user.CapNhatTichXanh();

        Assert.False(user.DaXacMinhDanhTinh);
    }

    [Fact]
    public void CapNhatTichXanh_KhongGhiDe_KhiDaDuocAdminXacMinhThuCong()
    {
        // Admin đã cấp tích xanh thủ công dù chưa xác nhận email/SĐT -> không được tự động tắt lại.
        var user = new ApplicationUser
        {
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            XacMinhBoiAdmin = true,
            DaXacMinhDanhTinh = true
        };

        user.CapNhatTichXanh();

        Assert.True(user.DaXacMinhDanhTinh);
    }

    [Fact]
    public void PremiumConHieuLuc_TraVeFalse_KhiChuaTungMuaPremium()
    {
        var user = new ApplicationUser { IsPremium = false, PremiumHetHan = null };

        Assert.False(user.PremiumConHieuLuc);
    }

    [Fact]
    public void PremiumConHieuLuc_TraVeTrue_KhiConHanSuDung()
    {
        var user = new ApplicationUser { IsPremium = true, PremiumHetHan = DateTime.Today.AddDays(5) };

        Assert.True(user.PremiumConHieuLuc);
    }

    [Fact]
    public void PremiumConHieuLuc_TraVeFalse_KhiDaHetHan()
    {
        var user = new ApplicationUser { IsPremium = true, PremiumHetHan = DateTime.Today.AddDays(-1) };

        Assert.False(user.PremiumConHieuLuc);
    }
}
