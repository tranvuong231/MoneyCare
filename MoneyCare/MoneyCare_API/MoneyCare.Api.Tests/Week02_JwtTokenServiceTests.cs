// Tuần 2: đăng ký/đăng nhập/đăng xuất (AccountController + ASP.NET Identity), đăng nhập Google, và
// sinh JWT cho Web/App dùng chung (JwtTokenService). Test dưới đây kiểm tra token sinh ra đúng định
// dạng, đúng claim, đúng role, và đúng hạn sử dụng theo cấu hình Jwt:ExpireDays.
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MoneyCare.Models;
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week02_JwtTokenServiceTests
{
    private static JwtTokenService TaoService(int? soNgayHetHan = null)
    {
        var cauHinh = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "khoa-bi-mat-du-dai-de-ky-jwt-trong-moi-truong-kiem-thu-tu-dong",
            ["Jwt:Issuer"] = "MoneyCare.Api.Tests",
            ["Jwt:Audience"] = "MoneyCare.Api.Tests.Client",
        };
        if (soNgayHetHan.HasValue) cauHinh["Jwt:ExpireDays"] = soNgayHetHan.Value.ToString();

        var config = new ConfigurationBuilder().AddInMemoryCollection(cauHinh).Build();
        return new JwtTokenService(config);
    }

    private static ApplicationUser TaoUser() => new()
    {
        Id = "user-001",
        Email = "vuong@moneycare.vn",
        UserName = "vuong@moneycare.vn"
    };

    [Fact]
    public void TaoToken_SinhChuoiJwtHopLe()
    {
        var service = TaoService();
        var token = service.TaoToken(TaoUser(), new List<string>());

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(new JwtSecurityTokenHandler().CanReadToken(token));
    }

    [Fact]
    public void TaoToken_ChuaDungClaimUserIdVaEmail()
    {
        var service = TaoService();
        var user = TaoUser();
        var token = service.TaoToken(user, new List<string>());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }

    [Fact]
    public void TaoToken_DungTenClaimNgan_role_KhongPhaiUriDaiCuaClaimTypesRole()
    {
        // Xem ghi chú trong JwtTokenService: PHẢI dùng "role" (không phải ClaimTypes.Role) để
        // MoneyCare_Web tự đọc quyền Admin từ token hoạt động đúng.
        var service = TaoService();
        var token = service.TaoToken(TaoUser(), new List<string> { "Admin" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public void TaoToken_HanSuDungMacDinh30Ngay_KhiKhongCauHinhExpireDays()
    {
        var service = TaoService(soNgayHetHan: null);
        var truoc = DateTime.UtcNow;
        var token = service.TaoToken(TaoUser(), new List<string>());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var soNgay = (jwt.ValidTo - truoc).TotalDays;

        Assert.InRange(soNgay, 29.9, 30.1);
    }

    [Fact]
    public void TaoToken_ApDungDungSoNgayHetHanDaCauHinh()
    {
        var service = TaoService(soNgayHetHan: 7);
        var truoc = DateTime.UtcNow;
        var token = service.TaoToken(TaoUser(), new List<string>());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var soNgay = (jwt.ValidTo - truoc).TotalDays;

        Assert.InRange(soNgay, 6.9, 7.1);
    }
}
