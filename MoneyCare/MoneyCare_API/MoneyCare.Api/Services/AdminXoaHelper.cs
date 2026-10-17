// Logic xóa an toàn dùng chung giữa khu vực Admin (web MVC - Areas/Admin/Controllers) và
// Admin API (Controllers/Api/Admin) - tránh lặp code và đảm bảo 2 nơi luôn xóa nhất quán nhau.
//
// Vì sao cần "an toàn" thay vì chỉ gọi Remove() rồi SaveChanges(): 1 vài bảng (vd ZaloPayOrders)
// được thêm vào sau và ràng buộc khóa ngoại trên CSDL thực tế không set NULL tự động như cấu hình
// trong ApplicationDbContext, nên nếu xóa thẳng bản ghi cha khi vẫn còn bản ghi con tham chiếu tới sẽ
// bị SQL Server chặn lại (lỗi "The DELETE statement conflicted with the REFERENCE constraint...").
// Cách an toàn nhất bất kể CSDL thực tế đang cấu hình ràng buộc kiểu gì: chủ động gỡ liên kết/xóa hết
// dữ liệu con liên quan bằng code trước khi xóa bản ghi cha.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Data;
using MoneyCare.Models;

namespace MoneyCare.Services;

public static class AdminXoaHelper
{
    /// <summary>Xóa 1 đơn Premium, tự gỡ liên kết ở các đơn ZaloPay đang trỏ tới đơn đó trước.</summary>
    public static async Task XoaPremiumOrderAnToanAsync(ApplicationDbContext context, PremiumOrder don)
    {
        var zaloPayLienQuan = await context.ZaloPayOrders.Where(z => z.PremiumOrderId == don.Id).ToListAsync();
        foreach (var z in zaloPayLienQuan) z.PremiumOrderId = null;

        context.PremiumOrders.Remove(don);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Xóa hẳn 1 người dùng khỏi hệ thống, kèm toàn bộ dữ liệu riêng của họ (giao dịch, danh mục, hóa đơn,
    /// liên kết ngân hàng, đơn Premium, đơn ZaloPay, thông báo). Nếu người dùng đang là chủ hộ của 1 hộ
    /// gia đình, hộ đó sẽ bị giải tán (các thành viên còn lại về trạng thái không có hộ) thay vì chặn xóa.
    /// Tin nhắn liên hệ họ từng gửi được GIỮ LẠI (chỉ gỡ liên kết UserId) để admin còn tra cứu lịch sử.
    /// Toàn bộ nằm trong 1 transaction - lỗi ở bất kỳ bước nào sẽ rollback hết, không để dữ liệu nửa vời.
    /// </summary>
    public static async Task<(bool ThanhCong, string? Loi)> XoaNguoiDungAnToanAsync(
        ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Hộ gia đình do người này làm chủ hộ -> giải tán hộ, không xóa lây các thành viên khác
            var hoLamChu = await context.Households.Include(h => h.ThanhVien)
                .Where(h => h.ChuHoId == user.Id).ToListAsync();
            foreach (var ho in hoLamChu)
            {
                foreach (var tv in ho.ThanhVien ?? new List<ApplicationUser>())
                {
                    if (tv.Id != user.Id) tv.HouseholdId = null;
                }
                context.Households.Remove(ho);
            }

            // Nếu chỉ là thành viên (không phải chủ hộ) của 1 hộ khác -> tự rời hộ đó
            user.HouseholdId = null;

            // Xóa các đơn ZaloPay trước (bảng con của cả PremiumOrder lẫn FinancialLink)
            var zaloPayOrders = await context.ZaloPayOrders.Where(z => z.UserId == user.Id).ToListAsync();
            context.ZaloPayOrders.RemoveRange(zaloPayOrders);

            var premiumOrders = await context.PremiumOrders.Where(o => o.UserId == user.Id).ToListAsync();
            context.PremiumOrders.RemoveRange(premiumOrders);

            var financialLinks = await context.FinancialLinks.Where(l => l.UserId == user.Id).ToListAsync();
            context.FinancialLinks.RemoveRange(financialLinks);

            // Xóa giao dịch trước khi xóa danh mục (Transaction.CategoryId tham chiếu Category)
            var transactions = await context.Transactions.Where(t => t.UserId == user.Id).ToListAsync();
            context.Transactions.RemoveRange(transactions);

            var categories = await context.Categories.Where(c => c.UserId == user.Id).ToListAsync();
            context.Categories.RemoveRange(categories);

            var bills = await context.Bills.Where(b => b.UserId == user.Id).ToListAsync();
            context.Bills.RemoveRange(bills);

            var thongBaos = await context.ThongBaos.Where(t => t.UserId == user.Id).ToListAsync();
            context.ThongBaos.RemoveRange(thongBaos);

            // Giữ lại tin nhắn liên hệ nhưng gỡ liên kết tới tài khoản sắp xóa
            var tinNhanLienHe = await context.ContactMessages.Where(m => m.UserId == user.Id).ToListAsync();
            foreach (var tn in tinNhanLienHe) tn.UserId = null;

            await context.SaveChangesAsync();

            var ketQuaXoa = await userManager.DeleteAsync(user);
            if (!ketQuaXoa.Succeeded)
            {
                await transaction.RollbackAsync();
                return (false, string.Join("; ", ketQuaXoa.Errors.Select(e => e.Description)));
            }

            await transaction.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }
}
