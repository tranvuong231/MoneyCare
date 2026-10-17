// Đại diện dữ liệu của biểu mẫu liên hệ và phản hồi.
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.ViewModels;

public class ContactViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email của bạn")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    [StringLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
    [Display(Name = "Nội dung liên hệ / góp ý")]
    public string NoiDung { get; set; } = string.Empty;
}
