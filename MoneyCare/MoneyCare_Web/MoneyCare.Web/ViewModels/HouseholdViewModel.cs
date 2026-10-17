// Đại diện dữ liệu của hộ gia đình hoặc nhóm người dùng.
namespace MoneyCare.ViewModels;

public class HouseholdViewModel
{
    public MoneyCare.Models.Household? HoGiaDinh { get; set; }
    public List<ThanhVienChiTieu> ThanhVienChiTieu { get; set; } = new();
    public decimal TongThuGiaDinh { get; set; }
    public decimal TongChiGiaDinh { get; set; }
    public List<MoneyCare.Models.Transaction> GiaoDichGanDay { get; set; } = new();
}

public class ThanhVienChiTieu
{
    public string TenThanhVien { get; set; } = string.Empty;
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
}
