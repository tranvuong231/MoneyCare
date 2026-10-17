//Cung cấp chức năng phân tích và gợi ý tài chính bằng trí tuệ nhân tạo (AI).
using Microsoft.EntityFrameworkCore;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.ViewModels;

namespace MoneyCare.Services;

/// <summary>
/// Dịch vụ "AI phân tích tài chính cá nhân".
/// Đây KHÔNG phải là gọi tới một mô hình AI bên ngoài (không cần API key, chạy hoàn toàn offline),
/// mà là một tập hợp thuật toán thống kê/phân tích xu hướng thực thụ trên dữ liệu giao dịch của người dùng:
///   - So sánh chi tiêu theo danh mục giữa các tháng (phát hiện tăng/giảm bất thường)
///   - Phân tích ngày trong tuần chi tiêu nhiều nhất
///   - Hồi quy tuyến tính đơn giản (linear regression) trên chi tiêu 6 tháng gần nhất để dự báo xu hướng
///   - Ngoại suy tuyến tính (linear extrapolation) chi tiêu/thu nhập trung bình mỗi ngày để dự đoán số dư cuối tháng
/// Nếu muốn nâng cấp thành gọi LLM thật (Claude API) để sinh nhận xét tự nhiên hơn, có thể inject
/// HttpClient gọi tới Anthropic API tại đây, dùng đúng các số liệu đã tính toán được làm ngữ cảnh đầu vào.
/// </summary>
public class AiInsightService : IAiInsightService
{
    private readonly ApplicationDbContext _context;

    public AiInsightService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InsightsViewModel> PhanTichAsync(string userId)
    {
        var vm = new InsightsViewModel();
        var homNay = DateTime.Today;
        var tuNgay = homNay.AddMonths(-6).AddDays(1 - homNay.Day); // đầu tháng, 6 tháng trước

        var giaoDichs = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.NgayGiaoDich >= tuNgay)
            .ToListAsync();

        var chiTieu = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Chi).ToList();
        var thuNhap = giaoDichs.Where(t => t.Loai == LoaiGiaoDich.Thu).ToList();

        // 1. THÓI QUEN CHI TIÊU 
        vm.ThoiQuenChiTieu = PhanTichThoiQuen(chiTieu, homNay);

        // 2. GỢI Ý TIẾT KIỆM 
        vm.GoiYTietKiem = SinhGoiYTietKiem(chiTieu, homNay);

        // 3. DỮ LIỆU BIỂU ĐỒ XU HƯỚNG 6 THÁNG + DỰ BÁO HỒI QUY
        var (nhanThang, thucTe, duBao) = TinhXuHuongVaDuBao(chiTieu, homNay);
        vm.NhanThang = nhanThang;
        vm.ChiTieuThucTe = thucTe;
        vm.ChiTieuDuBao = duBao;

        // 4. DỰ ĐOÁN SỐ DƯ CUỐI THÁNG 
        vm.DuDoan = DuDoanSoDuCuoiThang(giaoDichs, homNay);

        return vm;
    }

    private static List<InsightItem> PhanTichThoiQuen(List<Transaction> chiTieu, DateTime homNay)
    {
        var ketQua = new List<InsightItem>();
        if (!chiTieu.Any())
        {
            ketQua.Add(new InsightItem
            {
                Icon = "bi-info-circle",
                MucDo = "info",
                TieuDe = "Chưa đủ dữ liệu",
                NoiDung = "Hãy ghi nhận thêm giao dịch chi tiêu để hệ thống có thể phân tích thói quen của bạn."
            });
            return ketQua;
        }

        var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
        var dauThangTruoc = dauThangNay.AddMonths(-1);

        var chiThangNay = chiTieu.Where(t => t.NgayGiaoDich >= dauThangNay).ToList();
        var chiThangTruoc = chiTieu.Where(t => t.NgayGiaoDich >= dauThangTruoc && t.NgayGiaoDich < dauThangNay).ToList();

        // So sánh danh mục chi nhiều nhất tháng này với tháng trước 
        var theoDanhMucThangNay = chiThangNay
            .GroupBy(t => t.Category?.TenDanhMuc ?? "Khác")
            .Select(g => new { Ten = g.Key, Tong = g.Sum(t => t.SoTien) })
            .OrderByDescending(x => x.Tong)
            .ToList();

        if (theoDanhMucThangNay.Any())
        {
            var topDanhMuc = theoDanhMucThangNay.First();
            var tongChiThangNay = chiThangNay.Sum(t => t.SoTien);
            var tyLe = tongChiThangNay > 0 ? (topDanhMuc.Tong / tongChiThangNay * 100) : 0;

            ketQua.Add(new InsightItem
            {
                Icon = "bi-pie-chart",
                MucDo = tyLe >= 40 ? "warning" : "info",
                TieuDe = $"Danh mục chi nhiều nhất: {topDanhMuc.Ten}",
                NoiDung = $"Chiếm {tyLe:N0}% tổng chi tiêu tháng này ({topDanhMuc.Tong:N0} đ). " +
                          (tyLe >= 40 ? "Tỷ trọng khá cao, bạn nên xem xét cân đối lại chi tiêu." : "Mức tỷ trọng hợp lý.")
            });
        }

        // So sánh tổng chi tháng này vs tháng trước theo từng danh mục để phát hiện tăng bất thường 
        var theoDanhMucThangTruoc = chiThangTruoc
            .GroupBy(t => t.Category?.TenDanhMuc ?? "Khác")
            .ToDictionary(g => g.Key, g => g.Sum(t => t.SoTien));

        foreach (var dm in theoDanhMucThangNay.Take(5))
        {
            if (theoDanhMucThangTruoc.TryGetValue(dm.Ten, out var thang_truoc) && thang_truoc > 0)
            {
                var phanTramThayDoi = (dm.Tong - thang_truoc) / thang_truoc * 100;
                if (phanTramThayDoi >= 30)
                {
                    ketQua.Add(new InsightItem
                    {
                        Icon = "bi-graph-up-arrow",
                        MucDo = "warning",
                        TieuDe = $"{dm.Ten} tăng {phanTramThayDoi:N0}% so với tháng trước",
                        NoiDung = $"Tháng trước: {thang_truoc:N0} đ → Tháng này: {dm.Tong:N0} đ. Đây là mức tăng đáng chú ý."
                    });
                }
            }
        }

        // Ngày trong tuần chi tiêu nhiều nhất (dựa trên toàn bộ 6 tháng dữ liệu) 
        var theoThu = chiTieu
            .GroupBy(t => t.NgayGiaoDich.DayOfWeek)
            .Select(g => new { Thu = g.Key, Tong = g.Sum(t => t.SoTien), SoLan = g.Count() })
            .OrderByDescending(x => x.Tong)
            .FirstOrDefault();

        if (theoThu != null)
        {
            var tenThu = ThuTiengViet(theoThu.Thu);
            ketQua.Add(new InsightItem
            {
                Icon = "bi-calendar-week",
                MucDo = "info",
                TieuDe = $"Bạn thường chi tiêu nhiều nhất vào {tenThu}",
                NoiDung = $"Tổng cộng {theoThu.Tong:N0} đ qua {theoThu.SoLan} giao dịch trong 6 tháng gần đây."
            });
        }

        // Giao dịch có giá trị lớn bất thường (so với trung bình + 2 lần độ lệch chuẩn) 
        if (chiTieu.Count >= 5)
        {
            var trungBinh = (double)chiTieu.Average(t => t.SoTien);
            var doLechChuan = Math.Sqrt(chiTieu.Sum(t => Math.Pow((double)t.SoTien - trungBinh, 2)) / chiTieu.Count);
            var nguong = trungBinh + 2 * doLechChuan;

            var giaoDichBatThuong = chiTieu.Where(t => (double)t.SoTien > nguong).OrderByDescending(t => t.SoTien).FirstOrDefault();
            if (giaoDichBatThuong != null)
            {
                ketQua.Add(new InsightItem
                {
                    Icon = "bi-exclamation-diamond",
                    MucDo = "danger",
                    TieuDe = "Phát hiện giao dịch chi tiêu lớn bất thường",
                    NoiDung = $"{giaoDichBatThuong.SoTien:N0} đ cho \"{giaoDichBatThuong.Category?.TenDanhMuc}\" " +
                              $"ngày {giaoDichBatThuong.NgayGiaoDich:dd/MM/yyyy}, cao hơn nhiều so với mức chi tiêu trung bình ({trungBinh:N0} đ)."
                });
            }
        }

        return ketQua;
    }

    private static List<InsightItem> SinhGoiYTietKiem(List<Transaction> chiTieu, DateTime homNay)
    {
        var goiY = new List<InsightItem>();
        if (!chiTieu.Any()) return goiY;

        var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
        var chi3ThangTruoc = chiTieu.Where(t => t.NgayGiaoDich < dauThangNay && t.NgayGiaoDich >= dauThangNay.AddMonths(-3)).ToList();
        var chiThangNay = chiTieu.Where(t => t.NgayGiaoDich >= dauThangNay).ToList();

        if (chi3ThangTruoc.Any())
        {
            // Trung bình chi mỗi tháng (3 tháng trước) theo từng danh mục
            var trungBinhThangTruoc = chi3ThangTruoc
                .GroupBy(t => t.Category?.TenDanhMuc ?? "Khác")
                .ToDictionary(g => g.Key, g => g.Sum(t => t.SoTien) / 3m);

            var soNgayDaTrongThang = homNay.Day;
            var soNgayTrongThang = DateTime.DaysInMonth(homNay.Year, homNay.Month);
            var heSoNoiSuy = soNgayDaTrongThang > 0 ? (decimal)soNgayTrongThang / soNgayDaTrongThang : 1;

            var uocTinhThangNay = chiThangNay
                .GroupBy(t => t.Category?.TenDanhMuc ?? "Khác")
                .Select(g => new { Ten = g.Key, UocTinh = g.Sum(t => t.SoTien) * heSoNoiSuy })
                .ToList();

            foreach (var dm in uocTinhThangNay.OrderByDescending(x => x.UocTinh).Take(3))
            {
                if (trungBinhThangTruoc.TryGetValue(dm.Ten, out var tb) && tb > 0 && dm.UocTinh > tb * 1.2m)
                {
                    var tietKiemCoThe = dm.UocTinh - tb;
                    goiY.Add(new InsightItem
                    {
                        Icon = "bi-piggy-bank",
                        MucDo = "success",
                        TieuDe = $"Có thể tiết kiệm ở danh mục \"{dm.Ten}\"",
                        NoiDung = $"Nếu duy trì mức chi như 3 tháng trước ({tb:N0} đ/tháng) thay vì xu hướng hiện tại " +
                                  $"(ước tính {dm.UocTinh:N0} đ), bạn có thể tiết kiệm thêm khoảng {tietKiemCoThe:N0} đ trong tháng này."
                    });
                }
            }
        }

        // Gợi ý quy tắc 50/30/20 dựa trên tổng thu nhập trung bình (nếu có)
        goiY.Add(new InsightItem
        {
            Icon = "bi-stars",
            MucDo = "info",
            TieuDe = "Áp dụng quy tắc 50/30/20",
            NoiDung = "Cân nhắc phân bổ: 50% thu nhập cho nhu cầu thiết yếu, 30% cho chi tiêu cá nhân/giải trí, " +
                      "20% cho tiết kiệm và trả nợ. Vào trang Báo cáo để xem tỷ lệ hiện tại của bạn."
        });

        if (!goiY.Any(g => g.MucDo == "success"))
        {
            goiY.Add(new InsightItem
            {
                Icon = "bi-check-circle",
                MucDo = "success",
                TieuDe = "Chi tiêu của bạn khá ổn định",
                NoiDung = "Không phát hiện danh mục nào tăng bất thường so với 3 tháng trước đó. Tiếp tục duy trì nhé!"
            });
        }

        return goiY;
    }

    private static (List<string>, List<decimal>, List<decimal?>) TinhXuHuongVaDuBao(List<Transaction> chiTieu, DateTime homNay)
    {
        var nhanThang = new List<string>();
        var thucTe = new List<decimal>();

        var diemTheoThang = new List<(int ThuTu, decimal Tong)>();
        for (int i = 5; i >= 0; i--)
        {
            var thang = homNay.AddMonths(-i);
            var dau = new DateTime(thang.Year, thang.Month, 1);
            var cuoi = dau.AddMonths(1).AddDays(-1);
            var tong = chiTieu.Where(t => t.NgayGiaoDich >= dau && t.NgayGiaoDich < cuoi.AddDays(1)).Sum(t => t.SoTien);

            nhanThang.Add($"Th{thang.Month}/{thang.Year % 100}");
            thucTe.Add(tong);
            diemTheoThang.Add((5 - i, tong));
        }

        // Hồi quy tuyến tính đơn giản y = a*x + b trên 6 điểm dữ liệu để dự báo tháng kế tiếp
        var n = diemTheoThang.Count;
        var sumX = diemTheoThang.Sum(d => (double)d.ThuTu);
        var sumY = diemTheoThang.Sum(d => (double)d.Tong);
        var sumXY = diemTheoThang.Sum(d => (double)d.ThuTu * (double)d.Tong);
        var sumX2 = diemTheoThang.Sum(d => (double)d.ThuTu * (double)d.ThuTu);

        var mauSo = n * sumX2 - sumX * sumX;
        double a = 0, b = thucTe.Any() ? (double)thucTe.Average() : 0;
        if (mauSo != 0)
        {
            a = (n * sumXY - sumX * sumY) / mauSo;
            b = (sumY - a * sumX) / n;
        }

        // Dự báo: 5 điểm null (đã là thực tế) + điểm thứ 6 (tháng kế tiếp) là dự báo
        var duBao = new List<decimal?>();
        for (int i = 0; i < n; i++) duBao.Add(null);
        var duBaoThangKeTiep = a * n + b; // x = n (điểm tiếp theo sau index 0..n-1)
        duBao.Add(duBaoThangKeTiep < 0 ? 0 : (decimal)duBaoThangKeTiep);
        nhanThang.Add($"Th{homNay.AddMonths(1).Month}/{homNay.AddMonths(1).Year % 100} (dự báo)");
        thucTe.Add(0); // placeholder để 2 mảng cùng độ dài, view sẽ tự bỏ qua điểm cuối của "thực tế"

        return (nhanThang, thucTe, duBao);
    }

    private static DuDoanSoDu DuDoanSoDuCuoiThang(List<Transaction> giaoDichs, DateTime homNay)
    {
        var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
        var soNgayTrongThang = DateTime.DaysInMonth(homNay.Year, homNay.Month);
        var soNgayDaQua = homNay.Day;
        var soNgayConLai = soNgayTrongThang - soNgayDaQua;

        var giaoDichThangNay = giaoDichs.Where(t => t.NgayGiaoDich >= dauThangNay && t.NgayGiaoDich < homNay.AddDays(1)).ToList();
        var tongThu = giaoDichThangNay.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien);
        var tongChi = giaoDichThangNay.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien);
        var soDuHienTai = tongThu - tongChi;

        // Dùng trung bình 3 tháng gần nhất (đã hoàn thành) để ước tính tốc độ thu/chi mỗi ngày,
        // kết hợp trọng số với tốc độ thực tế của tháng hiện tại để dự báo sát hơn
        var ba_thang_truoc = dauThangNay.AddMonths(-3);
        var giaoDich3ThangTruoc = giaoDichs.Where(t => t.NgayGiaoDich >= ba_thang_truoc && t.NgayGiaoDich < dauThangNay).ToList();

        decimal chiTrungBinhNgayLichSu = 0, thuTrungBinhNgayLichSu = 0;
        if (giaoDich3ThangTruoc.Any())
        {
            var soNgayLichSu = (dauThangNay - ba_thang_truoc).Days;
            chiTrungBinhNgayLichSu = giaoDich3ThangTruoc.Where(t => t.Loai == LoaiGiaoDich.Chi).Sum(t => t.SoTien) / soNgayLichSu;
            thuTrungBinhNgayLichSu = giaoDich3ThangTruoc.Where(t => t.Loai == LoaiGiaoDich.Thu).Sum(t => t.SoTien) / soNgayLichSu;
        }

        var chiTrungBinhNgayThangNay = soNgayDaQua > 0 ? tongChi / soNgayDaQua : 0;
        var thuTrungBinhNgayThangNay = soNgayDaQua > 0 ? tongThu / soNgayDaQua : 0;

        // Trọng số 60% xu hướng thực tế tháng này + 40% trung bình lịch sử (nếu có dữ liệu lịch sử)
        decimal chiTrungBinhNgay, thuTrungBinhNgay;
        if (giaoDich3ThangTruoc.Any())
        {
            chiTrungBinhNgay = chiTrungBinhNgayThangNay * 0.6m + chiTrungBinhNgayLichSu * 0.4m;
            thuTrungBinhNgay = thuTrungBinhNgayThangNay * 0.6m + thuTrungBinhNgayLichSu * 0.4m;
        }
        else
        {
            chiTrungBinhNgay = chiTrungBinhNgayThangNay;
            thuTrungBinhNgay = thuTrungBinhNgayThangNay;
        }

        var netNgay = thuTrungBinhNgay - chiTrungBinhNgay;
        var soDuDuDoan = soDuHienTai + netNgay * soNgayConLai;

        string xuHuong = netNgay > 1000 ? "tang" : netNgay < -1000 ? "giam" : "on_dinh";

        return new DuDoanSoDu
        {
            SoDuHienTai = soDuHienTai,
            ChiTieuTrungBinhNgay = chiTrungBinhNgay,
            ThuNhapTrungBinhNgay = thuTrungBinhNgay,
            SoNgayConLaiTrongThang = soNgayConLai,
            SoDuDuDoanCuoiThang = soDuDuDoan,
            XuHuong = xuHuong
        };
    }

    private static string ThuTiengViet(DayOfWeek thu) => thu switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        _ => "Chủ Nhật"
    };
}
