using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

var builder = WebApplication.CreateBuilder(args);

// ==== Database (SQL Server) ====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==== Identity (chỉ dùng để quản lý user/role/mật khẩu - KHÔNG dùng cookie, API xác thực bằng JWT) ====
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // Dùng token 6 số (giống OTP) cho xác minh Email VÀ đặt lại mật khẩu, gửi qua email thật (SMTP)
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ==== JWT Bearer - toàn bộ API xác thực bằng token này (Web và Flutter App dùng chung) ====
var jwtConfig = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig["Issuer"],
        ValidAudience = jwtConfig["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

// ==== CORS - cho phép MoneyCare_Web (và bất kỳ origin nào khi phát triển) gọi API này ====
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("MoneyCareClients", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Môi trường phát triển: cho phép mọi origin để không phải sửa cấu hình liên tục
            policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// ==== Đăng ký các Service nghiệp vụ (di chuyển nguyên vẹn từ bản Web) ====
builder.Services.AddScoped<MoneyCare.Services.IAiInsightService, MoneyCare.Services.AiInsightService>();
builder.Services.AddScoped<MoneyCare.Services.IExportService, MoneyCare.Services.ExportService>();
builder.Services.AddScoped<MoneyCare.Services.IQrPaymentService, MoneyCare.Services.QrPaymentService>();
builder.Services.AddScoped<MoneyCare.Services.IOcrService, MoneyCare.Services.TesseractOcrService>();
builder.Services.AddScoped<MoneyCare.Services.IEmailService, MoneyCare.Services.SmtpEmailService>();
builder.Services.AddScoped<MoneyCare.Services.ISmsService, MoneyCare.Services.DemoSmsService>();
builder.Services.AddSingleton<MoneyCare.Services.IChatbotService, MoneyCare.Services.ChatbotService>();
builder.Services.AddHttpClient<MoneyCare.Services.ILlmService, MoneyCare.Services.LmStudioService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddHttpClient<MoneyCare.Services.IZaloPayService, MoneyCare.Services.ZaloPayService>();
builder.Services.AddHttpClient(); // IHttpClientFactory dùng chung (vd AuthController xác thực Google idToken)
builder.Services.AddHostedService<MoneyCare.BackgroundJobs.NhacNhoBackgroundService>();

builder.Services.AddScoped<MoneyCare.Services.IFinancialLinkProvider, MoneyCare.Services.NganHangProviderStub>();
builder.Services.AddScoped<MoneyCare.Services.IFinancialLinkProvider, MoneyCare.Services.MoMoProviderStub>();
builder.Services.AddScoped<MoneyCare.Services.IFinancialLinkProvider, MoneyCare.Services.ZaloPayProvider>();
builder.Services.AddScoped<MoneyCare.Services.FinancialLinkProviderFactory>();

builder.Services.AddScoped<MoneyCare.Services.JwtTokenService>();
builder.Services.AddScoped<MoneyCare.Api.Filters.InternalApiKeyFilter>();

// ==== Controllers - JSON trả về camelCase (khớp với Flutter models.dart / TypeScript-style) ====
// Controllers - JSON trả về camelCase theo mặc định của ASP.NET Core (khớp models.dart bên Flutter).
// QUAN TRỌNG: KHÔNG dùng JsonStringEnumConverter ở đây - toàn bộ enum (LoaiGiaoDich, GioiTinh,
// TanSuatBaoCao, NhaCungCap, LoaiThongBao...) phải serialize dạng SỐ (mặc định của .NET) vì cả
// MoneyCare_App (Flutter) lẫn MoneyCare_Web đều đọc enum dưới dạng số nguyên, không phải chuỗi.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "MoneyCare API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Nhập: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ==== Tự động áp dụng migrations + seed Role Admin/tài khoản quản trị khi khởi động ====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var adminEmail = builder.Configuration["AdminSeed:Email"];
    var adminPassword = builder.Configuration["AdminSeed:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                HoTen = "Quản trị viên"
            };
            await userManager.CreateAsync(adminUser, adminPassword);
        }
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ghi chú: KHÔNG dùng UseHttpsRedirection() ở đây - API này chủ yếu được gọi bởi Web/App (server gọi
// server, không phải trình duyệt), nếu ép redirect sang HTTPS thì HttpClient của Web sẽ đi theo
// redirect và có thể lỗi bắt tay SSL nếu cổng HTTPS không sẵn sàng/chưa tin cậy chứng chỉ dev. Khi
// triển khai thật, hãy bắt buộc HTTPS ở tầng reverse proxy (Nginx/IIS) phía trước thay vì ở đây.
app.UseStaticFiles(); // phục vụ /uploads/avatars, /uploads/backgrounds

app.UseCors("MoneyCareClients");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
