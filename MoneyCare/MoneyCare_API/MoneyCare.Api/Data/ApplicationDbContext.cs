using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Models;

namespace MoneyCare.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Bill> Bills { get; set; } = null!;
    public DbSet<Household> Households { get; set; } = null!;
    public DbSet<FinancialLink> FinancialLinks { get; set; } = null!;
    public DbSet<PremiumOrder> PremiumOrders { get; set; } = null!;
    public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
    public DbSet<ThongBao> ThongBaos { get; set; } = null!;
    public DbSet<ZaloPayOrder> ZaloPayOrders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Không cho xóa danh mục đang có giao dịch (chặn ở tầng code)

        builder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Giao dịch tự động sinh ra từ 1 Hóa đơn (khi đánh dấu "Đã thanh toán") - nếu xóa Hóa đơn,
        // CHỈ gỡ liên kết (KHÔNG xóa giao dịch, vì khoản tiền đó thực tế đã chi rồi).
        // Dùng ClientSetNull (xử lý ở tầng ứng dụng/EF) thay vì SetNull ở DB, vì SQL Server không cho
        // phép nhiều đường cascade cùng trỏ về 1 bảng (AspNetUsers -> Transactions VÀ
        // AspNetUsers -> Bills -> Transactions) - BillsController sẽ tự gỡ liên kết trước khi xóa Hóa đơn.
        builder.Entity<Transaction>()
            .HasOne(t => t.Bill)
            .WithMany()
            .HasForeignKey(t => t.BillId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.Entity<Category>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bill>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Transaction>().Property(t => t.SoTien).HasColumnType("decimal(18,2)");
        builder.Entity<Bill>().Property(b => b.SoTien).HasColumnType("decimal(18,2)");

        // Hộ gia đình: 1 chủ hộ có thể tạo nhiều hộ; 1 user thuộc tối đa 1 hộ
        builder.Entity<Household>()
            .HasOne(h => h.ChuHo)
            .WithMany()
            .HasForeignKey(h => h.ChuHoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Household)
            .WithMany(h => h.ThanhVien)
            .HasForeignKey(u => u.HouseholdId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FinancialLink>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PremiumOrder>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ContactMessage>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Entity<ThongBao>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ZaloPayOrder>()
            .HasOne(z => z.User)
            .WithMany()
            .HasForeignKey(z => z.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction (thay vì SetNull) để tránh lỗi "multiple cascade paths" của SQL Server, vì
        // AspNetUsers đã có đường Cascade riêng tới ZaloPayOrders lẫn tới PremiumOrders/FinancialLinks.
        builder.Entity<ZaloPayOrder>()
            .HasOne(z => z.PremiumOrder)
            .WithMany()
            .HasForeignKey(z => z.PremiumOrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<ZaloPayOrder>()
            .HasOne(z => z.FinancialLink)
            .WithMany()
            .HasForeignKey(z => z.FinancialLinkId)
            .OnDelete(DeleteBehavior.NoAction);

        // AppTransId là mã giao dịch gửi cho ZaloPay - bắt buộc duy nhất để tránh tạo trùng đơn
        builder.Entity<ZaloPayOrder>()
            .HasIndex(z => z.AppTransId)
            .IsUnique();
    }
}
