// Helper tạo thông báo dùng chung cho mọi API controller - gọi TaoAsync(...) ở bất kỳ đâu
// cần báo cho người dùng biết có sự kiện mới (không cần SaveChanges riêng, hàm tự lưu).
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Services;

public static class ThongBaoHelper
{
    public static async Task TaoAsync(
        ApplicationDbContext context,
        string userId,
        string tieuDe,
        string noiDung,
        LoaiThongBao loai,
        string? duongDanDieuHuong = null)
    {
        context.ThongBaos.Add(new ThongBao
        {
            UserId = userId,
            TieuDe = tieuDe,
            NoiDung = noiDung,
            Loai = loai,
            DuongDanDieuHuong = duongDanDieuHuong
        });
        await context.SaveChangesAsync();
    }

    // Tránh tạo trùng thông báo cùng loại trong ngày (dùng cho cảnh báo AI, nhắc hóa đơn lặp lại mỗi ngày)
    public static async Task<bool> DaCoHomNayAsync(ApplicationDbContext context, string userId, string tieuDe)
    {
        var homNay = DateTime.Today;
        return await Task.FromResult(context.ThongBaos.Any(t =>
            t.UserId == userId && t.TieuDe == tieuDe && t.NgayTao >= homNay));
    }
}
