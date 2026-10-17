using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly MoneyCareApiClient _api;

    public ProfileController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var dto = await _api.GetAsync<UserDto>("/api/auth/toi");
        return View(UserMapper.TuDto(dto!, _api.BaseUrl));
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _api.GetAsync<UserDto>("/api/auth/toi");
        return View(new EditProfileViewModel
        {
            UserName = user!.Email ?? "",
            HoTen = user.HoTen ?? "",
            NgaySinh = user.NgaySinh,
            GioiTinh = user.GioiTinh,
            CanCuocCongDan = user.CanCuocCongDan
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PutAsync("/api/profile", new CapNhatHoSoRequest
            {
                UserName = model.UserName,
                HoTen = model.HoTen,
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                CanCuocCongDan = model.CanCuocCongDan
            });
            TempData["ThongBao"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult DoiMatKhau() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync("/api/profile/doi-mat-khau", new DoiMatKhauRequest { MatKhauHienTai = model.MatKhauHienTai, MatKhauMoi = model.MatKhauMoi });
            TempData["ThongBao"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ThemSoDienThoai()
    {
        var user = await _api.GetAsync<UserDto>("/api/auth/toi");
        ViewBag.SoHienTai = user?.PhoneNumber;
        return View(new ThemSoDienThoaiViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSoDienThoai(ThemSoDienThoaiViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var user = await _api.GetAsync<UserDto>("/api/auth/toi");
            ViewBag.SoHienTai = user?.PhoneNumber;
            return View(model);
        }

        var res = await _api.PostAsync<Dictionary<string, object?>>("/api/profile/them-so-dien-thoai", new ThemSoDienThoaiRequest { SoDienThoai = model.SoDienThoai });

        TempData["SoDienThoaiChoXacMinh"] = model.SoDienThoai;
        if (res != null && res.TryGetValue("otpDemo", out var otp) && otp != null)
        {
            TempData["OtpDemo"] = otp.ToString();
            TempData["GhiChuDemo"] = "Chưa gửi được email thật - kiểm tra lại cấu hình SMTP trong appsettings.json của MoneyCare_API (mục EmailSettings).";
        }

        return RedirectToAction(nameof(XacMinhSoDienThoai));
    }

    [HttpGet]
    public IActionResult XacMinhSoDienThoai()
    {
        if (TempData.Peek("SoDienThoaiChoXacMinh") == null)
        {
            TempData["Loi"] = "Vui lòng nhập số điện thoại trước";
            return RedirectToAction(nameof(ThemSoDienThoai));
        }
        return View(new XacMinhOtpViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacMinhSoDienThoai(XacMinhOtpViewModel model)
    {
        var soDienThoai = TempData.Peek("SoDienThoaiChoXacMinh") as string;
        if (string.IsNullOrEmpty(soDienThoai))
        {
            TempData["Loi"] = "Phiên xác minh đã hết hạn, vui lòng thử lại";
            return RedirectToAction(nameof(ThemSoDienThoai));
        }

        if (!ModelState.IsValid) { TempData.Keep("SoDienThoaiChoXacMinh"); return View(model); }

        try
        {
            await _api.PostAsync($"/api/profile/xac-minh-so-dien-thoai?soDienThoai={Uri.EscapeDataString(soDienThoai)}", new XacMinhOtpRequest { MaOtp = model.MaOtp });
            TempData["ThongBao"] = "Xác minh số điện thoại thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            TempData.Keep("SoDienThoaiChoXacMinh");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiXacMinhEmail()
    {
        var res = await _api.PostAsync<Dictionary<string, object?>>("/api/profile/gui-xac-minh-email");
        if (res != null && res.TryGetValue("otpDemo", out var otp) && otp != null)
        {
            TempData["OtpDemo"] = otp.ToString();
            TempData["GhiChuDemo"] = "Chưa gửi được email thật - kiểm tra lại cấu hình SMTP trong appsettings.json của MoneyCare_API.";
        }
        else if (res != null && res.TryGetValue("thongBao", out var tb))
        {
            TempData["ThongBao"] = tb?.ToString();
        }
        return RedirectToAction(nameof(XacNhanEmail));
    }

    [HttpGet]
    public IActionResult XacNhanEmail() => View(new XacMinhOtpViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanEmail(XacMinhOtpViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _api.PostAsync("/api/profile/xac-nhan-email", new XacMinhOtpRequest { MaOtp = model.MaOtp });
            TempData["ThongBao"] = "Xác minh email thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiAnhDaiDien(IFormFile anhDaiDien)
    {
        if (anhDaiDien == null || anhDaiDien.Length == 0)
        {
            TempData["Loi"] = "Vui lòng chọn một ảnh";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = anhDaiDien.OpenReadStream();
            await _api.PostFileAsync<Dictionary<string, string>>("/api/profile/doi-anh-dai-dien", "anhDaiDien", stream, anhDaiDien.FileName);
            TempData["ThongBao"] = "Cập nhật ảnh đại diện thành công!";
        }
        catch (ApiException ex)
        {
            TempData["Loi"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
