namespace MoneyCare.Models;

/// <summary>Nguồn phát sinh 1 giao dịch - dùng để hiển thị icon/nhãn phân biệt giao dịch tự động
/// đồng bộ (ZaloPay, hóa đơn, ngân hàng...) với giao dịch người dùng tự tay nhập.</summary>
public enum NguonGiaoDich
{
    /// <summary>Người dùng tự nhập tay (mặc định) - qua form Thêm giao dịch.</summary>
    ThuCong = 0,

    /// <summary>Tự động tạo khi đánh dấu 1 Hóa đơn (Bill) là "Đã thanh toán".</summary>
    HoaDon = 1,

    /// <summary>Tự động đồng bộ từ thanh toán ZaloPay UAT (xác thực liên kết hoặc nâng cấp Premium).</summary>
    ZaloPay = 2,

    /// <summary>Tự động đồng bộ từ liên kết Ngân hàng (khi có API thật).</summary>
    NganHang = 3,

    /// <summary>Tự động đồng bộ từ liên kết ví MoMo (khi có API thật).</summary>
    MoMo = 4,

    /// <summary>Người dùng quét hóa đơn bằng OCR + AI rồi xác nhận lưu.</summary>
    QuetOcr = 5
}

/// <summary>Trạng thái đồng bộ (chỉ có ý nghĩa với giao dịch có Nguon khác ThuCong).</summary>
public enum TrangThaiDongBoGiaoDich
{
    DangXuLy = 0,
    ThanhCong = 1,
    ThatBai = 2
}
