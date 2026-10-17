// Helper hiển thị mốc thời gian giao dịch theo định dạng CỐ ĐỊNH giờ:phút - ngày/tháng/năm
// (VD: "16:02 - 23/08/2026") dùng chung cho Tổng quan và danh sách Giao dịch.
using System.Globalization;

namespace MoneyCare.Helpers;

public static class ThoiGianHelper
{
    /// <summary>Định dạng cố định "HH:mm - dd/MM/yyyy", dễ đọc và nhất quán, không phụ thuộc vào
    /// thời điểm xem (khác với hiển thị tương đối kiểu "Hôm nay/Hôm qua/Thứ...").</summary>
    public static string HienThiGioNgay(DateTime thoiGian) =>
        thoiGian.ToString("HH:mm - dd/MM/yyyy", CultureInfo.InvariantCulture);
}
