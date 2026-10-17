using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.BackgroundJobs;

/// <summary>
/// Dịch vụ chạy nền, định kỳ kiểm tra cài đặt Thông báo &amp; Nhắc nhở của từng người dùng
/// (mục "Cài đặt" → tab "Thông báo & nhắc nhở") để:
///   1. Gửi báo cáo tài chính định kỳ (hằng ngày/hằng tuần) vào đúng khung giờ người dùng chọn.
///   2. Gửi email nhắc nhở nếu người dùng lâu ngày (≥ 3 ngày) chưa ghi nhận giao dịch nào.
///
/// Đây là code chạy thật (không phải giả lập) — miễn là ứng dụng đang chạy liên tục (deploy trên
/// VPS/hosting 24/7) và đã cấu hình SMTP thật trong appsettings.json (mục EmailSettings).
/// Nếu chưa cấu hình SMTP, email sẽ không gửi được nhưng ứng dụng vẫn hoạt động bình thường
/// (IEmailService.GuiEmailAsync sẽ trả về false một cách an toàn, không văng lỗi).
/// </summary>
public class NhacNhoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NhacNhoBackgroundService> _logger;
    private static readonly TimeSpan KhoangKiemTra = TimeSpan.FromMinutes(15);
    private const int SoNgayKhongGhiNhanDeNhacNho = 3;

    public NhacNhoBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NhacNhoBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ 1 phút sau khi ứng dụng khởi động rồi mới bắt đầu kiểm tra lần đầu
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (TaskCanceledException) { return; }

        using var timer = new PeriodicTimer(KhoangKiemTra);
        do
        {
            try
            {
                await XuLyAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý báo cáo/nhắc nhở định kỳ");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task XuLyAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var nguoiDungs = await context.Users
            .Where(u => u.BatBaoCaoTaiChinh || u.BatNhacNhoNhapLieu)
            .ToListAsync(ct);

        var bayGio = DateTime.Now;
        var coThayDoi = false;

        foreach (var user in nguoiDungs)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;

            if (user.BatBaoCaoTaiChinh && await NenGuiBaoCaoAsync(user, bayGio))
            {
                await GuiBaoCaoTaiChinhAsync(context, emailService, user, ct);
                user.LanCuoiGuiBaoCao = bayGio;
                coThayDoi = true;

                await ThongBaoHelper.TaoAsync(context, user.Id, "Báo cáo tài chính định kỳ",
                    "Báo cáo tài chính định kỳ của bạn đã sẵn sàng - xem chi tiết trong email hoặc mục Báo cáo.",
                    LoaiThongBao.BaoCao, "/reports");
            }

            if (user.BatNhacNhoNhapLieu)
            {
                var soNgayKhongGhiNhan = await TinhSoNgayKhongGhiNhanAsync(context, user, ct);
                var canNhacNho = soNgayKhongGhiNhan >= SoNgayKhongGhiNhanDeNhacNho &&
                    (user.LanCuoiNhacNho == null || (bayGio - user.LanCuoiNhacNho.Value).TotalHours >= 24);

                if (canNhacNho)
                {
                    var noiDung = $@"
                        <p>Xin chào {user.HoTen ?? user.Email},</p>
                        <p>Đã <strong>{soNgayKhongGhiNhan} ngày</strong> bạn chưa ghi nhận giao dịch thu chi nào trên MoneyCare.
                        Đừng quên cập nhật để theo dõi tài chính chính xác hơn nhé! 💰</p>";
                    await emailService.GuiEmailAsync(user.Email!, "[MoneyCare] Nhắc nhở ghi chép chi tiêu", noiDung);
                    user.LanCuoiNhacNho = bayGio;
                    coThayDoi = true;

                    await ThongBaoHelper.TaoAsync(context, user.Id, "Nhắc nhở ghi chép chi tiêu",
                        $"Đã {soNgayKhongGhiNhan} ngày bạn chưa ghi nhận giao dịch nào. Đừng quên cập nhật nhé!",
                        LoaiThongBao.HoSo, "/transactions");
                }
            }
        }

        if (coThayDoi) await context.SaveChangesAsync(ct);

        await NhacHoaDonSapDenHanAsync(context, ct);
        await XuLyPremiumHetHanAsync(context, ct);
    }

    // Hộ gia đình là tính năng Premium: MỌI thành viên (kể cả chủ hộ) đều phải tự có Premium còn
    // hiệu lực. Hàm này chạy định kỳ cùng nhịp với job nhắc nhở ở trên để:
    //   1. Nhắc trước 3 ngày (mỗi ngày 1 lần) cho thành viên hộ gia đình có Premium sắp hết hạn,
    //      để họ biết cần gia hạn nếu không sẽ tự động bị đưa ra khỏi hộ.
    //   2. Khi Premium đã hết hạn thật sự (PremiumConHieuLuc == false) mà vẫn còn trong hộ:
    //      - Nếu là CHỦ HỘ: giải tán toàn bộ hộ (không có tính năng chuyển quyền chủ hộ), các
    //        thành viên còn lại đều bị đưa ra và nhận thông báo.
    //      - Nếu là thành viên thường: chỉ người đó bị đưa ra khỏi hộ, chủ hộ nhận thông báo.
    private static async Task XuLyPremiumHetHanAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var homNay = DateTime.Today;

        // ---- 1. Nhắc trước khi hết hạn (còn đúng 3 ngày) ----
        var ngayNhac = homNay.AddDays(3);
        var thanhVienSapHetHan = await context.Users
            .Where(u => u.HouseholdId != null && u.IsPremium
                && u.PremiumHetHan != null && u.PremiumHetHan.Value.Date == ngayNhac)
            .ToListAsync(ct);

        foreach (var user in thanhVienSapHetHan)
        {
            var tieuDe = "Premium sắp hết hạn - hộ gia đình";
            if (await ThongBaoHelper.DaCoHomNayAsync(context, user.Id, tieuDe)) continue;

            await ThongBaoHelper.TaoAsync(context, user.Id, tieuDe,
                $"Gói Premium của bạn hết hạn ngày {user.PremiumHetHan:dd/MM/yyyy}. Nếu không gia hạn, " +
                "bạn sẽ tự động bị đưa ra khỏi hộ gia đình đang tham gia.", LoaiThongBao.Premium, "/premium");
        }

        // ---- 2. Premium đã thực sự hết hạn - tự động đưa ra khỏi hộ ----
        var thanhVienHetHan = await context.Users
            .Include(u => u.Household)
            .Where(u => u.HouseholdId != null
                && (!u.IsPremium || u.PremiumHetHan == null || u.PremiumHetHan.Value.Date < homNay))
            .ToListAsync(ct);

        foreach (var user in thanhVienHetHan)
        {
            var hoGiaDinh = user.Household;
            if (hoGiaDinh == null) { user.HouseholdId = null; continue; }

            if (hoGiaDinh.ChuHoId == user.Id)
            {
                // Chủ hộ hết Premium => giải tán toàn bộ hộ
                var thanhVienKhac = await context.Users.Where(u => u.HouseholdId == hoGiaDinh.Id && u.Id != user.Id).ToListAsync(ct);
                foreach (var tv in thanhVienKhac)
                {
                    tv.HouseholdId = null;
                    await ThongBaoHelper.TaoAsync(context, tv.Id, "Hộ gia đình đã bị giải tán",
                        $"Hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\" đã tự động giải tán do Premium của chủ hộ đã hết hạn.",
                        LoaiThongBao.HoGiaDinh);
                }

                await ThongBaoHelper.TaoAsync(context, user.Id, "Hộ gia đình đã bị giải tán",
                    $"Premium của bạn đã hết hạn nên hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\" đã tự động giải tán.",
                    LoaiThongBao.HoGiaDinh, "/premium");

                user.HouseholdId = null;
                context.Households.Remove(hoGiaDinh);
            }
            else
            {
                // Thành viên thường hết Premium => chỉ người đó bị đưa ra khỏi hộ
                var tenNguoiRoi = user.HoTen ?? user.UserName ?? user.Email ?? "Một thành viên";
                await ThongBaoHelper.TaoAsync(context, hoGiaDinh.ChuHoId, "Thành viên đã rời hộ",
                    $"{tenNguoiRoi} đã tự động rời khỏi hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\" do Premium đã hết hạn.",
                    LoaiThongBao.HoGiaDinh, "/household");

                await ThongBaoHelper.TaoAsync(context, user.Id, "Bạn đã rời hộ gia đình",
                    $"Premium của bạn đã hết hạn nên bạn đã tự động rời khỏi hộ gia đình \"{hoGiaDinh.TenHoGiaDinh}\".",
                    LoaiThongBao.HoGiaDinh, "/premium");

                user.HouseholdId = null;
            }
        }

        if (thanhVienHetHan.Count > 0) await context.SaveChangesAsync(ct);
    }

    // Tạo thông báo trong app (mục Thông báo - chuông ở Trang chủ) cho các hóa đơn sắp đến hạn
    // trong vòng 2 ngày tới và chưa thanh toán - tối đa 1 thông báo/hóa đơn/ngày (tránh trùng lặp).
    private static async Task NhacHoaDonSapDenHanAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var homNay = DateTime.Today;
        var hanChot = homNay.AddDays(2);

        var hoaDonSapDenHan = await context.Bills
            .Where(b => !b.DaThanhToan && b.NgayDenHan >= homNay && b.NgayDenHan <= hanChot)
            .ToListAsync(ct);

        foreach (var bill in hoaDonSapDenHan)
        {
            var tieuDe = $"Hóa đơn sắp đến hạn: {bill.TenHoaDon}";
            if (await ThongBaoHelper.DaCoHomNayAsync(context, bill.UserId, tieuDe)) continue;

            await ThongBaoHelper.TaoAsync(context, bill.UserId, tieuDe,
                $"Hóa đơn \"{bill.TenHoaDon}\" ({bill.SoTien:N0}đ) đến hạn ngày {bill.NgayDenHan:dd/MM/yyyy}.",
                LoaiThongBao.HoaDon, "/bills");
        }
    }

    private static Task<bool> NenGuiBaoCaoAsync(ApplicationUser user, DateTime bayGio)
    {
        var khoangCach = user.TanSuatBaoCao == TanSuatBaoCao.HangNgay ? TimeSpan.FromDays(1) : TimeSpan.FromDays(7);
        var daDenHan = user.LanCuoiGuiBaoCao == null || (bayGio - user.LanCuoiGuiBaoCao.Value) >= khoangCach;
        var trongKhungGio = Math.Abs((bayGio.TimeOfDay - user.GioBaoCao).TotalMinutes) <= 15;
        return Task.FromResult(daDenHan && trongKhungGio);
    }

    private static async Task<int> TinhSoNgayKhongGhiNhanAsync(ApplicationDbContext context, ApplicationUser user, CancellationToken ct)
    {
        var ngayGanNhat = await context.Transactions
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.NgayGiaoDich)
            .Select(t => (DateTime?)t.NgayGiaoDich)
            .FirstOrDefaultAsync(ct);

        var mocThoiGian = ngayGanNhat?.Date ?? user.NgayTao.Date;
        return (DateTime.Today - mocThoiGian).Days;
    }

    private static async Task GuiBaoCaoTaiChinhAsync(
        ApplicationDbContext context, IEmailService emailService, ApplicationUser user, CancellationToken ct)
    {
        var tuNgay = user.TanSuatBaoCao == TanSuatBaoCao.HangNgay ? DateTime.Today.AddDays(-1) : DateTime.Today.AddDays(-7);

        var giaoDichs = await context.Transactions
            .Where(t => t.UserId == user.Id && t.NgayGiaoDich >= tuNgay)
            .ToListAsync(ct);

        var tongThu = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien);
        var tongChi = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien);
        var kyBaoCao = user.TanSuatBaoCao == TanSuatBaoCao.HangNgay ? "hôm qua" : "7 ngày qua";

        var noiDung = $@"
            <p>Xin chào {user.HoTen ?? user.Email},</p>
            <p>Đây là báo cáo tài chính <strong>{kyBaoCao}</strong> của bạn trên MoneyCare:</p>
            <ul>
                <li>Tổng thu: <strong>{tongThu:N0} đ</strong></li>
                <li>Tổng chi: <strong>{tongChi:N0} đ</strong></li>
                <li>Số dư: <strong>{(tongThu - tongChi):N0} đ</strong></li>
                <li>Số giao dịch: {giaoDichs.Count}</li>
            </ul>
            <p>Xem chi tiết trong ứng dụng MoneyCare.</p>";

        await emailService.GuiEmailAsync(user.Email!, $"[MoneyCare] Báo cáo tài chính {kyBaoCao}", noiDung);
    }
}
