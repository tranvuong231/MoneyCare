using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

[Route("api/premium")]
public class PremiumController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public PremiumController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
    }

    [HttpGet("bang-gia")]
    public IActionResult BangGia() => Ok(new PremiumPricingDto
    {
        GiaThang = _config.GetValue<decimal>("PremiumPayment:GiaThang"),
        SoTaiKhoan = _config["PremiumPayment:SoTaiKhoan"] ?? "",
        BinNganHang = _config["PremiumPayment:BinNganHang"] ?? "",
        TenNganHangHienThi = _config["PremiumPayment:TenNganHangHienThi"] ?? ""
    });

    [HttpPost("nang-cap")]
    public async Task<IActionResult> DangKyNangCap(int soThang = 1)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user!.PremiumConHieuLuc) return Loi("Tài khoản của bạn đang là Premium rồi!");

        var giaThang = _config.GetValue<decimal>("PremiumPayment:GiaThang");
        var soTien = giaThang * soThang;
        var maGiaoDich = "PREMIUM" + user.Id.Substring(0, Math.Min(6, user.Id.Length)).ToUpper() + DateTime.Now.ToString("HHmmss");

        var don = new PremiumOrder { UserId = user.Id, MaGiaoDich = maGiaoDich, SoTien = soTien, SoThangDangKy = soThang };
        _context.PremiumOrders.Add(don);
        await _context.SaveChangesAsync();

        return Ok(new PremiumOrderDto
        {
            Id = don.Id,
            MaGiaoDich = don.MaGiaoDich,
            SoTien = don.SoTien,
            SoThangDangKy = don.SoThangDangKy,
            DaXacNhan = don.DaXacNhan,
            NgayTao = don.NgayTao,
            SoTaiKhoan = _config["PremiumPayment:SoTaiKhoan"] ?? "",
            BinNganHang = _config["PremiumPayment:BinNganHang"] ?? "",
            TenNganHangHienThi = _config["PremiumPayment:TenNganHangHienThi"] ?? ""
        });
    }

    // Trong thực tế bước này sẽ được thay bằng webhook tự động từ cổng thanh toán/ngân hàng xác nhận
    // giao dịch theo "MaGiaoDich" (nội dung chuyển khoản). Ở đây làm demo xác nhận thủ công (chuyển
    // khoản ngân hàng thường) - thanh toán ZaloPay thật thì tự động xác nhận qua ZaloPayController.
    [HttpPost("xac-nhan-thanh-toan/{donId}")]
    public async Task<IActionResult> XacNhanThanhToan(int donId)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var don = await _context.PremiumOrders.FirstOrDefaultAsync(d => d.Id == donId && d.UserId == user!.Id);
        if (don == null) return NotFound();

        if (!don.DaXacNhan)
        {
            don.DaXacNhan = true;
            don.NgayXacNhan = DateTime.Now;

            var hetHanHienTai = user!.PremiumConHieuLuc ? user.PremiumHetHan!.Value : DateTime.Today;
            user.IsPremium = true;
            user.PremiumHetHan = hetHanHienTai.AddMonths(don.SoThangDangKy);

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
        }

        return Ok(new ThongBaoResponse { ThongBao = $"Nâng cấp Premium thành công! Hiệu lực đến {user!.PremiumHetHan:dd/MM/yyyy}." });
    }
}
