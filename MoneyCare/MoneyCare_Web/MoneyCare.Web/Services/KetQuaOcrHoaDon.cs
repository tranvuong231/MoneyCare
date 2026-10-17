// Bản sao NHẸ chỉ để giữ nguyên Views/Ocr/KetQua.cshtml (view cast ViewBag.KetQuaOcr về đúng kiểu
// này) - logic OCR/AI thật sự chạy ở MoneyCare_API (Services/TesseractOcrService.cs + OcrAiHelper.cs).
using MoneyCare.Models;

namespace MoneyCare.Services;

public class KetQuaOcrHoaDon
{
    public string VanBanGoc { get; set; } = string.Empty;
    public decimal? SoTienNhanDien { get; set; }
    public DateTime? NgayNhanDien { get; set; }
    public string? TenCuaHang { get; set; }
    public string? GhiChuGoiY { get; set; }
    public int? DanhMucGoiYId { get; set; }
    public string? DanhMucGoiYTen { get; set; }
    public LoaiGiaoDich LoaiGoiY { get; set; } = LoaiGiaoDich.Chi;
    public string NguonPhanTich { get; set; } = "QuyTac";
}
