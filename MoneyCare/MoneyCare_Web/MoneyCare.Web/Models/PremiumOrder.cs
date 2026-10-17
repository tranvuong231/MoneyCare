//đơn nâng cấp pre
using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyCare.Models;

public class PremiumOrder
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string MaGiaoDich { get; set; } = string.Empty; // Nội dung chuyển khoản, dùng để đối soát

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoTien { get; set; }

    public int SoThangDangKy { get; set; } = 1;

    public bool DaXacNhan { get; set; } = false;

    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayXacNhan { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
}
