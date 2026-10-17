// Tuần 7: sinh mã QR chuyển khoản chuẩn VietQR (QrPaymentService) và chatbot hỗ trợ người dùng dạng
// so khớp từ khóa, chạy offline không cần AI ngoài (ChatbotService).
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week07_QrAndChatbotTests
{
    // ==== QrPaymentService ====

    [Fact]
    public void TaoAnhVietQR_TraVeAnhPngHopLe_KhongRong()
    {
        var service = new QrPaymentService();

        var bytes = service.TaoAnhVietQR("0071001234567", "970436", 150000, "Chuyen tien an uong");

        Assert.NotEmpty(bytes);
        // Chữ ký PNG: 8 byte đầu cố định là 89 50 4E 47 0D 0A 1A 0A.
        byte[] chuKyPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Assert.Equal(chuKyPng, bytes.Take(8));
    }

    [Fact]
    public void TaoAnhVietQR_VanSinhAnhHopLe_KhiKhongCoSoTienVaNoiDung()
    {
        // Số tiền (field 54) và nội dung (field 62) đều KHÔNG bắt buộc trong đặc tả VietQR
        // -> vẫn phải sinh được ảnh, chỉ là mã QR "tĩnh" (người quét tự nhập số tiền).
        var service = new QrPaymentService();

        var bytes = service.TaoAnhVietQR("0071001234567", "970436", 0, "");

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void TaoAnhTuChuoi_SinhAnhKhacNhau_ChoNoiDungKhacNhau()
    {
        var service = new QrPaymentService();

        var anh1 = service.TaoAnhTuChuoi("noi dung A");
        var anh2 = service.TaoAnhTuChuoi("noi dung B hoan toan khac");

        Assert.NotEqual(anh1, anh2);
    }

    // ==== ChatbotService ====

    [Fact]
    public void TraLoi_CauHoiRong_TraVeLoiChaoGoiY()
    {
        var chatbot = new ChatbotService();

        var traLoi = chatbot.TraLoi("");

        Assert.Contains("thêm giao dịch", traLoi, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TraLoi_KhopChinhXacCauHoiMau_TraVeDungCauTraLoi()
    {
        var chatbot = new ChatbotService();

        var traLoi = chatbot.TraLoi("Làm sao để tạo danh mục mới?");

        Assert.Contains("Thêm danh mục", traLoi);
    }

    [Fact]
    public void TraLoi_KhopTheoTuKhoa_KhongDauVaSaiChinhTaNhe_VanNhanDienDuoc()
    {
        var chatbot = new ChatbotService();

        // Không dấu, viết tắt, khác hẳn câu hỏi mẫu -> vẫn phải khớp nhờ chấm điểm từ khóa "premium"
        var traLoi = chatbot.TraLoi("cho hoi goi premium co gi hay khong");

        Assert.Contains("Premium", traLoi);
    }

    [Fact]
    public void TraLoi_CauHoiKhongLienQuan_TraVeCauTraLoiMacDinh()
    {
        var chatbot = new ChatbotService();

        var traLoi = chatbot.TraLoi("hôm nay thời tiết thế nào");

        Assert.Contains("Xin lỗi", traLoi);
    }

    [Fact]
    public void LayCauHoiGoiY_TraVeDanhSachKhongRong()
    {
        var chatbot = new ChatbotService();

        var danhSach = chatbot.LayCauHoiGoiY();

        Assert.NotEmpty(danhSach);
        Assert.All(danhSach, c => Assert.False(string.IsNullOrWhiteSpace(c.CauHoi)));
    }
}
