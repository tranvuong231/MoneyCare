// Đại diện dữ liệu dùng để tạo và hiển thị báo cáo.
namespace MoneyCare.ViewModels;

public class ReportViewModel
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }

    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public decimal SoDu => TongThu - TongChi;

    public List<string> TenDanhMucChi { get; set; } = new();
    public List<decimal> SoTienDanhMucChi { get; set; } = new();
    public List<string> MauSacDanhMucChi { get; set; } = new();

    public List<string> TenDanhMucThu { get; set; } = new();
    public List<decimal> SoTienDanhMucThu { get; set; } = new();
    public List<string> MauSacDanhMucThu { get; set; } = new();
}
