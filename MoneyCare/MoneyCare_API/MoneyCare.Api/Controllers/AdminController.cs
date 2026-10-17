using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;

    // ================== DASHBOARD ==================
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var homNay = DateTime.Today;
        var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);

        var dto = new AdminDashboardDto
        {
            TongNguoiDung = await _userManager.Users.CountAsync(),
            TongNguoiDungPremium = await _userManager.Users.CountAsync(u => u.IsPremium && u.PremiumHetHan >= homNay),
            NguoiDungMoiThangNay = await _userManager.Users.CountAsync(u => u.NgayTao >= dauThangNay),
            TongGiaoDich = await _context.Transactions.CountAsync(),
            TongHoGiaDinh = await _context.Households.CountAsync(),
            TongDoanhThu = (await _context.PremiumOrders.Where(o => o.DaXacNhan).Select(o => o.SoTien).ToListAsync()).Sum(),
            DonChoXacNhan = await _context.PremiumOrders.CountAsync(o => !o.DaXacNhan)
        };

        for (int i = 5; i >= 0; i--)
        {
            var thang = homNay.AddMonths(-i);
            var dau = new DateTime(thang.Year, thang.Month, 1);
            var cuoi = dau.AddMonths(1).AddDays(-1);

            var doanhThu = (await _context.PremiumOrders
                .Where(o => o.DaXacNhan && o.NgayXacNhan >= dau && o.NgayXacNhan <= cuoi)
                .Select(o => o.SoTien).ToListAsync()).Sum();
            var nguoiDungMoi = await _userManager.Users.CountAsync(u => u.NgayTao >= dau && u.NgayTao <= cuoi);

            dto.NhanThang.Add($"Th{thang.Month}/{thang.Year % 100}");
            dto.DoanhThuTheoThang.Add(doanhThu);
            dto.NguoiDungMoiTheoThang.Add(nguoiDungMoi);
        }

        return Ok(dto);
    }

    // ================== USERS ==================
    [HttpGet("users")]
    public async Task<IActionResult> DanhSachUsers(string? tuKhoa)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            query = query.Where(u =>
                (u.HoTen != null && u.HoTen.Contains(tuKhoa)) ||
                (u.Email != null && u.Email.Contains(tuKhoa)) ||
                (u.UserName != null && u.UserName.Contains(tuKhoa)));
        }

        var users = await query.OrderByDescending(u => u.NgayTao).ToListAsync();
        var ds = new List<AdminUserDto>();
        foreach (var u in users)
        {
            ds.Add(new AdminUserDto
            {
                Id = u.Id,
                HoTen = u.HoTen,
                Email = u.Email,
                UserName = u.UserName,
                NgayTao = u.NgayTao,
                IsPremium = u.IsPremium,
                PremiumConHieuLuc = u.PremiumConHieuLuc,
                LaAdmin = await _userManager.IsInRoleAsync(u, "Admin"),
                BiKhoa = await _userManager.IsLockedOutAsync(u),
                DaXacMinhDanhTinh = u.DaXacMinhDanhTinh
            });
        }
        return Ok(ds);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> ChiTietUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var giaoDichs = await _context.Transactions.Where(t => t.UserId == id).ToListAsync();

        return Ok(new AdminUserDetailDto
        {
            Id = user.Id,
            HoTen = user.HoTen,
            Email = user.Email,
            UserName = user.UserName,
            NgayTao = user.NgayTao,
            IsPremium = user.IsPremium,
            PremiumConHieuLuc = user.PremiumConHieuLuc,
            LaAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
            BiKhoa = await _userManager.IsLockedOutAsync(user),
            DaXacMinhDanhTinh = user.DaXacMinhDanhTinh,
            TongThu = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
            TongChi = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien),
            SoGiaoDich = giaoDichs.Count,
            SoDonPremium = await _context.PremiumOrders.CountAsync(o => o.UserId == id && o.DaXacNhan),
            PremiumHetHan = user.PremiumHetHan
        });
    }

    [HttpPost("users/{id}/premium")]
    public async Task<IActionResult> CapNhatPremium(string id, CapNhatPremiumRequest req)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var hetHanHienTai = user.PremiumConHieuLuc ? user.PremiumHetHan!.Value : DateTime.Today;
        user.IsPremium = true;
        user.PremiumHetHan = hetHanHienTai.AddMonths(req.SoThang);
        await _userManager.UpdateAsync(user);
        return Ok();
    }

    [HttpDelete("users/{id}/premium")]
    public async Task<IActionResult> HuyPremium(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsPremium = false;
        user.PremiumHetHan = null;
        await _userManager.UpdateAsync(user);
        return Ok();
    }

    [HttpPost("users/{id}/khoa")]
    public async Task<IActionResult> KhoaTaiKhoan(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return Ok();
    }

    [HttpPost("users/{id}/mo-khoa")]
    public async Task<IActionResult> MoKhoaTaiKhoan(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        await _userManager.SetLockoutEndDateAsync(user, null);
        return Ok();
    }

    [HttpPost("users/{id}/cap-quyen-admin")]
    public async Task<IActionResult> CapQuyenAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (!await _userManager.IsInRoleAsync(user, "Admin")) await _userManager.AddToRoleAsync(user, "Admin");
        return Ok();
    }

    [HttpPost("users/{id}/thu-hoi-quyen-admin")]
    public async Task<IActionResult> ThuHoiQuyenAdmin(string id)
    {
        if (id == CurrentUserId) return BadRequest(new { loi = "Bạn không thể tự thu hồi quyền Admin của chính mình." });
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (await _userManager.IsInRoleAsync(user, "Admin")) await _userManager.RemoveFromRoleAsync(user, "Admin");
        return Ok();
    }

    [HttpPost("users/{id}/xac-minh")]
    public async Task<IActionResult> CapXacMinh(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.DaXacMinhDanhTinh = true;
        user.XacMinhBoiAdmin = true;
        await _userManager.UpdateAsync(user);
        await ThongBaoHelper.TaoAsync(_context, user.Id, "Đã được cấp tích xanh",
            "Quản trị viên vừa xác minh danh tính cho tài khoản của bạn.", LoaiThongBao.XacMinh, "/profile");
        return Ok();
    }

    [HttpDelete("users/{id}/xac-minh")]
    public async Task<IActionResult> HuyXacMinh(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.DaXacMinhDanhTinh = false;
        user.XacMinhBoiAdmin = true;
        await _userManager.UpdateAsync(user);
        await ThongBaoHelper.TaoAsync(_context, user.Id, "Đã bị hủy tích xanh",
            "Quản trị viên vừa hủy xác minh danh tính của tài khoản của bạn.", LoaiThongBao.XacMinh, "/profile");
        return Ok();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> XoaUser(string id)
    {
        if (id == CurrentUserId) return BadRequest(new { loi = "Bạn không thể tự xóa tài khoản của chính mình." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var (thanhCong, loi) = await AdminXoaHelper.XoaNguoiDungAnToanAsync(_context, _userManager, user);
        if (!thanhCong) return BadRequest(new { loi = $"Không thể xóa tài khoản: {loi}" });
        return Ok();
    }

    // ================== HOUSEHOLDS ==================
    [HttpGet("households")]
    public async Task<IActionResult> DanhSachHouseholds(string? tuKhoa)
    {
        var query = _context.Households.Include(h => h.ChuHo).Include(h => h.ThanhVien).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(h => h.TenHoGiaDinh.Contains(tuKhoa) || h.MaMoi.Contains(tuKhoa));

        var ds = await query.OrderByDescending(h => h.NgayTao).ToListAsync();
        return Ok(ds.Select(h => new AdminHouseholdDto
        {
            Id = h.Id,
            TenHoGiaDinh = h.TenHoGiaDinh,
            MaMoi = h.MaMoi,
            TenChuHo = h.ChuHo?.HoTen ?? h.ChuHo?.UserName,
            SoThanhVien = h.ThanhVien?.Count ?? 0,
            NgayTao = h.NgayTao
        }));
    }

    [HttpDelete("households/{id}")]
    public async Task<IActionResult> XoaHousehold(int id)
    {
        var hoGiaDinh = await _context.Households.Include(h => h.ThanhVien).FirstOrDefaultAsync(h => h.Id == id);
        if (hoGiaDinh == null) return NotFound();

        foreach (var tv in hoGiaDinh.ThanhVien ?? new List<ApplicationUser>())
        {
            tv.HouseholdId = null;
            await ThongBaoHelper.TaoAsync(_context, tv.Id, "Hộ gia đình đã bị giải tán",
                $"Quản trị viên đã xóa hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\".", LoaiThongBao.HoGiaDinh);
        }

        _context.Households.Remove(hoGiaDinh);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // ================== PREMIUM ORDERS ==================
    [HttpGet("premium-orders")]
    public async Task<IActionResult> DanhSachPremiumOrders(bool? chuaXacNhan, string? tuKhoa)
    {
        var query = _context.PremiumOrders.Include(o => o.User).AsQueryable();
        if (chuaXacNhan == true) query = query.Where(o => !o.DaXacNhan);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            query = query.Where(o => o.MaGiaoDich.Contains(tuKhoa) ||
                (o.User != null && o.User.Email != null && o.User.Email.Contains(tuKhoa)));
        }

        var ds = await query.OrderByDescending(o => o.NgayTao).ToListAsync();
        return Ok(ds.Select(o => new AdminPremiumOrderDto
        {
            Id = o.Id,
            MaGiaoDich = o.MaGiaoDich,
            SoTien = o.SoTien,
            SoThangDangKy = o.SoThangDangKy,
            DaXacNhan = o.DaXacNhan,
            NgayTao = o.NgayTao,
            EmailNguoiDung = o.User?.Email
        }));
    }

    [HttpPost("premium-orders/{id}/xac-nhan")]
    public async Task<IActionResult> XacNhanDon(int id)
    {
        var don = await _context.PremiumOrders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
        if (don == null) return NotFound();

        if (!don.DaXacNhan)
        {
            don.DaXacNhan = true;
            don.NgayXacNhan = DateTime.Now;

            var user = don.User!;
            var hetHanHienTai = user.PremiumConHieuLuc ? user.PremiumHetHan!.Value : DateTime.Today;
            user.IsPremium = true;
            user.PremiumHetHan = hetHanHienTai.AddMonths(don.SoThangDangKy);

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    [HttpDelete("premium-orders/{id}")]
    public async Task<IActionResult> XoaDon(int id)
    {
        var don = await _context.PremiumOrders.FindAsync(id);
        if (don == null) return NotFound();
        await AdminXoaHelper.XoaPremiumOrderAnToanAsync(_context, don);
        return Ok();
    }

    // ================== CONTACT MESSAGES ==================
    [HttpGet("contact-messages")]
    public async Task<IActionResult> DanhSachContactMessages(string? tuKhoa)
    {
        var query = _context.ContactMessages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
            query = query.Where(t => t.HoTen.Contains(tuKhoa) || t.Email.Contains(tuKhoa));

        var ds = await query.OrderByDescending(t => t.NgayGui).ToListAsync();
        return Ok(ds.Select(t => new AdminContactMessageDto
        {
            Id = t.Id,
            HoTen = t.HoTen,
            Email = t.Email,
            NoiDung = t.NoiDung,
            NgayGui = t.NgayGui,
            DaXem = t.DaXem
        }));
    }

    [HttpPost("contact-messages/{id}/danh-dau-da-xem")]
    public async Task<IActionResult> DanhDauDaXem(int id)
    {
        var tinNhan = await _context.ContactMessages.FindAsync(id);
        if (tinNhan == null) return NotFound();
        tinNhan.DaXem = !tinNhan.DaXem;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("contact-messages/{id}")]
    public async Task<IActionResult> XoaTinNhan(int id)
    {
        var tinNhan = await _context.ContactMessages.FindAsync(id);
        if (tinNhan == null) return NotFound();
        _context.ContactMessages.Remove(tinNhan);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
