// Đơn hàng ZaloPay (UAT/sandbox) - ghi lại mọi đơn tạo qua API v2/create của ZaloPay để:
//  1) Xác thực trạng thái thanh toán khi ZaloPay gọi callback / khi hệ thống tự truy vấn (polling).
//  2) Dùng làm "nguồn giao dịch" để đồng bộ tự động vào Transactions (xem ZaloPayProvider.LayGiaoDichMoiAsync).
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyCare.Models;

public enum MucDichZaloPay
{
    // Đơn xác thực nhỏ (1.000đ) để chứng minh người dùng sở hữu tài khoản ZaloPay UAT khi liên kết
    XacThucLienKet = 0,

    // Đơn thanh toán để nâng cấp gói Premium
    NangCapPremium = 1
}

public enum TrangThaiZaloPay
{
    ChoThanhToan = 0,
    ThanhCong = 1,
    ThatBai = 2
}

public class ZaloPayOrder
{
    public int Id { get; set; }

    // Mã giao dịch phía MoneyCare gửi cho ZaloPay - PHẢI duy nhất, định dạng yymmdd_xxxxxxx
    [Required]
    [StringLength(50)]
    public string AppTransId { get; set; } = string.Empty;

    // Mã giao dịch phía ZaloPay trả về sau khi thanh toán thành công (dùng để đối soát/hoàn tiền)
    public long? ZpTransId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTien { get; set; }

    [StringLength(255)]
    public string MoTa { get; set; } = string.Empty;

    public MucDichZaloPay MucDich { get; set; }
    public TrangThaiZaloPay TrangThai { get; set; } = TrangThaiZaloPay.ChoThanhToan;

    // true khi đơn NangCapPremium đã được ZaloPayProvider đưa vào Transactions (tránh đồng bộ trùng)
    public bool DaDongBoGiaoDich { get; set; } = false;

    // Liên kết tới đơn Premium tương ứng (chỉ có khi MucDich = NangCapPremium)
    public int? PremiumOrderId { get; set; }
    public PremiumOrder? PremiumOrder { get; set; }

    // Liên kết tới FinancialLink đang chờ xác thực (chỉ có khi MucDich = XacThucLienKet)
    public int? FinancialLinkId { get; set; }
    public FinancialLink? FinancialLink { get; set; }

    // Chuỗi "qr_code" (định dạng VietQR/EMVCo) mà ZaloPay trả về lúc tạo đơn - lưu lại để
    // vẽ ảnh QR bất kỳ lúc nào (endpoint ảnh QR) mà không cần gọi lại API tạo đơn.
    [StringLength(1000)]
    public string? QrCodeRaw { get; set; }

    // Link ZaloPay Gateway (dùng khi cần mở trực tiếp trên trình duyệt/app thay vì quét QR)
    [StringLength(1000)]
    public string? OrderUrl { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayThanhToan { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
}
