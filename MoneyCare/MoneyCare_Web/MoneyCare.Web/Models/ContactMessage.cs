// tin nhắn liên hệ hoặc phản hồi từ người dùng.
using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email liên hệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    [StringLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
    [Display(Name = "Nội dung liên hệ / góp ý")]
    public string NoiDung { get; set; } = string.Empty;

    public DateTime NgayGui { get; set; } = DateTime.Now;
    public bool DaXem { get; set; } = false;

    // Null nếu người gửi chưa đăng nhập (khách vãng lai ở Landing Page)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
