// Sinh sẵn 1 đoạn HTML <i> icon nhỏ (Bootstrap Icons) đặt cạnh mỗi giao dịch trong danh sách
// "Giao dịch gần đây" / "Giao dịch thu chi" để phân biệt: giao dịch người dùng tự nhập tay (không
// hiện icon) với giao dịch được HỆ THỐNG tự động tạo/đồng bộ (từ Hóa đơn, ZaloPay, Ngân hàng, MoMo,
// hoặc quét OCR) - kèm màu sắc thể hiện trạng thái đồng bộ (Thành công/Đang xử lý/Thất bại).
using MoneyCare.Models;

namespace MoneyCare.Helpers;

public static class GiaoDichIconHelper
{
    /// <summary>Trả về HTML icon (rỗng nếu là giao dịch nhập tay - không cần hiện gì thêm).</summary>
    public static string HtmlIconNguon(Transaction gd)
    {
        if (gd.Nguon == NguonGiaoDich.ThuCong) return string.Empty;

        var (bieuTuong, nhan) = gd.Nguon switch
        {
            NguonGiaoDich.HoaDon => ("bi-receipt", "Tự động từ Hóa đơn"),
            NguonGiaoDich.ZaloPay => ("bi-wallet2", "Đồng bộ từ ZaloPay"),
            NguonGiaoDich.NganHang => ("bi-bank", "Đồng bộ từ Ngân hàng"),
            NguonGiaoDich.MoMo => ("bi-wallet2", "Đồng bộ từ MoMo"),
            NguonGiaoDich.QuetOcr => ("bi-camera", "Quét hóa đơn (OCR + AI)"),
            _ => ("bi-arrow-repeat", "Tự động")
        };

        var (mauTrangThai, nhanTrangThai, bieuTuongTrangThai) = gd.TrangThaiDongBo switch
        {
            TrangThaiDongBoGiaoDich.DangXuLy => ("text-warning", "Đang xử lý", "bi-hourglass-split"),
            TrangThaiDongBoGiaoDich.ThatBai => ("text-danger", "Thất bại", "bi-exclamation-triangle-fill"),
            _ => ("text-success", "Thành công", "bi-check-circle-fill")
        };

        var chuThich = $"{nhan} · {nhanTrangThai}";
        return $"<span class=\"d-inline-flex align-items-center gap-1\" title=\"{chuThich}\">" +
               $"<i class=\"bi {bieuTuong} text-muted\"></i>" +
               $"<i class=\"bi {bieuTuongTrangThai} {mauTrangThai}\" style=\"font-size:.65rem;\"></i>" +
               $"</span>";
    }
}
