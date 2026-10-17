using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Api.Dtos;
using MoneyCare.Api.Filters;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

[Route("api/household")]
public class HouseholdController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HouseholdController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> ThongTin()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user?.HouseholdId == null) return Ok(null);

        var hoGiaDinh = await _context.Households
            .Include(h => h.ThanhVien)
            .Include(h => h.ChuHo)
            .FirstOrDefaultAsync(h => h.Id == user.HouseholdId);
        if (hoGiaDinh == null) return Ok(null);

        var thanhVienIds = hoGiaDinh.ThanhVien!.Select(t => t.Id).ToList();

        var giaoDichChiaSe = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.User)
            .Where(t => thanhVienIds.Contains(t.UserId) && t.ChiaSeGiaDinh)
            .OrderByDescending(t => t.NgayGiaoDich)
            .ToListAsync();

        var dto = new HouseholdDto
        {
            Id = hoGiaDinh.Id,
            TenHoGiaDinh = hoGiaDinh.TenHoGiaDinh,
            MaMoi = hoGiaDinh.MaMoi,
            TenChuHo = hoGiaDinh.ChuHo?.HoTen ?? hoGiaDinh.ChuHo?.UserName,
            LaChuHo = hoGiaDinh.ChuHoId == user.Id,
            TongThuGiaDinh = giaoDichChiaSe.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
            TongChiGiaDinh = giaoDichChiaSe.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien),
            GiaoDichGanDay = giaoDichChiaSe.Take(15).Select(TransactionDto.TuModel).ToList(),
            ThanhVienChiTieu = giaoDichChiaSe
                .GroupBy(t => t.User!.HoTen ?? t.User!.UserName ?? "N/A")
                .Select(g => new ThanhVienChiTieuDto
                {
                    TenThanhVien = g.Key,
                    TongThu = g.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien),
                    TongChi = g.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien)
                }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost("tao")]
    [RequirePremiumApi]
    public async Task<IActionResult> Tao([FromBody] string tenHoGiaDinh)
    {
        if (string.IsNullOrWhiteSpace(tenHoGiaDinh)) return Loi("Vui lòng nhập tên hộ gia đình");

        var user = await _userManager.FindByIdAsync(UserId);
        if (user!.HouseholdId != null) return Loi("Bạn đã thuộc một hộ gia đình rồi!");

        var hoGiaDinh = new Household { TenHoGiaDinh = tenHoGiaDinh, ChuHoId = user.Id, MaMoi = SinhMaMoi() };
        _context.Households.Add(hoGiaDinh);
        await _context.SaveChangesAsync();

        user.HouseholdId = hoGiaDinh.Id;
        await _userManager.UpdateAsync(user);

        return Ok(new ThongBaoResponse { ThongBao = $"Tạo hộ gia đình thành công! Mã mời: {hoGiaDinh.MaMoi}" });
    }

    [HttpPost("tham-gia")]
    [RequirePremiumApi]
    public async Task<IActionResult> ThamGia([FromBody] string maMoi)
    {
        var hoGiaDinh = await _context.Households.FirstOrDefaultAsync(h => h.MaMoi == maMoi.Trim().ToUpper());
        if (hoGiaDinh == null) return Loi("Mã mời không hợp lệ");

        var user = await _userManager.FindByIdAsync(UserId);
        if (user!.HouseholdId != null) return Loi("Bạn đã thuộc một hộ gia đình rồi!");

        user.HouseholdId = hoGiaDinh.Id;
        await _userManager.UpdateAsync(user);

        return Ok(new ThongBaoResponse { ThongBao = $"Đã tham gia hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\"!" });
    }

    [HttpPost("roi-ho")]
    public async Task<IActionResult> RoiHo()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var hoGiaDinh = await _context.Households.FindAsync(user!.HouseholdId);
        if (hoGiaDinh != null && hoGiaDinh.ChuHoId == user.Id)
            return Loi("Bạn là chủ hộ - hãy dùng chức năng xóa hộ gia đình nếu muốn giải tán.");

        user.HouseholdId = null;
        await _userManager.UpdateAsync(user);

        if (hoGiaDinh != null)
        {
            var tenNguoiRoi = user.HoTen ?? user.UserName ?? user.Email ?? "Một thành viên";
            await ThongBaoHelper.TaoAsync(_context, hoGiaDinh.ChuHoId, "Thành viên đã rời hộ",
                $"{tenNguoiRoi} đã rời khỏi hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\"", LoaiThongBao.HoGiaDinh, "/household");
        }

        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> XoaHoGiaDinh()
    {
        var user = await _userManager.FindByIdAsync(UserId);
        var hoGiaDinh = await _context.Households.Include(h => h.ThanhVien).FirstOrDefaultAsync(h => h.Id == user!.HouseholdId);
        if (hoGiaDinh == null) return Ok();

        if (hoGiaDinh.ChuHoId != user!.Id) return Loi("Chỉ chủ hộ mới có quyền xóa hộ gia đình");

        var thanhVienKhac = hoGiaDinh.ThanhVien!.Where(t => t.Id != user.Id).ToList();
        foreach (var tv in thanhVienKhac)
        {
            tv.HouseholdId = null;
            await ThongBaoHelper.TaoAsync(_context, tv.Id, "Hộ gia đình đã bị giải tán",
                $"Chủ hộ đã xóa hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\".", LoaiThongBao.HoGiaDinh);
        }

        _context.Households.Remove(hoGiaDinh);
        await _context.SaveChangesAsync();
        return Ok();
    }

    private static string SinhMaMoi()
    {
        const string kyTu = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rd = new Random();
        return new string(Enumerable.Range(0, 6).Select(_ => kyTu[rd.Next(kyTu.Length)]).ToArray());
    }
}
