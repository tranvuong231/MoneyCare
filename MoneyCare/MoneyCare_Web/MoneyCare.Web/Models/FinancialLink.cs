// liền kết ngân hàng
using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.Models;

public enum NhaCungCap
{
    NganHang = 0,
    MoMo = 1,
    ZaloPay = 2
}

// Ghi chú quan trọng: Đây là "khung tích hợp" (integration scaffold).
// Việc kết nối THẬT với API ngân hàng / MoMo / ZaloPay yêu cầu hợp đồng đối tác
// chính thức (Open Banking API, MoMo Business API, ZaloPay Merchant API...).
// Bảng này lưu trạng thái/metadata của liên kết; phần gọi API thật sẽ được
// implement trong Services/IFinancialLinkProvider.cs khi có API Key chính thức.
public class FinancialLink
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Nhà cung cấp")]
    public NhaCungCap NhaCungCap { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên gợi nhớ")]
    [StringLength(100)]
    [Display(Name = "Tên gợi nhớ")]
    public string TenGoiNho { get; set; } = string.Empty; // VD: "Vietcombank cá nhân", "MoMo chính"

    [StringLength(30)]
    [Display(Name = "Số tài khoản / SĐT (ẩn 1 phần)")]
    public string SoTaiKhoanAn { get; set; } = string.Empty; // Chỉ lưu dạng che (**** 1234)

    public bool DaKetNoi { get; set; } = false;
    public DateTime? NgayKetNoi { get; set; }
    public DateTime? DongBoLanCuoi { get; set; }

    [StringLength(255)]
    public string? GhiChu { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
}
