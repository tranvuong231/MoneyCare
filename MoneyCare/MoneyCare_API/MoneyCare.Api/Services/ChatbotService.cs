// Cung cấp chức năng chatbot hỗ trợ người dùng.
using System.Globalization;
using System.Text;

namespace MoneyCare.Services;

/// <summary>
/// Trợ lý ảo (chatbot) trả lời câu hỏi về cách sử dụng hệ thống MoneyCare.
/// Đây là chatbot dạng "so khớp từ khóa" (rule-based/keyword matching) chạy hoàn toàn offline,
/// KHÔNG gọi API AI bên ngoài nên không tốn phí và không cần API key.
///
/// Muốn nâng cấp thành chatbot AI thực thụ (hiểu ngôn ngữ tự nhiên linh hoạt hơn), có thể thay
/// phần thân hàm TraLoi() bằng lệnh gọi tới Anthropic API (Claude), truyền kèm danh sách
/// CauHoiMau bên dưới làm ngữ cảnh (system prompt) để mô hình trả lời tự nhiên và chính xác hơn.
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly List<CauHoiMau> _khoCauHoi;

    public ChatbotService()
    {
        _khoCauHoi = new List<CauHoiMau>
        {
            new()
            {
                CauHoi = "Làm sao để thêm giao dịch mới?",
                CauTraLoi = "Vào menu <b>Giao dịch</b> ở thanh bên → bấm nút <b>\"Thêm giao dịch\"</b> → chọn Loại (Thu/Chi), Danh mục, nhập Số tiền, Ngày giao dịch rồi bấm Lưu.",
                TuKhoa = new() { "them giao dich", "tao giao dich", "ghi chi tieu", "nhap thu chi", "giao dich moi" }
            },
            new()
            {
                CauHoi = "Làm sao để tạo danh mục mới?",
                CauTraLoi = "Vào menu <b>Danh mục</b> → bấm <b>\"Thêm danh mục\"</b> → đặt tên, chọn Loại (Thu/Chi) và màu sắc hiển thị.",
                TuKhoa = new() { "danh muc", "tao danh muc", "them danh muc" }
            },
            new()
            {
                CauHoi = "Gói Premium có những tính năng gì?",
                CauTraLoi = "Gói Premium (29.000đ/tháng) mở khóa: AI phân tích thói quen chi tiêu, gợi ý tiết kiệm, dự đoán số dư cuối tháng, quét hóa đơn OCR, xuất báo cáo Excel/PDF, quản lý tài chính gia đình và kết nối ngân hàng/ví điện tử.",
                TuKhoa = new() { "premium la gi", "tinh nang premium", "goi premium", "nang cap co gi" }
            },
            new()
            {
                CauHoi = "Làm sao để nâng cấp Premium?",
                CauTraLoi = "Vào menu người dùng góc phải → <b>\"Gói Premium\"</b> → bấm <b>Nâng cấp ngay</b> → quét mã VietQR để chuyển khoản → sau khi chuyển khoản xong, bấm nút <b>\"Tôi đã chuyển khoản\"</b> để kích hoạt.",
                TuKhoa = new() { "nang cap premium", "mua premium", "thanh toan premium", "dang ky premium" }
            },
            new()
            {
                CauHoi = "Làm sao để xuất báo cáo Excel/PDF?",
                CauTraLoi = "Vào menu <b>Xuất Excel/PDF</b> (tính năng Premium) → chọn khoảng thời gian Từ ngày - Đến ngày → bấm nút Xuất Excel hoặc Xuất PDF.",
                TuKhoa = new() { "xuat excel", "xuat pdf", "xuat bao cao", "tai bao cao", "download bao cao" }
            },
            new()
            {
                CauHoi = "Làm sao để thêm hóa đơn nhắc nhở?",
                CauTraLoi = "Vào menu <b>Hóa đơn</b> → bấm <b>\"Thêm hóa đơn\"</b> → nhập tên, số tiền, ngày đến hạn. Hệ thống sẽ tự động cảnh báo khi sắp/đã quá hạn thanh toán.",
                TuKhoa = new() { "hoa don", "nhac nho", "them hoa don", "thanh toan hoa don" }
            },
            new()
            {
                CauHoi = "Làm sao để đổi mật khẩu?",
                CauTraLoi = "Vào menu người dùng góc phải → <b>\"Trang cá nhân\"</b> → bấm <b>\"Đổi mật khẩu\"</b> → nhập mật khẩu hiện tại và mật khẩu mới.",
                TuKhoa = new() { "doi mat khau", "thay doi mat khau", "cap nhat mat khau" }
            },
            new()
            {
                CauHoi = "Quên mật khẩu phải làm sao?",
                CauTraLoi = "Ở trang Đăng nhập, bấm vào link <b>\"Quên mật khẩu?\"</b>, nhập email đã đăng ký, hệ thống sẽ gửi hướng dẫn đặt lại mật khẩu qua email.",
                TuKhoa = new() { "quen mat khau", "khong nho mat khau", "reset mat khau", "lay lai mat khau" }
            },
            new()
            {
                CauHoi = "Làm sao để thêm số điện thoại?",
                CauTraLoi = "Vào <b>Trang cá nhân</b> → bấm \"Thêm ngay\" cạnh mục Số điện thoại. Lưu ý: số điện thoại chỉ có thể thêm <b>một lần duy nhất</b> và không thể đổi sau khi xác minh.",
                TuKhoa = new() { "them so dien thoai", "cap nhat sdt", "xac minh so dien thoai" }
            },
            new()
            {
                CauHoi = "Làm sao để tạo mã QR chuyển khoản?",
                CauTraLoi = "Vào menu <b>Thanh toán QR</b> (miễn phí) → chọn ngân hàng, nhập số tài khoản, số tiền (không bắt buộc) và nội dung chuyển khoản → bấm \"Tạo mã QR\". Mã sinh ra là VietQR chuẩn, quét được bằng hầu hết app ngân hàng.",
                TuKhoa = new() { "tao ma qr", "chuyen khoan qr", "vietqr", "quet ma qr" }
            },
            new()
            {
                CauHoi = "Tích xanh cạnh tên nghĩa là gì?",
                CauTraLoi = "Tích xanh xuất hiện khi bạn đã xác minh danh tính thành công qua <b>Email hoặc Số điện thoại</b> (chỉ cần một trong hai). Vào Trang cá nhân để thực hiện xác minh.",
                TuKhoa = new() { "tich xanh", "xac minh danh tinh", "dau tick" }
            },
            new()
            {
                CauHoi = "Làm sao để quản lý tài chính gia đình?",
                CauTraLoi = "Đây là tính năng Premium. Vào menu <b>Gia đình</b> → Tạo hộ gia đình mới (bạn sẽ là chủ hộ, nhận mã mời) hoặc Tham gia bằng mã mời của người thân. Chỉ giao dịch được đánh dấu \"Chia sẻ với gia đình\" mới hiện trong báo cáo chung.",
                TuKhoa = new() { "gia dinh", "ho gia dinh", "chia se giao dich", "quan ly gia dinh" }
            },
            new()
            {
                CauHoi = "Ứng dụng có kết nối ngân hàng/MoMo/ZaloPay thật không?",
                CauTraLoi = "Mã QR chuyển khoản (VietQR) hoạt động thật 100%. Còn tính năng tự động đồng bộ sao kê từ Ngân hàng/MoMo/ZaloPay hiện đang ở chế độ khung tích hợp demo, vì kết nối thật cần hợp đồng đối tác chính thức với từng nhà cung cấp.",
                TuKhoa = new() { "ket noi ngan hang", "momo", "zalopay", "dong bo giao dich", "open banking" }
            },
            new()
            {
                CauHoi = "Làm sao để quét hóa đơn tự động (OCR)?",
                CauTraLoi = "Tính năng Premium. Vào menu <b>Quét hóa đơn</b> → tải ảnh hóa đơn lên → hệ thống tự nhận diện số tiền/ngày → bạn kiểm tra lại và bấm Lưu giao dịch.",
                TuKhoa = new() { "quet hoa don", "ocr", "chup hoa don", "nhan dien hoa don" }
            },
            new()
            {
                CauHoi = "Làm sao để đổi ảnh đại diện?",
                CauTraLoi = "Vào <b>Trang cá nhân</b> → bấm biểu tượng camera nhỏ trên ảnh đại diện → chọn ảnh (JPG/PNG/WEBP, tối đa 3MB) → Lưu ảnh.",
                TuKhoa = new() { "doi avatar", "anh dai dien", "cap nhat anh", "upload anh" }
            },
        };
    }

    public List<CauHoiMau> LayCauHoiGoiY() => _khoCauHoi;

    public string TraLoi(string cauHoiNguoiDung)
    {
        if (string.IsNullOrWhiteSpace(cauHoiNguoiDung))
        {
            return "Bạn có thể hỏi mình về cách sử dụng các tính năng trong MoneyCare, ví dụ: \"Làm sao để thêm giao dịch?\"";
        }

        var dauVao = ChuanHoa(cauHoiNguoiDung);

        // Nếu câu hỏi trùng gần như chính xác với 1 câu hỏi mẫu -> trả lời ngay
        var trungKhopChinhXac = _khoCauHoi.FirstOrDefault(c => ChuanHoa(c.CauHoi) == dauVao);
        if (trungKhopChinhXac != null) return trungKhopChinhXac.CauTraLoi;

        // So khớp theo điểm số từ khóa (đếm số từ khóa xuất hiện trong câu hỏi người dùng)
        CauHoiMau? totNhat = null;
        int diemCao = 0;

        foreach (var cauHoiMau in _khoCauHoi)
        {
            int diem = cauHoiMau.TuKhoa.Count(tk => dauVao.Contains(ChuanHoa(tk)));
            if (diem > diemCao)
            {
                diemCao = diem;
                totNhat = cauHoiMau;
            }
        }

        if (totNhat != null && diemCao > 0)
        {
            return totNhat.CauTraLoi;
        }

        return "Xin lỗi, mình chưa có câu trả lời chính xác cho câu hỏi này. Bạn thử chọn một trong các câu hỏi gợi ý bên dưới, " +
               "hoặc diễn đạt lại với các từ khóa như \"giao dịch\", \"danh mục\", \"premium\", \"hóa đơn\", \"mật khẩu\", \"gia đình\", \"QR\"...";
    }

    // Chuẩn hóa: chữ thường + bỏ dấu tiếng Việt, giúp so khớp linh hoạt hơn (có dấu hoặc không dấu đều nhận diện được)
    private static string ChuanHoa(string s)
    {
        var lower = s.ToLower(CultureInfo.InvariantCulture);
        var boDau = lower.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in boDau)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().Replace('đ', 'd').Normalize(NormalizationForm.FormC);
    }
}
