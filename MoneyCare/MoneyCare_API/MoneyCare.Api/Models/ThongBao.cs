// Thông báo trong app (chuông thông báo ở Trang chủ).
// LƯU Ý: Đây là BẢNG MỚI trong database -> sau khi thêm code này, bạn cần chạy:
//   dotnet ef migrations add ThemBangThongBao
//   dotnet ef database update
// (xem chi tiết trong HUONG_DAN_CAI_DAT.docx)
using System.ComponentModel.DataAnnotations;

namespace MoneyCare.Models;

public enum LoaiThongBao
{
    HoaDon,          // Nhắc hẹn hóa đơn sắp đến hạn
    PhanTichAI,      // Cảnh báo/gợi ý từ Phân tích & Gợi ý AI
    BaoCao,          // Báo cáo tài chính định kỳ
    XacMinh,         // Xác minh email / số điện thoại / tích xanh
    HoSo,            // Cập nhật hồ sơ, đổi ảnh, đổi cài đặt
    HoGiaDinh,       // Tạo/tham gia/rời/xóa hộ gia đình
    Premium,         // Đăng ký/kích hoạt/gia hạn/hủy Premium
    HeThong          // Thông báo chung từ quản trị viên
}

public class ThongBao
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [StringLength(200)]
    public string TieuDe { get; set; } = string.Empty;

    [StringLength(1000)]
    public string NoiDung { get; set; } = string.Empty;

    public LoaiThongBao Loai { get; set; } = LoaiThongBao.HeThong;

    // Tên route bên Flutter để điều hướng khi bấm vào thông báo, VD: "/bills", "/insights"
    [StringLength(50)]
    public string? DuongDanDieuHuong { get; set; }

    public bool DaXem { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.Now;
}
