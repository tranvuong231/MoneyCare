// Gọi AI cục bộ (LOCAL) chạy qua LM Studio (https://lmstudio.ai) thay cho Gemini API.
// LM Studio mở 1 server tương thích chuẩn OpenAI ngay trên máy (mặc định
// http://localhost:1234) - không cần API Key, không tốn phí, không gửi dữ liệu người
// dùng ra ngoài Internet. Chỉ cần mở app LM Studio, tải model "qwen2.5-3b-instruct",
// bấm "Start Server" ở tab Developer là dùng được ngay (xem HUONG_DAN_CAI_DAT.docx).
//
// Nếu LM Studio chưa mở / chưa load model / gọi lỗi -> trả về null để nơi gọi
// (ChatbotController/InsightsController/OcrApiController...) tự động dùng lại logic
// mẫu có sẵn (fallback), giống hệt cách GeminiService cũ hoạt động trước đây.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MoneyCare.Services;

/// <summary>Kết quả AI phân tích 1 hóa đơn từ văn bản OCR thô, dùng để tự động điền form.</summary>
public class KetQuaPhanTichHoaDonAi
{
    public decimal? SoTien { get; set; }
    public DateTime? Ngay { get; set; }
    public string? TenCuaHang { get; set; }

    /// <summary>Tên danh mục AI gợi ý (phải khớp với 1 trong danh sách danh mục đã gửi lên, hoặc null).</summary>
    public string? DanhMucGoiY { get; set; }
    public string? GhiChuGoiY { get; set; }

    /// <summary>"Thu" hoặc "Chi" - AI tự nhận định đây là khoản thu nhập hay chi tiêu.</summary>
    public string? Loai { get; set; }
}

public interface ILlmService
{
    /// <summary>Hỏi - đáp dạng chat tự do (dùng cho Chatbot hỗ trợ và gợi ý phân tích Insights).</summary>
    Task<string?> HoiAsync(string heThong, string cauHoi);

    /// <summary>
    /// Phân tích văn bản OCR thô của 1 hóa đơn thành dữ liệu có cấu trúc, đồng thời tự nhận định
    /// đây là khoản Thu hay Chi để gợi ý đúng danh mục (danh mục Thu và Chi được gửi tách riêng).
    /// </summary>
    Task<KetQuaPhanTichHoaDonAi?> PhanTichHoaDonAsync(string vanBanOcr, IEnumerable<string> tenDanhMucThu, IEnumerable<string> tenDanhMucChi);
}

public class LmStudioService : ILlmService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<LmStudioService> _logger;

    public LmStudioService(HttpClient http, IConfiguration config, ILogger<LmStudioService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private string BaseUrl => (_config["LmStudio:BaseUrl"] ?? "http://localhost:1234").TrimEnd('/');
    private string Model => _config["LmStudio:Model"] ?? "qwen2.5-3b-instruct";

    public async Task<string?> HoiAsync(string heThong, string cauHoi)
    {
        var noiDung = await GoiChatAsync(
            messages: new object[]
            {
                new { role = "system", content = heThong },
                new { role = "user", content = cauHoi }
            },
            temperature: 0.5,
            maxTokens: 800,
            timeoutGiay: 45);

        return string.IsNullOrWhiteSpace(noiDung) ? null : noiDung.Trim();
    }

    public async Task<KetQuaPhanTichHoaDonAi?> PhanTichHoaDonAsync(string vanBanOcr, IEnumerable<string> tenDanhMucThu, IEnumerable<string> tenDanhMucChi)
    {
        if (string.IsNullOrWhiteSpace(vanBanOcr)) return null;

        var danhSachThu = string.Join(", ", tenDanhMucThu);
        var danhSachChi = string.Join(", ", tenDanhMucChi);
        var heThong =
            "Bạn là hệ thống trích xuất dữ liệu hóa đơn cho app quản lý chi tiêu MoneyCare. " +
            "Người dùng sẽ gửi văn bản thô nhận diện được từ ảnh hóa đơn/biên nhận (OCR, có thể có lỗi chính tả/thiếu dấu, " +
            "thứ tự dòng có thể xáo trộn). " +
            "Nhiệm vụ: đọc kỹ TOÀN BỘ văn bản (đặc biệt chú ý vài dòng ĐẦU TIÊN - hầu hết hóa đơn Việt Nam luôn in " +
            "tên đơn vị/công ty/cửa hàng phát hành ngay ở phần đầu, thường có chữ \"CÔNG TY\", \"CTY\", \"CỬA HÀNG\", " +
            "\"SIÊU THỊ\", \"NHÀ HÀNG\", \"QUÁN\", hoặc là tên riêng viết hoa) rồi trả lời DUY NHẤT một object JSON " +
            "hợp lệ (không thêm chữ nào khác, không dùng markdown, không giải thích), đúng định dạng sau:\n" +
            "{\"so_tien\": <số nguyên VNĐ đã quy đổi, KHÔNG có dấu chấm/phẩy phân cách, hoặc null nếu không rõ>, " +
            "\"ngay\": <ngày giao dịch dạng \"yyyy-MM-dd\", hoặc null nếu không rõ>, " +
            "\"ten_cua_hang\": <BẮT BUỘC cố gắng trích xuất - tên đơn vị/công ty/cửa hàng/nhà cung cấp phát hành hóa đơn " +
            "này (KHÔNG PHẢI tên khách hàng), lấy nguyên văn gần đúng nhất có thể từ văn bản gốc (được phép sửa lỗi chính tả " +
            "OCR rõ ràng); CHỈ trả về null nếu thực sự không có bất kỳ tên đơn vị/cửa hàng nào xuất hiện trong văn bản>, " +
            "\"loai\": <\"Chi\" nếu đây là hóa đơn/biên nhận THANH TOÁN - CHI TIÊU (mua hàng, hóa đơn dịch vụ, ăn uống...), " +
            "hoặc \"Thu\" nếu đây là chứng từ THU NHẬP (phiếu lương, biên nhận NHẬN tiền, hóa đơn bán hàng do người dùng xuất...). " +
            "Mặc định chọn \"Chi\" nếu không chắc chắn - phần lớn hóa đơn/biên nhận quét được là chi tiêu>, " +
            "\"danh_muc_goi_y\": <CHỌN đúng 1 tên trong danh sách TƯƠNG ỨNG với \"loai\" đã chọn ở trên (giữ nguyên chính tả), " +
            $"hoặc null nếu không có danh mục nào phù hợp. Danh sách danh mục Thu: [{danhSachThu}]. Danh sách danh mục Chi: [{danhSachChi}]>, " +
            "\"ghi_chu_goi_y\": <1 câu ngắn gọn (dưới 15 từ) tóm tắt giao dịch càng cụ thể càng tốt - ƯU TIÊN nêu rõ " +
            "loại dịch vụ/hàng hóa (vd \"Tiền nước\", \"Tiền điện\", \"Ăn trưa\"), kỳ/tháng thanh toán nếu có, " +
            "và chỉ số/số lượng tiêu thụ nếu hóa đơn có ghi (vd \"64m3\", \"150kWh\"); có thể thêm tên cửa hàng/khách " +
            "hàng nếu ngắn gọn. Ví dụ: \"Tiền nước kỳ 11/2018 - 64m3\" hoặc \"Ăn trưa tại quán ABC\". Trả về null nếu " +
            "không đủ thông tin để tóm tắt>}\n" +
            "Lưu ý: so_tien PHẢI là số tiền TỔNG CỘNG/THÀNH TIỀN cuối cùng, không phải giá 1 món. " +
            "Kiểm tra lại lần nữa trước khi trả lời: đã cố gắng hết sức điền ten_cua_hang và loai đúng nhất có thể chưa " +
            "(đây là 2 trường người dùng thường bị bỏ trống nhất nếu AI trả lời hời hợt).";

        var noiDung = await GoiChatAsync(
            messages: new object[]
            {
                new { role = "system", content = heThong },
                new { role = "user", content = vanBanOcr.Length > 4000 ? vanBanOcr[..4000] : vanBanOcr }
            },
            temperature: 0.1,
            maxTokens: 400,
            // Model chạy CPU + chế độ ép JSON (grammar-constrained decoding) có thể khá chậm (thường
            // 20-90 giây tùy máy) -> cho thời gian chờ rộng rãi thay vì báo lỗi/rơi về quy tắc quá sớm.
            timeoutGiay: 100,
            // TẮT response_format=json_object: chế độ này bắt LM Studio sinh từng token theo văn phạm
            // (grammar) JSON, chậm hơn ĐÁNG KỂ khi chạy CPU (không GPU). Đã có prompt yêu cầu rõ + hàm
            // PhanTichJson() bên dưới tự trích JSON bằng regex nên không cần ép cứng vẫn ra kết quả đúng.
            jsonMode: false);

        if (string.IsNullOrWhiteSpace(noiDung)) return null;

        return PhanTichJson(noiDung);
    }

    private async Task<string?> GoiChatAsync(object[] messages, double temperature, int maxTokens, int timeoutGiay, bool jsonMode = false)
    {
        try
        {
            object body = jsonMode
                ? new
                {
                    model = Model,
                    messages,
                    temperature,
                    max_tokens = maxTokens,
                    response_format = new { type = "json_object" }
                }
                : new
                {
                    model = Model,
                    messages,
                    temperature,
                    max_tokens = maxTokens
                };

            var json = JsonSerializer.Serialize(body);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // LM Studio chạy local thường KHÔNG cần API Key. Nếu người dùng có đặt
            // "LmStudio:ApiKey" trong appsettings.json (ví dụ khi expose LM Studio qua mạng
            // có xác thực), vẫn gửi kèm cho chắc - không ảnh hưởng gì khi để trống.
            var apiKey = _config["LmStudio:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutGiay));
            var res = await _http.SendAsync(req, cts.Token);

            if (!res.IsSuccessStatusCode)
            {
                var noiDungLoi = await res.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("LM Studio trả lỗi {StatusCode}: {NoiDung}", res.StatusCode, noiDungLoi);
                return null;
            }

            var resJson = await res.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(resJson);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return text;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "Gọi LM Studio quá thời gian chờ ({TimeoutGiay}s) - model có thể đang xử lý chậm (máy không có " +
                "GPU/model lớn) hoặc LM Studio Server chưa bật. Sẽ dùng kết quả dự phòng.", timeoutGiay);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Không kết nối được LM Studio tại {BaseUrl}. Hãy mở app LM Studio -> tab Developer -> Start Server.",
                BaseUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi không xác định khi gọi LM Studio - sẽ dùng logic mẫu thay thế");
            return null;
        }
    }

    private static KetQuaPhanTichHoaDonAi? PhanTichJson(string noiDungTraVe)
    {
        try
        {
            // Model đôi khi vẫn bọc JSON trong ```json ... ``` dù đã dặn không làm vậy -> dọn sạch trước khi parse
            var sach = noiDungTraVe.Trim();
            var match = Regex.Match(sach, @"\{[\s\S]*\}");
            if (!match.Success) return null;

            using var doc = JsonDocument.Parse(match.Value);
            var root = doc.RootElement;

            decimal? soTien = null;
            if (root.TryGetProperty("so_tien", out var elSoTien) && elSoTien.ValueKind != JsonValueKind.Null)
            {
                if (elSoTien.ValueKind == JsonValueKind.Number && elSoTien.TryGetDecimal(out var soTienSo))
                    soTien = soTienSo;
                else if (elSoTien.ValueKind == JsonValueKind.String &&
                         decimal.TryParse(Regex.Replace(elSoTien.GetString() ?? "", @"[^\d]", ""), out var soTienChuoi))
                    soTien = soTienChuoi;
            }

            DateTime? ngay = null;
            if (root.TryGetProperty("ngay", out var elNgay) && elNgay.ValueKind == JsonValueKind.String)
            {
                var ngayChuoi = elNgay.GetString();
                // LUÔN phân tích theo định dạng "yyyy-MM-dd" bất biến (InvariantCulture) - KHÔNG dùng
                // CurrentCulture (vd server chạy culture vi-VN mặc định dd/MM/yyyy) vì nếu AI trả về
                // sai định dạng dù chỉ 1 lần, việc parse theo culture hiện tại của server rất dễ đọc
                // nhầm ngày/tháng cho nhau (28/10 hiểu nhầm thành 10/28 hoặc ngược lại).
                if (DateTime.TryParseExact(ngayChuoi, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngayChinhXac))
                    ngay = ngayChinhXac;
                else if (DateTime.TryParse(ngayChuoi, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngayParse))
                    ngay = ngayParse;
            }

            string? Chuoi(string ten) =>
                root.TryGetProperty(ten, out var el) && el.ValueKind == JsonValueKind.String
                    ? el.GetString()
                    : null;

            return new KetQuaPhanTichHoaDonAi
            {
                SoTien = soTien is > 0 and < 1_000_000_000 ? soTien : null,
                Ngay = ngay,
                TenCuaHang = Chuoi("ten_cua_hang"),
                DanhMucGoiY = Chuoi("danh_muc_goi_y"),
                GhiChuGoiY = Chuoi("ghi_chu_goi_y"),
                Loai = Chuoi("loai")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
