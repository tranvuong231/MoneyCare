// Đại diện dữ liệu hiển thị trên bảng điều khiển (Dashboard).
namespace MoneyCare.ViewModels;

public class DashboardViewModel
{
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public decimal SoDu => TongThu - TongChi;

    // Dữ liệu biểu đồ tròn: chi tiêu theo danh mục trong tháng
    public List<string> TenDanhMucChi { get; set; } = new();
    public List<decimal> SoTienTheoDanhMucChi { get; set; } = new();
    public List<string> MauSacDanhMucChi { get; set; } = new();

    // Dữ liệu biểu đồ cột: thu chi 6 tháng gần nhất
    public List<string> NhanThang { get; set; } = new();
    public List<decimal> ThuTheoThang { get; set; } = new();
    public List<decimal> ChiTheoThang { get; set; } = new();

    public List<MoneyCare.Models.Transaction> GiaoDichGanDay { get; set; } = new();
    public List<MoneyCare.Models.Bill> HoaDonSapToiHan { get; set; } = new();
}
