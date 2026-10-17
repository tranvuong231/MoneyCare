// Tuần 10: hoàn thiện Trang cá nhân/Cài đặt, khu vực Admin (khóa/mở/xác minh người dùng, xem/xóa dữ
// liệu), và chạy lại toàn bộ 10 tuần test tự động (10 file Week01..Week10) trước khi bàn giao.
// AdminXoaHelper.XoaPremiumOrderAnToanAsync: gỡ liên kết ở ZaloPayOrders TRƯỚC khi xóa PremiumOrder,
// tránh vi phạm ràng buộc khóa ngoại (xem chú thích chi tiết trong AdminXoaHelper.cs).
using MoneyCare.Models;
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week10_AdminXoaHelperTests
{
    [Fact]
    public async Task XoaPremiumOrderAnToanAsync_XoaDonThanhCong_KhiKhongCoZaloPayLienQuan()
    {
        await using var db = TestHelpers.TaoDbContext();
        var don = new PremiumOrder { UserId = "user-010", MaGiaoDich = "PREMIUM010A", SoTien = 29000 };
        db.PremiumOrders.Add(don);
        await db.SaveChangesAsync();

        await AdminXoaHelper.XoaPremiumOrderAnToanAsync(db, don);

        Assert.Empty(db.PremiumOrders);
    }

    [Fact]
    public async Task XoaPremiumOrderAnToanAsync_GoLienKetZaloPay_TruocKhiXoaDonPremium()
    {
        await using var db = TestHelpers.TaoDbContext();
        var don = new PremiumOrder { UserId = "user-010", MaGiaoDich = "PREMIUM010B", SoTien = 87000, SoThangDangKy = 3 };
        db.PremiumOrders.Add(don);
        await db.SaveChangesAsync();

        var donZaloPay = new ZaloPayOrder
        {
            AppTransId = "260827_0000001",
            SoTien = 87000,
            MucDich = MucDichZaloPay.NangCapPremium,
            PremiumOrderId = don.Id
        };
        db.ZaloPayOrders.Add(donZaloPay);
        await db.SaveChangesAsync();

        await AdminXoaHelper.XoaPremiumOrderAnToanAsync(db, don);

        Assert.Empty(db.PremiumOrders);
        // Đơn ZaloPay KHÔNG bị xóa theo (vẫn cần giữ lại lịch sử giao dịch thanh toán thật với ZaloPay),
        // chỉ gỡ liên kết PremiumOrderId về null.
        var donZaloPaySauKhiXoa = Assert.Single(db.ZaloPayOrders);
        Assert.Null(donZaloPaySauKhiXoa.PremiumOrderId);
    }

    [Fact]
    public async Task XoaPremiumOrderAnToanAsync_KhongAnhHuong_DonPremiumKhac()
    {
        await using var db = TestHelpers.TaoDbContext();
        var donCanXoa = new PremiumOrder { UserId = "user-010", MaGiaoDich = "PREMIUM010C", SoTien = 29000 };
        var donGiuLai = new PremiumOrder { UserId = "user-010-khac", MaGiaoDich = "PREMIUM010D", SoTien = 29000 };
        db.PremiumOrders.AddRange(donCanXoa, donGiuLai);
        await db.SaveChangesAsync();

        await AdminXoaHelper.XoaPremiumOrderAnToanAsync(db, donCanXoa);

        var conLai = Assert.Single(db.PremiumOrders);
        Assert.Equal(donGiuLai.MaGiaoDich, conLai.MaGiaoDich);
    }
}
