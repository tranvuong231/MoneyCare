// Chuyển đổi DTO (JSON từ MoneyCare_API) sang các Model cục bộ (MoneyCare.Models) mà Views/*.cshtml
// đang dùng - nhờ vậy TOÀN BỘ giao diện Razor giữ nguyên, không cần sửa 1 dòng .cshtml nào.
using MoneyCare.Models;

namespace MoneyCare.Web.Dtos;

public static class UserMapper
{
    /// <summary>apiBaseUrl dùng để ghép thành URL tuyệt đối cho ảnh đại diện/ảnh nền (do file ảnh
    /// nằm trên MoneyCare_API, không nằm trên Web) - vd "/uploads/avatars/x.jpg" -> "https://api.../uploads/avatars/x.jpg".</summary>
    public static ApplicationUser TuDto(UserDto d, string? apiBaseUrl = null) => new()
    {
        Id = d.Id,
        HoTen = d.HoTen,
        Email = d.Email,
        UserName = d.Email,
        PhoneNumber = d.PhoneNumber,
        DaXacMinhDanhTinh = d.DaXacMinhDanhTinh,
        EmailConfirmed = d.EmailConfirmed,
        DuongDanAnhDaiDien = GhepUrl(apiBaseUrl, d.DuongDanAnhDaiDien),
        DuongDanAnhNen = GhepUrl(apiBaseUrl, d.DuongDanAnhNen),
        GiaoDienToi = d.GiaoDienToi,
        IsPremium = d.IsPremium,
        PremiumHetHan = d.PremiumHetHan,
        HouseholdId = d.HouseholdId,
        DangNhapBangGoogle = d.DangNhapBangGoogle,
        PhoneNumberConfirmed = d.PhoneNumberConfirmed,
        NgaySinh = d.NgaySinh,
        GioiTinh = d.GioiTinh,
        CanCuocCongDan = d.CanCuocCongDan,
        BatBaoCaoTaiChinh = d.BatBaoCaoTaiChinh,
        BatNhacNhoNhapLieu = d.BatNhacNhoNhapLieu,
        TanSuatBaoCao = d.TanSuatBaoCao,
        GioBaoCao = TimeSpan.TryParse(d.GioBaoCao, out var g) ? g : new TimeSpan(8, 0, 0)
    };

    private static string? GhepUrl(string? apiBaseUrl, string? duongDanTuongDoi)
    {
        if (string.IsNullOrEmpty(duongDanTuongDoi)) return duongDanTuongDoi;
        if (duongDanTuongDoi.StartsWith("http")) return duongDanTuongDoi;
        return string.IsNullOrEmpty(apiBaseUrl) ? duongDanTuongDoi : apiBaseUrl.TrimEnd('/') + duongDanTuongDoi;
    }
}

public static class CategoryMapper
{
    public static Category TuDto(CategoryDto d) => new() { Id = d.Id, TenDanhMuc = d.TenDanhMuc, Loai = d.Loai, MauSac = d.MauSac };
}

public static class TransactionMapper
{
    public static Transaction TuDto(TransactionDto d) => new()
    {
        Id = d.Id,
        SoTien = d.SoTien,
        Loai = d.Loai,
        NgayGiaoDich = d.NgayGiaoDich,
        GhiChu = d.GhiChu,
        CategoryId = d.CategoryId,
        ChiaSeGiaDinh = d.ChiaSeGiaDinh,
        Category = new Category { Id = d.CategoryId, TenDanhMuc = d.TenDanhMuc ?? "", MauSac = d.MauDanhMuc ?? "#0d6efd" },
        User = d.TenNguoiTao == null ? null : new ApplicationUser { HoTen = d.TenNguoiTao }
    };
}

public static class BillMapper
{
    public static Bill TuDto(BillDto d) => new()
    {
        Id = d.Id,
        TenHoaDon = d.TenHoaDon,
        SoTien = d.SoTien,
        NgayDenHan = d.NgayDenHan,
        DaThanhToan = d.DaThanhToan,
        GhiChu = d.GhiChu
    };
}

public static class FinancialLinkMapper
{
    public static FinancialLink TuDto(FinancialLinkDto d) => new()
    {
        Id = d.Id,
        NhaCungCap = d.NhaCungCap,
        TenGoiNho = d.TenGoiNho,
        SoTaiKhoanAn = d.SoTaiKhoanAn,
        DaKetNoi = d.DaKetNoi,
        NgayKetNoi = d.NgayKetNoi,
        DongBoLanCuoi = d.DongBoLanCuoi,
        GhiChu = d.GhiChu
    };
}

public static class ThongBaoMapper
{
    public static ThongBao TuDto(ThongBaoDto d) => new()
    {
        Id = d.Id,
        TieuDe = d.TieuDe,
        NoiDung = d.NoiDung,
        Loai = d.Loai,
        DuongDanDieuHuong = d.DuongDanDieuHuong,
        DaXem = d.DaXem,
        NgayTao = d.NgayTao
    };
}
