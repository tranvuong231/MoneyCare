// Giao dịch thu / chi
using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; //dùng để cấu hình bảng và kiểu dữ liệu trong CSDL.

namespace MoneyCare.Models;

public class Transaction
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền")]
    [Range(0.01, 999999999999, ErrorMessage = "Số tiền phải lớn hơn 0")]
    [Display(Name = "Số tiền")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTien { get; set; }

    [Display(Name = "Loại")]
    public LoaiGiaoDich Loai { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày giao dịch")]
    [Display(Name = "Ngày giao dịch")]
    [DataType(DataType.Date)]
    public DateTime NgayGiaoDich { get; set; } = DateTime.Today;

    /// <summary>Mốc thời gian CHÍNH XÁC (ngày + giờ:phút:giây) lúc giao dịch được ghi nhận vào hệ
    /// thống - dùng để SẮP XẾP "Giao dịch gần đây" chuẩn xác (kể cả nhiều giao dịch cùng ngày) và để
    /// hiển thị mốc thời gian tương đối ("Vừa xong", "10 phút trước"...). Khác với <see cref="NgayGiaoDich"/>
    /// (chỉ lưu NGÀY, người dùng có thể tự chọn lùi/tới ngày giao dịch thực tế).</summary>
    [Display(Name = "Thời gian tạo")]
    public DateTime ThoiGianTao { get; set; } = DateTime.Now;

    [StringLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Nếu true và người dùng thuộc 1 hộ gia đình, giao dịch sẽ hiện trong báo cáo chung của gia đình (tính năng Premium)
    [Display(Name = "Chia sẻ với gia đình")]
    public bool ChiaSeGiaDinh { get; set; } = false;

    /// <summary>Nguồn phát sinh giao dịch (thủ công/hóa đơn/ZaloPay/ngân hàng/OCR) - hiển thị icon phân biệt.</summary>
    public NguonGiaoDich Nguon { get; set; } = NguonGiaoDich.ThuCong;

    /// <summary>Trạng thái đồng bộ - null với giao dịch <see cref="NguonGiaoDich.ThuCong"/> (không áp dụng).</summary>
    public TrangThaiDongBoGiaoDich? TrangThaiDongBo { get; set; }

    /// <summary>Liên kết tới Hóa đơn (Bill) đã sinh ra giao dịch này (nếu Nguon = HoaDon) - dùng để
    /// tránh tạo trùng/xóa đồng bộ khi hóa đơn đổi trạng thái thanh toán.</summary>
    public int? BillId { get; set; }
    public Bill? Bill { get; set; }
}
