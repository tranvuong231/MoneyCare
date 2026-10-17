// Giao diện định nghĩa các chức năng nhận dạng văn bản (OCR).
using MoneyCare.Models;

namespace MoneyCare.Services;

public class KetQuaOcrHoaDon
{
    public string VanBanGoc { get; set; } = string.Empty;
    public decimal? SoTienNhanDien { get; set; }
    public DateTime? NgayNhanDien { get; set; }

    // ==== Bổ sung bởi AI (LM Studio) sau khi có văn bản OCR thô - xem OcrController/OcrApiController ====
    public string? TenCuaHang { get; set; }
    public string? GhiChuGoiY { get; set; }
    public int? DanhMucGoiYId { get; set; }
    public string? DanhMucGoiYTen { get; set; }

    /// <summary>Loại giao dịch (Thu/Chi) AI gợi ý - mặc định Chi vì đa số hóa đơn quét được là chi tiêu.</summary>
    public LoaiGiaoDich LoaiGoiY { get; set; } = LoaiGiaoDich.Chi;

    /// <summary>"AI" nếu LM Studio phân tích thành công, "QuyTac" nếu chỉ dùng biểu thức chính quy.</summary>
    public string NguonPhanTich { get; set; } = "QuyTac";
}

public interface IOcrService
{
    KetQuaOcrHoaDon QuetHoaDon(Stream anhHoaDon);
}
