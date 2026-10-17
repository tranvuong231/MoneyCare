// ============================================================================
// TOÀN BỘ DTO (Data Transfer Object) CỦA API - MỖI CLASS TƯƠNG ỨNG 1-1 VỚI
// MODEL BÊN FLUTTER (lib/models/models.dart) VÀ VIEWMODEL BÊN WEB.
//
// QUAN TRỌNG: tên thuộc tính ở đây PHẢI khớp camelCase với tên field bên
// Flutter (vd "tenDanhMuc" khớp j['tenDanhMuc']) vì Program.cs đã cấu hình
// System.Text.Json dùng camelCase mặc định cho toàn bộ API.
// ============================================================================
using MoneyCare.Models;

namespace MoneyCare.Web.Dtos;

// ================== AUTH ==================
public class DangKyRequest
{
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class DangNhapRequest
{
    public string Email { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class DangNhapGoogleRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class DangNhapGoogleWebRequest
{
    // Dùng bởi AccountController.GoogleCallback: Web đã tự hoàn tất bắt tay OAuth server-side với
    // Google (có ClientSecret) nên chỉ cần gửi thẳng email đã được Google xác thực lên API - xem
    // class cùng tên bên MoneyCare_API/Dtos/Dtos.cs (AuthController.DangNhapGoogleWeb).
    public string Email { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public string? AnhDaiDien { get; set; }
}

public class QuenMatKhauRequest
{
    public string Email { get; set; } = string.Empty;
}

public class DatLaiMatKhauRequest
{
    public string Email { get; set; } = string.Empty;
    public string Ma { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool DaXacMinhDanhTinh { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? DuongDanAnhDaiDien { get; set; }
    public string? DuongDanAnhNen { get; set; }
    public bool GiaoDienToi { get; set; }
    public bool IsPremium { get; set; }
    public bool PremiumConHieuLuc { get; set; }
    public DateTime? PremiumHetHan { get; set; }
    public int? HouseholdId { get; set; }
    public bool LaAdmin { get; set; }
    public bool DangNhapBangGoogle { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public DateTime? NgaySinh { get; set; }
    public GioiTinh? GioiTinh { get; set; }
    public string? CanCuocCongDan { get; set; }
    public bool BatBaoCaoTaiChinh { get; set; }
    public bool BatNhacNhoNhapLieu { get; set; }
    public TanSuatBaoCao TanSuatBaoCao { get; set; }
    public string GioBaoCao { get; set; } = "08:00";
}

// ================== CATEGORY ==================
public class CategoryDto
{
    public int Id { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public LoaiGiaoDich Loai { get; set; }
    public string MauSac { get; set; } = "#0d6efd";
}

// ================== TRANSACTION ==================
public class TransactionInputDto
{
    public decimal SoTien { get; set; }
    public LoaiGiaoDich Loai { get; set; }
    public DateTime NgayGiaoDich { get; set; }
    public string? GhiChu { get; set; }
    public int CategoryId { get; set; }
    public bool ChiaSeGiaDinh { get; set; }
}

public class TransactionDto
{
    public int Id { get; set; }
    public decimal SoTien { get; set; }
    public LoaiGiaoDich Loai { get; set; }
    public DateTime NgayGiaoDich { get; set; }
    public string? GhiChu { get; set; }
    public int CategoryId { get; set; }
    public string? TenDanhMuc { get; set; }
    public string? MauDanhMuc { get; set; }
    public bool ChiaSeGiaDinh { get; set; }
    public string? TenNguoiTao { get; set; }

    public static TransactionDto TuModel(Transaction t) => new()
    {
        Id = t.Id,
        SoTien = t.SoTien,
        Loai = t.Loai,
        NgayGiaoDich = t.NgayGiaoDich,
        GhiChu = t.GhiChu,
        CategoryId = t.CategoryId,
        TenDanhMuc = t.Category?.TenDanhMuc,
        MauDanhMuc = t.Category?.MauSac,
        ChiaSeGiaDinh = t.ChiaSeGiaDinh,
        TenNguoiTao = t.User?.HoTen ?? t.User?.UserName
    };
}

public class TransactionDanhSachResponse
{
    public List<TransactionDto> DanhSach { get; set; } = new();
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
}

// ================== BILL ==================
public class BillInputDto
{
    public string TenHoaDon { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public DateTime NgayDenHan { get; set; }
    public bool DaThanhToan { get; set; }
    public string? GhiChu { get; set; }
}

public class BillDto
{
    public int Id { get; set; }
    public string TenHoaDon { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public DateTime NgayDenHan { get; set; }
    public bool DaThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public int SoNgayConLai { get; set; }

    public static BillDto TuModel(Bill b) => new()
    {
        Id = b.Id,
        TenHoaDon = b.TenHoaDon,
        SoTien = b.SoTien,
        NgayDenHan = b.NgayDenHan,
        DaThanhToan = b.DaThanhToan,
        GhiChu = b.GhiChu,
        SoNgayConLai = b.SoNgayConLai
    };
}

// ================== REPORT ==================
public class ReportCategorySliceDto
{
    public string Ten { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public string MauSac { get; set; } = "#0d6efd";
}

public class ReportDto
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public List<ReportCategorySliceDto> DanhMucChi { get; set; } = new();
    public List<ReportCategorySliceDto> DanhMucThu { get; set; } = new();
}

// ================== HOUSEHOLD ==================
public class ThanhVienChiTieuDto
{
    public string TenThanhVien { get; set; } = string.Empty;
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
}

public class HouseholdDto
{
    public int Id { get; set; }
    public string TenHoGiaDinh { get; set; } = string.Empty;
    public string MaMoi { get; set; } = string.Empty;
    public string? TenChuHo { get; set; }
    public bool LaChuHo { get; set; }
    public decimal TongThuGiaDinh { get; set; }
    public decimal TongChiGiaDinh { get; set; }
    public List<TransactionDto> GiaoDichGanDay { get; set; } = new();
    public List<ThanhVienChiTieuDto> ThanhVienChiTieu { get; set; } = new();
}

// ================== PREMIUM ==================
public class PremiumPricingDto
{
    public decimal GiaThang { get; set; }
    public string SoTaiKhoan { get; set; } = string.Empty;
    public string BinNganHang { get; set; } = string.Empty;
    public string TenNganHangHienThi { get; set; } = string.Empty;
}

public class PremiumOrderDto
{
    public int Id { get; set; }
    public string MaGiaoDich { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public int SoThangDangKy { get; set; }
    public bool DaXacNhan { get; set; }
    public DateTime NgayTao { get; set; }
    public string SoTaiKhoan { get; set; } = string.Empty;
    public string BinNganHang { get; set; } = string.Empty;
    public string TenNganHangHienThi { get; set; } = string.Empty;
}

// ================== FINANCIAL LINK (Integrations) ==================
public class FinancialLinkInputDto
{
    public NhaCungCap NhaCungCap { get; set; }
    public string TenGoiNho { get; set; } = string.Empty;
    public string SoTaiKhoanDayDu { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class FinancialLinkDto
{
    public int Id { get; set; }
    public NhaCungCap NhaCungCap { get; set; }
    public string TenGoiNho { get; set; } = string.Empty;
    public string SoTaiKhoanAn { get; set; } = string.Empty;
    public bool DaKetNoi { get; set; }
    public DateTime? NgayKetNoi { get; set; }
    public DateTime? DongBoLanCuoi { get; set; }
    public string? GhiChu { get; set; }

    public static FinancialLinkDto TuModel(FinancialLink f) => new()
    {
        Id = f.Id,
        NhaCungCap = f.NhaCungCap,
        TenGoiNho = f.TenGoiNho,
        SoTaiKhoanAn = f.SoTaiKhoanAn,
        DaKetNoi = f.DaKetNoi,
        NgayKetNoi = f.NgayKetNoi,
        DongBoLanCuoi = f.DongBoLanCuoi,
        GhiChu = f.GhiChu
    };
}

// ================== ZALOPAY ==================
public class ZaloPayDonHangDto
{
    public string AppTransId { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public string MoTa { get; set; } = string.Empty;
    public string UrlAnhQr { get; set; } = string.Empty;
    public string? OrderUrl { get; set; }
    public int? PremiumOrderId { get; set; }
    public int? SoThangDangKy { get; set; }
}

public class ZaloPayTrangThaiDto
{
    public string AppTransId { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "ChoThanhToan"; // ChoThanhToan | ThanhCong | ThatBai
    public decimal SoTien { get; set; }
    public bool IsPremium { get; set; }
    public DateTime? PremiumHetHan { get; set; }
}

// ================== CHATBOT ==================
public class CauHoiMauDto
{
    public string CauHoi { get; set; } = string.Empty;
    public string CauTraLoi { get; set; } = string.Empty;
}

// ================== PROFILE / SETTINGS ==================
public class CapNhatHoSoRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public DateTime? NgaySinh { get; set; }
    public GioiTinh? GioiTinh { get; set; }
    public string? CanCuocCongDan { get; set; }
}

public class DoiMatKhauRequest
{
    public string MatKhauHienTai { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class ThemSoDienThoaiRequest
{
    public string SoDienThoai { get; set; } = string.Empty;
}

public class XacMinhOtpRequest
{
    public string MaOtp { get; set; } = string.Empty;
}

public class CapNhatGiaoDienRequest
{
    public bool GiaoDienToi { get; set; }
    public bool XoaAnhNen { get; set; }
}

public class CapNhatThongBaoRequest
{
    public bool BatBaoCaoTaiChinh { get; set; }
    public bool BatNhacNhoNhapLieu { get; set; }
    public TanSuatBaoCao TanSuatBaoCao { get; set; }
    public string GioBaoCao { get; set; } = "08:00";
}

// ================== CONTACT ==================
public class ContactRequest
{
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
}

// ================== THÔNG BÁO ==================
public class ThongBaoDto
{
    public int Id { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public LoaiThongBao Loai { get; set; }
    public string? DuongDanDieuHuong { get; set; }
    public bool DaXem { get; set; }
    public DateTime NgayTao { get; set; }

    public static ThongBaoDto TuModel(ThongBao t) => new()
    {
        Id = t.Id,
        TieuDe = t.TieuDe,
        NoiDung = t.NoiDung,
        Loai = t.Loai,
        DuongDanDieuHuong = t.DuongDanDieuHuong,
        DaXem = t.DaXem,
        NgayTao = t.NgayTao
    };
}

public class ThongBaoDanhSachResponse
{
    public List<ThongBaoDto> DanhSach { get; set; } = new();
    public int SoChuaXem { get; set; }
}

// ================== ADMIN ==================
public class AdminDashboardDto
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
}

public class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? HoTen { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public DateTime NgayTao { get; set; }
    public bool IsPremium { get; set; }
    public bool PremiumConHieuLuc { get; set; }
    public bool LaAdmin { get; set; }
    public bool BiKhoa { get; set; }
    public bool DaXacMinhDanhTinh { get; set; }
}

public class AdminUserDetailDto : AdminUserDto
{
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public int SoGiaoDich { get; set; }
    public int SoDonPremium { get; set; }
    public DateTime? PremiumHetHan { get; set; }
}

public class AdminHouseholdDto
{
    public int Id { get; set; }
    public string TenHoGiaDinh { get; set; } = string.Empty;
    public string MaMoi { get; set; } = string.Empty;
    public string? TenChuHo { get; set; }
    public int SoThanhVien { get; set; }
    public DateTime NgayTao { get; set; }
}

public class AdminPremiumOrderDto
{
    public int Id { get; set; }
    public string MaGiaoDich { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public int SoThangDangKy { get; set; }
    public bool DaXacNhan { get; set; }
    public DateTime NgayTao { get; set; }
    public string? EmailNguoiDung { get; set; }
}

public class AdminContactMessageDto
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public DateTime NgayGui { get; set; }
    public bool DaXem { get; set; }
}

public class CapNhatPremiumRequest
{
    public int SoThang { get; set; }
}

// ================== THÔNG BÁO CHUNG (Ok/lỗi) ==================
public class ThongBaoResponse
{
    public string ThongBao { get; set; } = string.Empty;
}
