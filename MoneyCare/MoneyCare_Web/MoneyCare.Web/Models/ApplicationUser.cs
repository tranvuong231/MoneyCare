// Bản dành cho MoneyCare_Web: KHÔNG kế thừa IdentityUser (Web không kết nối Database/Identity Store
// trực tiếp nữa - toàn bộ đọc/ghi user đi qua MoneyCare_API). Khai báo lại đúng các thuộc tính Views
// đang dùng để toàn bộ Views/*.cshtml giữ nguyên không cần sửa.
namespace MoneyCare.Models;

public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }

    public string? HoTen { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public string? DuongDanAnhDaiDien { get; set; }
    public DateTime? NgaySinh { get; set; }
    public GioiTinh? GioiTinh { get; set; }
    public string? CanCuocCongDan { get; set; }
    public bool DaXacMinhDanhTinh { get; set; }
    public bool XacMinhBoiAdmin { get; set; }
    public bool GiaoDienToi { get; set; }
    public string? DuongDanAnhNen { get; set; }
    public bool BatBaoCaoTaiChinh { get; set; }
    public bool BatNhacNhoNhapLieu { get; set; }
    public TanSuatBaoCao TanSuatBaoCao { get; set; } = TanSuatBaoCao.HangTuan;
    public TimeSpan GioBaoCao { get; set; } = new TimeSpan(8, 0, 0);
    public bool IsPremium { get; set; }
    public DateTime? PremiumHetHan { get; set; }
    public bool PremiumConHieuLuc => IsPremium && PremiumHetHan.HasValue && PremiumHetHan.Value >= DateTime.Today;
    public int? HouseholdId { get; set; }

    // Chỉ dùng ở Web (không có trong API): tài khoản đăng nhập bằng Google thì không có mật khẩu nội bộ
    public bool DangNhapBangGoogle { get; set; }
}
