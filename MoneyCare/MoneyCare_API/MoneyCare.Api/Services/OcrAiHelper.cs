// Logic dùng chung giữa OcrController (web) và OcrApiController (app Flutter): sau khi Tesseract
// đọc được văn bản thô + tự dò số tiền/ngày bằng biểu thức chính quy (quy tắc), gọi thêm AI (LM Studio)
// để phân tích lại cho chính xác hơn và gợi ý thêm loại thu/chi, tên cửa hàng, danh mục, ghi chú.
using System.Text.RegularExpressions;
using MoneyCare.Models;

namespace MoneyCare.Services;

public static class OcrAiHelper
{
    /// <summary>
    /// Kiểm tra 1 ngày nhận diện được (từ regex quy tắc HOẶC từ AI) có hợp lý để là ngày phát sinh
    /// hóa đơn hay không: hóa đơn không thể được lập ở TƯƠNG LAI (sau hôm nay), và cũng không hợp lý
    /// nếu quá cũ (>2 năm) - cả 2 trường hợp này gần như chắc chắn là đọc/nhận diện NHẦM (VD: regex
    /// bắt nhầm mã hóa đơn/số điện thoại/hạn sử dụng thành ngày tháng, hoặc AI "ảo giác" ra 1 ngày
    /// không có thật - lỗi này khiến giao dịch vẫn được lưu bình thường nhưng ngày sai lệch hẳn khỏi
    /// mọi khoảng lọc theo tháng/"đến hôm nay", khiến người dùng tưởng giao dịch "biến mất"/không được
    /// lưu). Ngày không hợp lý sẽ bị loại bỏ (trả về null), Web sẽ tự rơi về mặc định "hôm nay".
    /// </summary>
    public static DateTime? NgayHopLyHoacNull(DateTime? ngay)
    {
        if (ngay == null) return null;
        var homNay = DateTime.Today;
        if (ngay.Value.Date > homNay || ngay.Value.Date < homNay.AddYears(-2)) return null;
        return ngay;
    }

    /// <summary>
    /// Gọi AI phân tích <paramref name="ketQua"/>.VanBanGoc rồi GHI ĐÈ trực tiếp lên các trường của
    /// <paramref name="ketQua"/> khi AI có kết quả (ưu tiên AI vì đọc hiểu ngữ cảnh tốt hơn regex thuần).
    /// Nếu AI không khả dụng (LM Studio chưa bật Server / lỗi mạng) thì GIỮ NGUYÊN kết quả quy tắc cũ
    /// - tính năng quét hóa đơn vẫn hoạt động bình thường, chỉ là không có gợi ý thông minh thêm.
    /// Riêng "tên cửa hàng": nếu AI không trả về (hoặc AI không khả dụng), LUÔN thử thêm 1 lần dò
    /// bằng quy tắc (thường hóa đơn VN in tên đơn vị ngay ở vài dòng đầu) để tránh để trống "-".
    /// </summary>
    /// <param name="tatCaDanhMucCuaUser">Toàn bộ danh mục (cả Thu và Chi) của người dùng.</param>
    public static async Task LamGiauKetQuaBangAiAsync(ILlmService llmService, KetQuaOcrHoaDon ketQua, List<Category> tatCaDanhMucCuaUser)
    {
        if (string.IsNullOrWhiteSpace(ketQua.VanBanGoc)) return;

        var danhMucThu = tatCaDanhMucCuaUser.Where(c => c.Loai == LoaiGiaoDich.Thu).ToList();
        var danhMucChi = tatCaDanhMucCuaUser.Where(c => c.Loai == LoaiGiaoDich.Chi).ToList();

        var ketQuaAi = await llmService.PhanTichHoaDonAsync(
            ketQua.VanBanGoc,
            danhMucThu.Select(c => c.TenDanhMuc),
            danhMucChi.Select(c => c.TenDanhMuc));

        if (ketQuaAi == null)
        {
            ketQua.NguonPhanTich = "QuyTac";
            ketQua.TenCuaHang = DoTenCuaHangBangQuyTac(ketQua.VanBanGoc);
            return;
        }

        if (ketQuaAi.SoTien is > 0) ketQua.SoTienNhanDien = ketQuaAi.SoTien;
        var ngayAi = NgayHopLyHoacNull(ketQuaAi.Ngay);
        if (ngayAi != null) ketQua.NgayNhanDien = ngayAi;
        // AI đôi khi trả về null cho tên cửa hàng dù văn bản có đủ thông tin (model nhỏ, chạy CPU đôi
        // lúc "lười") -> luôn có lưới an toàn bằng quy tắc để không để trống "-" trên giao diện.
        ketQua.TenCuaHang = !string.IsNullOrWhiteSpace(ketQuaAi.TenCuaHang)
            ? ketQuaAi.TenCuaHang
            : DoTenCuaHangBangQuyTac(ketQua.VanBanGoc);
        ketQua.GhiChuGoiY = ketQuaAi.GhiChuGoiY;
        ketQua.NguonPhanTich = "AI";

        // Mặc định vẫn là Chi (an toàn nhất) - chỉ đổi sang Thu khi AI khẳng định rõ ràng.
        ketQua.LoaiGoiY = string.Equals(ketQuaAi.Loai, "Thu", StringComparison.OrdinalIgnoreCase)
            ? LoaiGiaoDich.Thu
            : LoaiGiaoDich.Chi;

        var danhMucTheoLoai = ketQua.LoaiGoiY == LoaiGiaoDich.Thu ? danhMucThu : danhMucChi;
        var danhMucGoiY = TimDanhMucPhuHop(danhMucTheoLoai, ketQuaAi.DanhMucGoiY, ketQua.VanBanGoc);
        if (danhMucGoiY != null)
        {
            ketQua.DanhMucGoiYId = danhMucGoiY.Id;
            ketQua.DanhMucGoiYTen = danhMucGoiY.TenDanhMuc;
        }
    }

    /// <summary>Từ khóa hay xuất hiện đầu tên đơn vị/cửa hàng phát hành hóa đơn tại Việt Nam.</summary>
    private static readonly string[] TuKhoaDonVi =
    [
        "CÔNG TY", "CTY", "TNHH", "CỔ PHẦN", "SIÊU THỊ", "CỬA HÀNG", "NHÀ HÀNG", "QUÁN",
        "DOANH NGHIỆP", "TẬP ĐOÀN", "CHI NHÁNH", "TRUNG TÂM", "PHÒNG KHÁM", "BỆNH VIỆN"
    ];

    /// <summary>
    /// Dò tên cửa hàng/đơn vị phát hành bằng quy tắc (không cần AI): hầu hết hóa đơn/biên nhận tại
    /// Việt Nam in tên đơn vị ngay trong vài dòng đầu tiên của văn bản. Ưu tiên dòng có chứa từ khóa
    /// loại hình đơn vị (VD "CÔNG TY", "CỬA HÀNG"...), nếu dòng đó ngắn và dòng kế tiếp cũng ngắn
    /// (nhiều khả năng OCR ngắt tên đơn vị thành 2 dòng) thì ghép lại cho đủ nghĩa.
    /// </summary>
    private static string? DoTenCuaHangBangQuyTac(string vanBanGoc)
    {
        var dong = vanBanGoc.Split('\n')
            .Select(d => d.Trim().Trim(',', '.', ';', ':', '"', '\'', '`', '.', '-'))
            .Where(d => d.Length >= 3 && Regex.IsMatch(d, "[A-Za-zÀ-ỹ]"))
            .Take(8)
            .ToList();

        if (dong.Count == 0) return null;

        var idxCoTuKhoa = dong.FindIndex(d => TuKhoaDonVi.Any(tk => d.Contains(tk, StringComparison.OrdinalIgnoreCase)));
        if (idxCoTuKhoa >= 0)
        {
            var dongGoc = dong[idxCoTuKhoa];
            // Dòng có từ khóa nhưng khá ngắn (VD chỉ "CÔNG TY CỔ PHẦN") và dòng kế tiếp cũng ngắn,
            // không chứa nhiều số (không phải địa chỉ/mã số thuế) -> nhiều khả năng là phần tiếp nối tên.
            if (dongGoc.Length <= 30 && idxCoTuKhoa + 1 < dong.Count)
            {
                var dongKe = dong[idxCoTuKhoa + 1];
                if (dongKe.Length <= 45 && Regex.Matches(dongKe, @"\d").Count < 4)
                {
                    return $"{dongGoc} {dongKe}".Trim();
                }
            }
            return dongGoc;
        }

        // Không dòng nào có từ khóa rõ ràng -> lấy dòng đầu tiên "hợp lý" (không quá ngắn/toàn số/toàn ký hiệu)
        return dong.FirstOrDefault(d => d.Length is >= 4 and <= 60 && !Regex.IsMatch(d, @"^[\d\W]+$"));
    }

    /// <summary>Từ điển từ khóa hóa đơn tiện ích phổ biến -> các từ khóa tên danh mục nên ưu tiên tìm.</summary>
    private static readonly (string[] TuKhoaHoaDon, string[] TuKhoaTenDanhMuc)[] TuKhoaTienIch =
    [
        (["tiền nước", "cấp nước", "hóa đơn nước"], ["nước", "tiện ích", "hóa đơn"]),
        (["tiền điện", "hóa đơn điện", "evn", "điện lực"], ["điện", "tiện ích", "hóa đơn"]),
        (["internet", "wifi", "cước mạng", "truyền hình cáp"], ["internet", "mạng", "tiện ích", "hóa đơn"]),
        (["điện thoại", "viễn thông", "cước phí"], ["điện thoại", "viễn thông", "tiện ích", "hóa đơn"]),
        (["tiền gas", "ga công nghiệp", "bình gas"], ["gas", "ga", "tiện ích", "hóa đơn"]),
    ];

    /// <summary>
    /// Tìm 1 danh mục phù hợp nhất trong <paramref name="danhMucTheoLoai"/>, thử lần lượt:
    /// (1) khớp chính xác tên AI gợi ý, (2) khớp gần đúng (chứa 1 phần tên), (3) dò từ khóa hóa đơn
    /// tiện ích phổ biến (điện/nước/internet/điện thoại/gas) trực tiếp trong văn bản OCR gốc - phòng khi
    /// AI không gợi ý được danh mục nào dù nội dung hóa đơn đã khá rõ ràng.
    /// </summary>
    private static Category? TimDanhMucPhuHop(List<Category> danhMucTheoLoai, string? tenAiGoiY, string vanBanGoc)
    {
        if (danhMucTheoLoai.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(tenAiGoiY))
        {
            var khopChinhXac = danhMucTheoLoai.FirstOrDefault(
                c => string.Equals(c.TenDanhMuc, tenAiGoiY, StringComparison.OrdinalIgnoreCase));
            if (khopChinhXac != null) return khopChinhXac;

            var khopGanDung = danhMucTheoLoai.FirstOrDefault(c =>
                c.TenDanhMuc.Contains(tenAiGoiY, StringComparison.OrdinalIgnoreCase) ||
                tenAiGoiY.Contains(c.TenDanhMuc, StringComparison.OrdinalIgnoreCase));
            if (khopGanDung != null) return khopGanDung;
        }

        var vanBanThuong = vanBanGoc.ToLowerInvariant();
        foreach (var (tuKhoaHoaDon, tuKhoaTenDanhMuc) in TuKhoaTienIch)
        {
            if (!tuKhoaHoaDon.Any(vanBanThuong.Contains)) continue;
            foreach (var tuKhoaTen in tuKhoaTenDanhMuc)
            {
                var danhMuc = danhMucTheoLoai.FirstOrDefault(c => c.TenDanhMuc.Contains(tuKhoaTen, StringComparison.OrdinalIgnoreCase));
                if (danhMuc != null) return danhMuc;
            }
        }

        return null;
    }

    /// <summary>
    /// Ghi chú điền sẵn cho form xác nhận: ưu tiên câu AI tóm tắt, kế đến tên cửa hàng AI/quy tắc dò được,
    /// cuối cùng mới rơi về 1 câu chung chung theo ngày - để "Ghi chú" luôn có nội dung cho người dùng
    /// kiểm tra lại thay vì để trống.
    /// </summary>
    public static string TaoGhiChuMacDinh(KetQuaOcrHoaDon ketQua)
    {
        if (!string.IsNullOrWhiteSpace(ketQua.GhiChuGoiY)) return ketQua.GhiChuGoiY!;
        if (!string.IsNullOrWhiteSpace(ketQua.TenCuaHang)) return $"Hóa đơn {ketQua.TenCuaHang}";
        var ngay = ketQua.NgayNhanDien ?? DateTime.Today;
        return $"Hóa đơn quét ngày {ngay:dd/MM/yyyy}";
    }
}
