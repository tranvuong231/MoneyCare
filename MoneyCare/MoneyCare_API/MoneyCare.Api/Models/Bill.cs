using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyCare.Models;

public class Bill
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hóa đơn")]
    [StringLength(150, ErrorMessage = "Tên hóa đơn tối đa 150 ký tự")]
    [Display(Name = "Tên hóa đơn")]
    public string TenHoaDon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số tiền")]
    [Range(0.01, 999999999999, ErrorMessage = "Số tiền phải lớn hơn 0")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Số tiền")]
    public decimal SoTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày đến hạn")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày đến hạn")]
    public DateTime NgayDenHan { get; set; } = DateTime.Today.AddDays(7);

    [Display(Name = "Đã thanh toán")]
    public bool DaThanhToan { get; set; } = false;

    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Số ngày còn lại đến hạn (âm nghĩa là đã quá hạn) - không lưu vào DB
    [NotMapped]
    public int SoNgayConLai => (NgayDenHan.Date - DateTime.Today).Days;
}
