using MoneyCare.Web.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Thiếu cấu hình ApiBaseUrl trong appsettings.json");

builder.Services.AddHttpContextAccessor();

// ==== HttpClient gọi MoneyCare_API - tự động đính kèm JWT lấy từ cookie đăng nhập ====
builder.Services.AddTransient<JwtForwardingHandler>();
builder.Services.AddHttpClient<MoneyCareApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<JwtForwardingHandler>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ==== Xác thực bằng Cookie: Web tự quản lý phiên đăng nhập trên trình duyệt, nhưng dữ liệu người
// dùng/mật khẩu thật sự nằm ở MoneyCare_API. JWT do API cấp được lưu làm 1 claim trong cookie này. ====
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultSignInScheme = "ExternalCookies"; // dùng tạm trong lúc xác thực Google, xem AccountController
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
})
.AddCookie("ExternalCookies");

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = "ExternalCookies";
        options.SaveTokens = true; // để đọc được id_token ở AccountController.GoogleCallback
        // GoogleHandler mặc định KHÔNG tự map claim "picture" (ảnh đại diện) - phải khai báo thủ
        // công thì mới đọc được ở AccountController.GoogleCallback qua ClaimTypes.Uri (Google trả
        // về sẵn, không cần gọi thêm API nào khác).
        options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Uri, "picture");
    });
}

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
