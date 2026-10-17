// Cung cấp chức năng nhận dạng văn bản từ hình ảnh bằng thư viện Tesseract OCR.
using System.Text.RegularExpressions;
using Tesseract;

namespace MoneyCare.Services;

/// <summary>
/// Dịch vụ OCR quét hóa đơn dùng Tesseract OCR (mã nguồn mở, chạy HOÀN TOÀN OFFLINE,
/// không cần API key, không tốn phí, không gửi ảnh ra ngoài Internet).
/// YÊU CẦU: cần tải file dữ liệu ngôn ngữ (.traineddata) đặt vào thư mục /tessdata
/// ở gốc dự án. Xem hướng dẫn chi tiết trong README.md mục "OCR quét hóa đơn".
/// Nếu muốn nhận diện chính xác hơn (đặc biệt hóa đơn tiếng Việt/font khó), có thể
/// thay thế bằng Google Cloud Vision API hoặc Azure Computer Vision (cần API key trả phí)
/// bằng cách implement IOcrService.QuetHoaDon() theo interface tương tự.
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly string _duongDanTessdata;

    public TesseractOcrService(IWebHostEnvironment env)
    {
        _duongDanTessdata = Path.Combine(env.ContentRootPath, "tessdata");
    }

    public KetQuaOcrHoaDon QuetHoaDon(Stream anhHoaDon)
    {
        using var ms = new MemoryStream();
        anhHoaDon.CopyTo(ms);
        var bytes = ms.ToArray();

        string vanBan;
        try
        {
            using var engine = new TesseractEngine(_duongDanTessdata, "eng+vie", EngineMode.Default);
            using var img = Pix.LoadFromMemory(bytes);
            using var page = engine.Process(img);
            vanBan = page.GetText();
        }
        catch (TesseractException ex)
        {
            throw new InvalidOperationException(
                "Không thể khởi tạo Tesseract OCR. Vui lòng kiểm tra đã tải file eng.traineddata " +
                "và vie.traineddata vào thư mục /tessdata theo hướng dẫn trong README.md. " +
                $"Chi tiết lỗi: {ex.Message}", ex);
        }

        return new KetQuaOcrHoaDon
        {
            VanBanGoc = vanBan,
            SoTienNhanDien = TrichXuatSoTien(vanBan),
            NgayNhanDien = OcrAiHelper.NgayHopLyHoacNull(TrichXuatNgay(vanBan))
        };
    }

    private static decimal? TrichXuatSoTien(string vanBan)
    {
        // Tìm dòng có từ khóa liên quan tới tổng tiền rồi lấy số lớn nhất gần đó,
        // hoặc fallback: lấy số tiền lớn nhất xuất hiện trong toàn bộ hóa đơn
        var dongCoTuKhoa = vanBan.Split('\n')
            .Where(dong => Regex.IsMatch(dong,
                "tổng|tong|total|thanh toán|thanh toan|tong cong|tổng cộng|thành tiền|thanh tien|amount|grand total|cần trả|can tra|tiền mặt|tien mat",
                RegexOptions.IgnoreCase))
            .ToList();

        var ungVien = new List<decimal>();
        var nguonTim = dongCoTuKhoa.Any() ? dongCoTuKhoa : vanBan.Split('\n').ToList();

        foreach (var dong in nguonTim)
        {
            // Bắt cả số có dấu chấm/phẩy phân cách hàng nghìn, số liền không dấu, và số có hậu tố đ/vnd
            foreach (Match m in Regex.Matches(dong, @"[\d]{1,3}([.,]\d{3})+(?=\D|$)|\d{4,}(?=\s*(đ|d|vnd)?\b)"))
            {
                var sach = m.Value.Replace(".", "").Replace(",", "");
                if (decimal.TryParse(sach, out var giaTri) && giaTri >= 1000 && giaTri < 1_000_000_000)
                {
                    ungVien.Add(giaTri);
                }
            }
        }

        // Ưu tiên số tiền tìm được ở dòng có từ khóa (thường là dòng cuối/tổng cộng);
        // nếu nhiều ứng viên, lấy số lớn nhất vì tổng tiền luôn >= các dòng chi tiết bên trên
        return ungVien.Any() ? ungVien.Max() : null;
    }

    private static DateTime? TrichXuatNgay(string vanBan)
    {
        var match = Regex.Match(vanBan, @"\b(\d{1,2})[/\-](\d{1,2})[/\-](\d{2,4})\b");
        if (match.Success)
        {
            var ngay = int.Parse(match.Groups[1].Value);
            var thang = int.Parse(match.Groups[2].Value);
            var namStr = match.Groups[3].Value;
            var nam = int.Parse(namStr.Length == 2 ? "20" + namStr : namStr);

            try { return new DateTime(nam, thang, ngay); }
            catch { return null; }
        }
        return null;
    }
}
