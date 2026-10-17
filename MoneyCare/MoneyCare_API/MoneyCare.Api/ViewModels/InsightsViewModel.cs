// Đại diện dữ liệu phân tích và thống kê tài chính.
namespace MoneyCare.ViewModels;

public class InsightItem
{
    public string Icon { get; set; } = "bi-lightbulb";
    public string MucDo { get; set; } = "info"; // info | warning | danger | success
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
}

public class DuDoanSoDu
{
    public decimal SoDuHienTai { get; set; }
    public decimal ChiTieuTrungBinhNgay { get; set; }
    public decimal ThuNhapTrungBinhNgay { get; set; }
    public int SoNgayConLaiTrongThang { get; set; }
    public decimal SoDuDuDoanCuoiThang { get; set; }
    public string XuHuong { get; set; } = "on_dinh"; // tang | giam | on_dinh
}

public class InsightsViewModel
{
    public List<InsightItem> ThoiQuenChiTieu { get; set; } = new();
    public List<InsightItem> GoiYTietKiem { get; set; } = new();
    public DuDoanSoDu DuDoan { get; set; } = new();

    public List<string> NhanThang { get; set; } = new();
    public List<decimal> ChiTieuThucTe { get; set; } = new();
    public List<decimal?> ChiTieuDuBao { get; set; } = new();
}
