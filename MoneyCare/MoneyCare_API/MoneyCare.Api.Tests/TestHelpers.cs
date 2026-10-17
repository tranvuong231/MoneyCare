// Các hàm dùng chung cho toàn bộ project test: dựng ApplicationDbContext bằng EF Core InMemory
// (mỗi test 1 database riêng biệt theo Guid, tránh test này ảnh hưởng test kia khi chạy song song),
// giả lập người dùng đã đăng nhập (ControllerContext.HttpContext.User) để test các Controller kế thừa
// BaseApiController, và dựng UserManager<ApplicationUser> thật (không mock) khi cần test các luồng
// có đụng tới Identity (Household, Premium...).
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Tests;

public static class TestHelpers
{
    /// <summary>Tạo 1 ApplicationDbContext InMemory hoàn toàn mới (database riêng, đặt tên theo Guid).</summary>
    public static ApplicationDbContext TaoDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Gán ControllerContext với 1 người dùng "đăng nhập" có UserId cho trước, để các Controller
    /// kế thừa BaseApiController (đọc UserId từ ClaimTypes.NameIdentifier) hoạt động được trong test.</summary>
    public static void DangNhapGia(ControllerBase controller, string userId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    /// <summary>Dựng 1 UserManager&lt;ApplicationUser&gt; THẬT (không mock) chạy trên cùng ApplicationDbContext
    /// InMemory truyền vào, dùng cho các test đụng tới Identity (VD HouseholdController, PremiumController)
    /// mà không cần kết nối SQL Server thật.</summary>
    public static UserManager<ApplicationUser> TaoUserManager(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddLogging();
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<UserManager<ApplicationUser>>();
    }
}
