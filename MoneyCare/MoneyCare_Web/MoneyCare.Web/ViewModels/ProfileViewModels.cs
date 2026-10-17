// Đại diện dữ liệu hồ sơ cá nhân của người dùng.
using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.ViewModels;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(100)]
    [Display(Name = "Tên đăng nhập")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? NgaySinh { get; set; }

    [Display(Name = "Giới tính")]
    public GioiTinh? GioiTinh { get; set; }

    [StringLength(12, MinimumLength = 9, ErrorMessage = "CCCD/CMND phải có 9 hoặc 12 chữ số")]
    [RegularExpression(@"^\d{9}(\d{3})?$", ErrorMessage = "CCCD/CMND chỉ gồm 9 hoặc 12 chữ số")]
    [Display(Name = "Số CCCD/CMND")]
    public string? CanCuocCongDan { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string MatKhauHienTai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string XacNhanMatKhauMoi { get; set; } = string.Empty;
}

public class ThemSoDienThoaiViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    // Người dùng phải tick xác nhận đã đọc cảnh báo "không thể đổi sau khi thêm"
    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần xác nhận đã đọc cảnh báo trước khi tiếp tục")]
    [Display(Name = "Tôi hiểu và đồng ý")]
    public bool DaXacNhanCanhBao { get; set; }
}

public class XacMinhOtpViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
    [StringLength(8, MinimumLength = 4)]
    [Display(Name = "Mã xác minh (OTP)")]
    public string MaOtp { get; set; } = string.Empty;
}
