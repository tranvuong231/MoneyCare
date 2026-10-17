// Đại diện dữ liệu hiển thị trên bảng điều khiển của trang quản trị.
namespace MoneyCare.Areas.Admin.ViewModels;

public class AdminDashboardViewModel
{
    public int TongNguoiDung { get; set; }
    public int TongNguoiDungPremium { get; set; }
    public int NguoiDungMoiThangNay { get; set; }
    public int TongGiaoDich { get; set; }
    public int TongHoGiaDinh { get; set; }
    public decimal TongDoanhThu { get; set; }
    public int DonChoXacNhan { get; set; }

    public List<string> NhanThang { get; set; } = new();
    public List<decimal> DoanhThuTheoThang { get; set; } = new();
    public List<int> NguoiDungMoiTheoThang { get; set; } = new();

    public List<MoneyCare.Models.PremiumOrder> DonGanDay { get; set; } = new();
    public List<MoneyCare.Models.ApplicationUser> NguoiDungMoiNhat { get; set; } = new();
}
