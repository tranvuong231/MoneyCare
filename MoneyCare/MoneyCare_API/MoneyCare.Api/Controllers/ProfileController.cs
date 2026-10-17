using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Dtos;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/profile")]
public class ProfileController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IWebHostEnvironment _env;

    public ProfileController(UserManager<ApplicationUser> userManager, IEmailService emailService, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _emailService = emailService;
        _env = env;
    }

    [HttpPut]
    public async Task<IActionResult> CapNhat(CapNhatHoSoRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var laTaiKhoanGoogle = !await _userManager.HasPasswordAsync(user!);

        var khoaTenDangNhap = laTaiKhoanGoogle || user!.EmailConfirmed;
        if (khoaTenDangNhap && req.UserName != user!.UserName)
            return Loi("Email đã được xác minh (hoặc là tài khoản Google) - không thể đổi tên đăng nhập/email nữa.");

        var khoaHoTen = laTaiKhoanGoogle || user!.DaXacMinhDanhTinh;
        if (khoaHoTen && req.HoTen != user!.HoTen)
            return Loi("Tài khoản đã xác minh danh tính (tích xanh) hoặc đăng nhập Google - không thể đổi họ tên nữa.");

        user!.UserName = req.UserName;
        user.HoTen = req.HoTen;
        user.NgaySinh = req.NgaySinh;
        user.GioiTinh = req.GioiTinh;
        user.CanCuocCongDan = req.CanCuocCongDan;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return Loi(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Ok();
    }

    [HttpPost("doi-mat-khau")]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var result = await _userManager.ChangePasswordAsync(user!, req.MatKhauHienTai, req.MatKhauMoi);
        if (!result.Succeeded) return Loi(string.Join("; ", result.Errors.Select(e => e.Description)));
        return Ok();
    }

    // Gửi mã OTP 6 số (qua email) để xác minh số điện thoại mới
    [HttpPost("them-so-dien-thoai")]
    public async Task<IActionResult> ThemSoDienThoai(ThemSoDienThoaiRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);

        var laDoiSo = !string.IsNullOrEmpty(user!.PhoneNumber) && user.PhoneNumber != req.SoDienThoai;
        if (laDoiSo && user.DaXacMinhDanhTinh)
        {
            user.PhoneNumberConfirmed = false;
            user.DaXacMinhDanhTinh = false;
            await _userManager.UpdateAsync(user);
        }

        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, req.SoDienThoai);

        var noiDungOtp = $@"
            <p>Xin chào {user.HoTen ?? user.Email},</p>
            <p>Mã xác minh số điện thoại <strong>{req.SoDienThoai}</strong> của bạn là:</p>
            <p style='font-size:24px;font-weight:bold;letter-spacing:4px'>{token}</p>
            <p>Nhập mã này để hoàn tất xác minh.</p>";

        var daGuiThat = await _emailService.GuiEmailAsync(user.Email!, "Mã xác minh số điện thoại MoneyCare", noiDungOtp);

        return Ok(new Dictionary<string, object?>
        {
            ["thongBao"] = "Mã xác minh đã được gửi tới email của bạn.",
            ["otpDemo"] = daGuiThat ? null : token
        });
    }

    [HttpPost("xac-minh-so-dien-thoai")]
    public async Task<IActionResult> XacMinhSoDienThoai(string soDienThoai, XacMinhOtpRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var result = await _userManager.ChangePhoneNumberAsync(user!, soDienThoai, req.MaOtp);
        if (!result.Succeeded) return Loi("Mã OTP không đúng hoặc đã hết hạn");

        user!.CapNhatTichXanh();
        await _userManager.UpdateAsync(user);
        return Ok();
    }

    // Gửi mã OTP 6 số (qua email) để xác minh chính email hiện tại
    [HttpPost("gui-xac-minh-email")]
    public async Task<IActionResult> GuiXacMinhEmail()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user!.EmailConfirmed) return Ok(new ThongBaoResponse { ThongBao = "Email của bạn đã được xác minh trước đó." });

        var ma = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var noiDung = $@"
            <p>Xin chào {user.HoTen ?? user.Email},</p>
            <p>Mã xác minh email MoneyCare của bạn là:</p>
            <p style='font-size:24px;font-weight:bold;letter-spacing:4px'>{ma}</p>";

        var daGuiThat = await _emailService.GuiEmailAsync(user.Email!, "Xác minh Email MoneyCare", noiDung);

        return Ok(new Dictionary<string, object?>
        {
            ["thongBao"] = "Mã xác minh đã được gửi tới email của bạn.",
            ["otpDemo"] = daGuiThat ? null : ma
        });
    }

    [HttpPost("xac-nhan-email")]
    public async Task<IActionResult> XacNhanEmail(XacMinhOtpRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var result = await _userManager.ConfirmEmailAsync(user!, req.MaOtp);
        if (!result.Succeeded) return Loi("Mã xác nhận không đúng hoặc đã hết hạn");

        user!.CapNhatTichXanh();
        await _userManager.UpdateAsync(user);
        return Ok();
    }

    private static readonly string[] DinhDangChoPhep = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long DungLuongToiDa = 3 * 1024 * 1024; // 3 MB

    [HttpPost("doi-anh-dai-dien")]
    public async Task<IActionResult> DoiAnhDaiDien(IFormFile anhDaiDien)
    {
        var user = await _userManager.FindByIdAsync(UserId);

        if (!await _userManager.HasPasswordAsync(user!))
            return Loi("Tài khoản đăng nhập bằng Google - ảnh đại diện được đồng bộ tự động, không thể đổi thủ công.");

        if (anhDaiDien == null || anhDaiDien.Length == 0) return Loi("Vui lòng chọn một ảnh");

        var duoi = Path.GetExtension(anhDaiDien.FileName).ToLowerInvariant();
        if (!DinhDangChoPhep.Contains(duoi)) return Loi("Chỉ chấp nhận ảnh định dạng JPG, PNG hoặc WEBP");
        if (anhDaiDien.Length > DungLuongToiDa) return Loi("Ảnh không được vượt quá 3MB");

        var thuMucUploads = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(thuMucUploads);

        if (!string.IsNullOrEmpty(user!.DuongDanAnhDaiDien))
        {
            var duongDanCu = Path.Combine(_env.WebRootPath, user.DuongDanAnhDaiDien.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(duongDanCu)) { try { System.IO.File.Delete(duongDanCu); } catch { } }
        }

        var tenFile = $"{user.Id}_{DateTime.Now.Ticks}{duoi}";
        var duongDanDayDu = Path.Combine(thuMucUploads, tenFile);
        using (var stream = new FileStream(duongDanDayDu, FileMode.Create)) { await anhDaiDien.CopyToAsync(stream); }

        user.DuongDanAnhDaiDien = $"/uploads/avatars/{tenFile}";
        await _userManager.UpdateAsync(user);

        return Ok(new { duongDan = user.DuongDanAnhDaiDien });
    }
}
