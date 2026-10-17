using MoneyCare.Models;
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.Models;

public class Household
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hộ gia đình")]
    [StringLength(150)]
    [Display(Name = "Tên hộ gia đình")]
    public string TenHoGiaDinh { get; set; } = string.Empty;

    // Mã mời 6 ký tự để thành viên khác tham gia hộ gia đình
    [StringLength(10)]
    public string MaMoi { get; set; } = string.Empty;

    public string ChuHoId { get; set; } = string.Empty;
    public ApplicationUser? ChuHo { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public ICollection<ApplicationUser>? ThanhVien { get; set; }
}
