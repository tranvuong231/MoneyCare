// Đăng ký / đăng nhập / quên mật khẩu - trả về JWT dùng chung cho cả Web lẫn App Flutter.
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        JwtTokenService jwtTokenService,
        IEmailService emailService,
        IHttpClientFactory httpClientFactory)
    {
        _userManager = userManager;
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy(DangKyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.MatKhau))
            return BadRequest(new { loi = "Vui lòng nhập đầy đủ Email và mật khẩu" });

        var daTonTai = await _userManager.FindByEmailAsync(req.Email);
        if (daTonTai != null) return BadRequest(new { loi = "Email này đã được đăng ký" });

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            HoTen = req.HoTen
        };

        var result = await _userManager.CreateAsync(user, req.MatKhau);
        if (!result.Succeeded)
            return BadRequest(new { loi = string.Join("; ", result.Errors.Select(e => e.Description)) });

        await TaoDanhMucMacDinhAsync(user.Id);

        var token = _jwtTokenService.TaoToken(user, await _userManager.GetRolesAsync(user));
        return Ok(new AuthResponse { Token = token });
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap(DangNhapRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return BadRequest(new { loi = "Email hoặc mật khẩu không đúng" });

        if (!await _userManager.HasPasswordAsync(user))
            return BadRequest(new { loi = "Tài khoản này đăng nhập bằng Google - hãy dùng nút Đăng nhập với Google" });

        var matKhauDung = await _userManager.CheckPasswordAsync(user, req.MatKhau);
        if (!matKhauDung) return BadRequest(new { loi = "Email hoặc mật khẩu không đúng" });

        var token = _jwtTokenService.TaoToken(user, await _userManager.GetRolesAsync(user));
        return Ok(new AuthResponse { Token = token });
    }

    // Xác thực idToken do Flutter/Web lấy được từ Google Sign-In, sau đó tự tạo/đăng nhập tài khoản
    // tương ứng. Cần cấu hình Authentication:Google:ClientId trong appsettings.json.
    [HttpPost("google")]
    public async Task<IActionResult> DangNhapGoogle(DangNhapGoogleRequest req)
    {
        var clientId = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return BadRequest(new { loi = "Server chưa cấu hình Authentication:Google:ClientId" });

        var http = _httpClientFactory.CreateClient();
        var res = await http.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(req.IdToken)}");
        if (!res.IsSuccessStatusCode)
            return BadRequest(new { loi = "Token Google không hợp lệ hoặc đã hết hạn" });

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var aud = root.TryGetProperty("aud", out var audEl) ? audEl.GetString() : null;
        if (aud != clientId) return BadRequest(new { loi = "Token Google không dành cho ứng dụng này" });

        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        var ten = root.TryGetProperty("name", out var tenEl) ? tenEl.GetString() : null;
        var anhDaiDien = root.TryGetProperty("picture", out var anhEl) ? anhEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { loi = "Tài khoản Google không có email công khai để đăng ký" });

        var user = await TimHoacTaoTaiKhoanGoogleAsync(email, ten, anhDaiDien);
        var token = _jwtTokenService.TaoToken(user, await _userManager.GetRolesAsync(user));
        return Ok(new AuthResponse { Token = token });
    }

    // Dành riêng cho MoneyCare_Web: Web đã tự hoàn tất bắt tay OAuth server-side với Google (dùng
    // ClientSecret bí mật mà chỉ Web giữ), nên KHÔNG cần verify lại id_token - chỉ cần xác nhận
    // request này thật sự đến từ Web (qua InternalApiKey dùng chung, xem InternalApiKeyFilter) rồi
    // tin thẳng email đã xác thực. KHÔNG public API contract này cho client khác (di động/bên thứ 3).
    [HttpPost("google-web")]
    [ServiceFilter(typeof(MoneyCare.Api.Filters.InternalApiKeyFilter))]
    public async Task<IActionResult> DangNhapGoogleWeb(DangNhapGoogleWebRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { loi = "Thiếu email từ Google" });

        var user = await TimHoacTaoTaiKhoanGoogleAsync(req.Email, req.HoTen, req.AnhDaiDien);
        var token = _jwtTokenService.TaoToken(user, await _userManager.GetRolesAsync(user));
        return Ok(new AuthResponse { Token = token });
    }

    private async Task<ApplicationUser> TimHoacTaoTaiKhoanGoogleAsync(string email, string? ten, string? anhDaiDien)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                HoTen = ten,
                EmailConfirmed = true,
                DuongDanAnhDaiDien = anhDaiDien
            };
            var taoKq = await _userManager.CreateAsync(user);
            if (!taoKq.Succeeded)
                throw new InvalidOperationException(string.Join(", ", taoKq.Errors.Select(e => e.Description)));
            await TaoDanhMucMacDinhAsync(user.Id);
        }
        else if (string.IsNullOrWhiteSpace(user.DuongDanAnhDaiDien) && !string.IsNullOrWhiteSpace(anhDaiDien))
        {
            // Chỉ tự điền ảnh Google cho tài khoản CŨ nếu họ chưa có ảnh nào - tránh ghi đè ảnh
            // người dùng đã tự tải lên thủ công ở mỗi lần đăng nhập lại.
            user.DuongDanAnhDaiDien = anhDaiDien;
            await _userManager.UpdateAsync(user);
        }
        return user;
    }

    [Authorize]
    [HttpGet("toi")]
    public async Task<IActionResult> Toi()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var laAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        var laGoogle = !await _userManager.HasPasswordAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            HoTen = user.HoTen,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DaXacMinhDanhTinh = user.DaXacMinhDanhTinh,
            EmailConfirmed = user.EmailConfirmed,
            DuongDanAnhDaiDien = user.DuongDanAnhDaiDien,
            DuongDanAnhNen = user.DuongDanAnhNen,
            GiaoDienToi = user.GiaoDienToi,
            IsPremium = user.IsPremium,
            PremiumConHieuLuc = user.PremiumConHieuLuc,
            PremiumHetHan = user.PremiumHetHan,
            HouseholdId = user.HouseholdId,
            LaAdmin = laAdmin,
            DangNhapBangGoogle = laGoogle,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            NgaySinh = user.NgaySinh,
            GioiTinh = user.GioiTinh,
            CanCuocCongDan = user.CanCuocCongDan,
            BatBaoCaoTaiChinh = user.BatBaoCaoTaiChinh,
            BatNhacNhoNhapLieu = user.BatNhacNhoNhapLieu,
            TanSuatBaoCao = user.TanSuatBaoCao,
            GioBaoCao = user.GioBaoCao.ToString(@"hh\:mm")
        });
    }

    // Gửi mã OTP 6 số qua email để đặt lại mật khẩu (cùng cơ chế OTP dùng cho xác minh email/SĐT).
    [HttpPost("quen-mat-khau")]
    public async Task<IActionResult> QuenMatKhau(QuenMatKhauRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        // Luôn trả về thành công dù email có tồn tại hay không, tránh lộ thông tin tài khoản.
        if (user == null) return Ok(new ThongBaoResponse { ThongBao = "Nếu email tồn tại, mã xác nhận đã được gửi." });

        var ma = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, "ResetPassword");

        var noiDung = $@"
            <p>Xin chào {user.HoTen ?? user.Email},</p>
            <p>Mã xác nhận đặt lại mật khẩu MoneyCare của bạn là:</p>
            <p style='font-size:24px;font-weight:bold;letter-spacing:4px'>{ma}</p>
            <p>Nếu không phải bạn yêu cầu, hãy bỏ qua email này.</p>";

        var daGuiThat = await _emailService.GuiEmailAsync(user.Email!, "Mã đặt lại mật khẩu MoneyCare", noiDung);

        // Chế độ demo (chưa cấu hình SMTP thật): trả kèm mã OTP để vẫn test được luồng đầy đủ.
        return Ok(new Dictionary<string, object?>
        {
            ["thongBao"] = "Mã xác nhận đã được gửi tới email của bạn.",
            ["otpDemo"] = daGuiThat ? null : ma
        });
    }

    [HttpPost("dat-lai-mat-khau")]
    public async Task<IActionResult> DatLaiMatKhau(DatLaiMatKhauRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return BadRequest(new { loi = "Mã xác nhận không đúng hoặc đã hết hạn" });

        var hopLe = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, "ResetPassword", req.Ma);
        if (!hopLe) return BadRequest(new { loi = "Mã xác nhận không đúng hoặc đã hết hạn" });

        // Đặt lại mật khẩu trực tiếp (đã tự xác minh mã ở trên) - xóa mật khẩu cũ rồi thêm mật khẩu mới
        // vì AddPasswordAsync yêu cầu tài khoản đang chưa có mật khẩu (một số tài khoản Google chưa có).
        if (await _userManager.HasPasswordAsync(user))
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                return BadRequest(new { loi = string.Join("; ", removeResult.Errors.Select(e => e.Description)) });
        }

        var addResult = await _userManager.AddPasswordAsync(user, req.MatKhauMoi);
        if (!addResult.Succeeded)
            return BadRequest(new { loi = string.Join("; ", addResult.Errors.Select(e => e.Description)) });

        return Ok();
    }

    private async Task TaoDanhMucMacDinhAsync(string userId)
    {
        var danhMucMacDinh = new List<Category>
        {
            new() { TenDanhMuc = "Lương", Loai = LoaiGiaoDich.Thu, MauSac = "#198754", UserId = userId },
            new() { TenDanhMuc = "Thưởng", Loai = LoaiGiaoDich.Thu, MauSac = "#20c997", UserId = userId },
            new() { TenDanhMuc = "Thu nhập khác", Loai = LoaiGiaoDich.Thu, MauSac = "#0dcaf0", UserId = userId },
            new() { TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, MauSac = "#dc3545", UserId = userId },
            new() { TenDanhMuc = "Di chuyển", Loai = LoaiGiaoDich.Chi, MauSac = "#fd7e14", UserId = userId },
            new() { TenDanhMuc = "Hóa đơn - Tiện ích", Loai = LoaiGiaoDich.Chi, MauSac = "#6f42c1", UserId = userId },
            new() { TenDanhMuc = "Mua sắm", Loai = LoaiGiaoDich.Chi, MauSac = "#d63384", UserId = userId },
            new() { TenDanhMuc = "Giải trí", Loai = LoaiGiaoDich.Chi, MauSac = "#ffc107", UserId = userId },
            new() { TenDanhMuc = "Sức khỏe", Loai = LoaiGiaoDich.Chi, MauSac = "#0d6efd", UserId = userId },
            new() { TenDanhMuc = "Chi tiêu khác", Loai = LoaiGiaoDich.Chi, MauSac = "#6c757d", UserId = userId },
        };

        _context.Categories.AddRange(danhMucMacDinh);
        await _context.SaveChangesAsync();
    }
}
