//Thông tin người dùng
using Microsoft.AspNetCore.Identity; // Identity là hệ thống quản lý dn/dk/mk,..

namespace MoneyCare.Models;

// Mở rộng IdentityUser để lưu thêm thông tin Họ tên, gói Premium, hộ gia đình
public class ApplicationUser : IdentityUser
{
    public string? HoTen { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Đường dẫn tương đối tới ảnh đại diện đã tải lên (VD: /uploads/avatars/{userId}.jpg)
    public string? DuongDanAnhDaiDien { get; set; }

    // Hồ sơ cá nhân bổ sung 
    public DateTime? NgaySinh { get; set; }
    public GioiTinh? GioiTinh { get; set; }
    public string? CanCuocCongDan { get; set; }

    // Xác minh danh tính (tích xanh): mặc định TỰ ĐỘNG bật khi CẢ HAI email và số điện thoại
    // đã được xác nhận (xem CapNhatTichXanh() bên dưới). Đây là cột thật trong database (không
    // còn là thuộc tính tính toán) để quản trị viên có thể cấp/hủy thủ công.
    public bool DaXacMinhDanhTinh { get; set; } = false;

    // true nếu tài khoản này được xác minh danh tính TỪ QUẢN TRỊ VIÊN (không phải tự động qua
    // email+SĐT) - dùng để tránh bị logic tự động ghi đè lại thành false khi có thay đổi khác.
    public bool XacMinhBoiAdmin { get; set; } = false;

    // Tự động cập nhật tích xanh: bật khi CẢ HAI email và số điện thoại đã xác nhận, trừ khi
    // quản trị viên đã can thiệp thủ công (XacMinhBoiAdmin). Gọi sau mỗi lần EmailConfirmed hoặc
    // PhoneNumberConfirmed có thể đã thay đổi (xác minh email, xác minh SĐT, đổi SĐT...).
    public void CapNhatTichXanh()
    {
        if (!XacMinhBoiAdmin)
        {
            DaXacMinhDanhTinh = EmailConfirmed && PhoneNumberConfirmed;
        }
    }

    // ==== Cài đặt Giao diện & hiển thị ====
    public bool GiaoDienToi { get; set; } = false;
    public string? DuongDanAnhNen { get; set; }

    // Cài đặt Thông báo & nhắc nhở 
    public bool BatBaoCaoTaiChinh { get; set; } = false;
    public bool BatNhacNhoNhapLieu { get; set; } = false;
    public TanSuatBaoCao TanSuatBaoCao { get; set; } = TanSuatBaoCao.HangTuan;
    public TimeSpan GioBaoCao { get; set; } = new TimeSpan(8, 0, 0);
    public DateTime? LanCuoiGuiBaoCao { get; set; }
    public DateTime? LanCuoiNhacNho { get; set; }

    // Thông tin gói Premium 
    public bool IsPremium { get; set; } = false;
    public DateTime? PremiumHetHan { get; set; }

    // Kiểm tra nhanh Premium còn hiệu lực hay không
    public bool PremiumConHieuLuc => IsPremium && PremiumHetHan.HasValue && PremiumHetHan.Value >= DateTime.Today;

    // Hộ gia đình (tính năng quản lý tài chính gia đình)
    public int? HouseholdId { get; set; }
    public Household? Household { get; set; }
}
