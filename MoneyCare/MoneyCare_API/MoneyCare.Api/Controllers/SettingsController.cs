using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Dtos;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/settings")]
public class SettingsController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public SettingsController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    [HttpPut("giao-dien")]
    public async Task<IActionResult> CapNhatGiaoDien(CapNhatGiaoDienRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        user!.GiaoDienToi = req.GiaoDienToi;

        if (req.XoaAnhNen && !string.IsNullOrEmpty(user.DuongDanAnhNen))
        {
            var duongDanCu = Path.Combine(_env.WebRootPath, user.DuongDanAnhNen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(duongDanCu)) { try { System.IO.File.Delete(duongDanCu); } catch { } }
            user.DuongDanAnhNen = null;
        }

        await _userManager.UpdateAsync(user);
        return Ok();
    }

    private static readonly string[] DinhDangAnhChoPhep = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long DungLuongAnhNenToiDa = 5 * 1024 * 1024; // 5 MB

    // Dùng riêng cho Web (Flutter không có tính năng đổi ảnh nền) - tách khỏi PUT /giao-dien vì đây
    // là multipart/form-data, không phải JSON.
    [HttpPost("doi-anh-nen")]
    public async Task<IActionResult> DoiAnhNen(IFormFile anhNen)
    {
        if (anhNen == null || anhNen.Length == 0) return Loi("Vui lòng chọn một ảnh");

        var duoi = Path.GetExtension(anhNen.FileName).ToLowerInvariant();
        if (!DinhDangAnhChoPhep.Contains(duoi)) return Loi("Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP");
        if (anhNen.Length > DungLuongAnhNenToiDa) return Loi("Ảnh nền không được vượt quá 5MB");

        var user = await _userManager.FindByIdAsync(UserId);
        if (!string.IsNullOrEmpty(user!.DuongDanAnhNen))
        {
            var duongDanCu = Path.Combine(_env.WebRootPath, user.DuongDanAnhNen.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(duongDanCu)) { try { System.IO.File.Delete(duongDanCu); } catch { } }
        }

        var thuMuc = Path.Combine(_env.WebRootPath, "uploads", "backgrounds");
        Directory.CreateDirectory(thuMuc);
        var tenFile = $"{user.Id}_{DateTime.Now.Ticks}{duoi}";
        using (var stream = new FileStream(Path.Combine(thuMuc, tenFile), FileMode.Create)) { await anhNen.CopyToAsync(stream); }

        user.DuongDanAnhNen = $"/uploads/backgrounds/{tenFile}";
        await _userManager.UpdateAsync(user);
        return Ok(new { duongDan = user.DuongDanAnhNen });
    }

    [HttpPut("thong-bao")]
    public async Task<IActionResult> CapNhatThongBao(CapNhatThongBaoRequest req)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        user!.BatBaoCaoTaiChinh = req.BatBaoCaoTaiChinh;
        user.BatNhacNhoNhapLieu = req.BatNhacNhoNhapLieu;
        user.TanSuatBaoCao = req.TanSuatBaoCao;
        if (TimeSpan.TryParse(req.GioBaoCao, out var gio)) user.GioBaoCao = gio;

        await _userManager.UpdateAsync(user);
        return Ok();
    }
}
