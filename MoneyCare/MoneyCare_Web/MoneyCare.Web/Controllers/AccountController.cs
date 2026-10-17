// Đăng ký / đăng nhập / quên mật khẩu phía Web - KHÔNG còn tự kiểm tra mật khẩu hay đụng Database:
// mọi nghiệp vụ xác thực nằm ở MoneyCare_API (AuthController). Ở đây chỉ gọi API lấy JWT rồi lưu
// JWT đó vào 1 Cookie đăng nhập trên trình duyệt (xem JwtForwardingHandler.cs).
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

public class AccountController : Controller
{
    private readonly MoneyCareApiClient _api;

    public AccountController(MoneyCareApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var res = await _api.PostAsync<AuthResponse>("/api/auth/dang-ky", new DangKyRequest
            {
                HoTen = model.HoTen,
                Email = model.Email,
                MatKhau = model.Password
            });

            await DangNhapBangTokenAsync(res!.Token);
            return RedirectToAction("Index", "Home");
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        try
        {
            // Ghi chú: hệ thống mới đăng nhập bằng Email (giống app di động) - trường "Tên đăng nhập"
            // trên giao diện được dùng để nhập Email cho nhất quán với 1 tài khoản duy nhất.
            var res = await _api.PostAsync<AuthResponse>("/api/auth/dang-nhap", new DangNhapRequest
            {
                Email = model.UserName,
                MatKhau = model.Password
            });

            await DangNhapBangTokenAsync(res!.Token, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
        catch (ApiException)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Login");
    }

    // ==== Đăng nhập Google: Web tự làm bước bắt tay OAuth với Google (chỉ để LẤY id_token, không tự
    // tạo tài khoản) rồi gửi id_token đó cho API xác thực + tạo/đăng nhập tài khoản - xem AuthController.DangNhapGoogle ====
    [HttpPost]
    public async Task<IActionResult> DangNhapGoogle(string? returnUrl = null)
    {
        // "Google" chỉ được đăng ký (Program.cs) khi CẢ Authentication:Google:ClientId LẪN ClientSecret
        // đều có trong appsettings.json của MoneyCare_Web - nếu thiếu, báo lỗi rõ ràng thay vì crash.
        var schemeProvider = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        if (await schemeProvider.GetSchemeAsync("Google") == null)
        {
            TempData["Loi"] = "Đăng nhập Google chưa được cấu hình. Cần điền Authentication:Google:ClientId " +
                "VÀ ClientSecret trong appsettings.json của MoneyCare_Web (khác với MoneyCare_API - phải cấu hình cả 2 nơi).";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var ketQuaXacThuc = await HttpContext.AuthenticateAsync("ExternalCookies");
        if (!ketQuaXacThuc.Succeeded || ketQuaXacThuc.Principal == null)
        {
            TempData["Loi"] = "Không lấy được thông tin đăng nhập từ Google";
            return RedirectToAction(nameof(Login));
        }

        // Đọc trực tiếp từ claims mà GoogleHandler đã xác thực sẵn (KHÔNG dùng id_token qua SaveTokens
        // - không phải lúc nào cũng có mặt tùy phiên bản/luồng OAuth). Claims này đáng tin cậy vì Web
        // vừa tự hoàn tất bắt tay OAuth server-side thật sự với Google bằng ClientSecret.
        var email = ketQuaXacThuc.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var ten = ketQuaXacThuc.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var anhDaiDien = ketQuaXacThuc.Principal.FindFirst(System.Security.Claims.ClaimTypes.Uri)?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Loi"] = "Tài khoản Google này không có email công khai để đăng nhập";
            return RedirectToAction(nameof(Login));
        }

        try
        {
            var res = await _api.PostAsync<AuthResponse>("/api/auth/google-web", new DangNhapGoogleWebRequest { Email = email, HoTen = ten, AnhDaiDien = anhDaiDien });
            await HttpContext.SignOutAsync("ExternalCookies");
            await DangNhapBangTokenAsync(res!.Token);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction(nameof(Login));
        }
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PostAsync<Dictionary<string, object?>>("/api/auth/quen-mat-khau", new QuenMatKhauRequest { Email = model.Email });
        TempData["EmailChoDatLai"] = model.Email;
        if (res != null && res.TryGetValue("otpDemo", out var otp) && otp != null)
        {
            // Chế độ demo (SMTP chưa cấu hình): hiển thị luôn mã cho người dùng test được trọn luồng
            TempData["MaDemo"] = otp.ToString();
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email = null)
    {
        var model = new ResetPasswordViewModel { Email = email ?? (string?)TempData.Peek("EmailChoDatLai") ?? "" };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync("/api/auth/dat-lai-mat-khau", new DatLaiMatKhauRequest
            {
                Email = model.Email,
                Ma = model.Token,
                MatKhauMoi = model.MatKhauMoi
            });
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    /// <summary>Đăng nhập cookie phía Web, lưu kèm JWT của API vào 1 claim riêng để
    /// JwtForwardingHandler tự đính kèm header Authorization cho mọi lần gọi API sau này. Đồng thời
    /// đọc sẵn claim "role" bên trong JWT (không cần xác minh chữ ký - việc đó API đã lo) để
    /// [Authorize(Roles = "Admin")] hoạt động được ngay trên các Controller của Web.</summary>
    private async Task DangNhapBangTokenAsync(string jwt, bool ghiNho = false)
    {
        var claims = new List<Claim> { new(JwtForwardingHandler.JwtClaimType, jwt) };
        claims.AddRange(DocRoleTuJwt(jwt).Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
        {
            IsPersistent = ghiNho,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        });
    }

    private static List<string> DocRoleTuJwt(string jwt)
    {
        var ketQua = new List<string>();
        try
        {
            var payload = jwt.Split('.')[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }

            using var doc = System.Text.Json.JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!doc.RootElement.TryGetProperty("role", out var roleEl)) return ketQua;

            if (roleEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                ketQua.AddRange(roleEl.EnumerateArray().Select(r => r.GetString() ?? "").Where(s => s != ""));
            else if (roleEl.GetString() is string s)
                ketQua.Add(s);
        }
        catch { /* JWT không đọc được claim role - coi như không có quyền admin */ }
        return ketQua;
    }
}
