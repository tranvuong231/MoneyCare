// Mô phỏng chức năng gửi tin nhắn SMS.
namespace MoneyCare.Services;

/// <summary>
/// LƯU Ý QUAN TRỌNG (tương tự phần kết nối Ngân hàng/MoMo/ZaloPay):
/// Gửi SMS thật ra đầu số điện thoại di động yêu cầu đăng ký tài khoản trả phí với một nhà cung cấp
/// dịch vụ SMS Brandname/OTP (ở Việt Nam phổ biến: eSMS.vn, SpeedSMS, FPT SMS, Viettel SMS, hoặc
/// quốc tế như Twilio/Vonage). Không có cách nào gửi SMS thật miễn phí mà không qua các dịch vụ này.
///
/// Class này là bản DEMO: thay vì gửi SMS thật, nó chỉ trả về kết quả "chưa gửi thật" để Controller
/// tự hiển thị mã OTP trực tiếp trên màn hình cho người dùng (giả lập việc "nhận được SMS").
///
/// Khi có tài khoản nhà cung cấp SMS thật, chỉ cần tạo class mới implement ISmsService, ví dụ:
///
///   public class ESmsService : ISmsService
///   {
///       public async Task&lt;KetQuaGuiSms&gt; GuiOtpAsync(string soDienThoai, string maOtp)
///       {
///           using var http = new HttpClient();
///           var payload = new {
///               ApiKey = _config["ESms:ApiKey"],
///               SecretKey = _config["ESms:SecretKey"],
///               Phone = soDienThoai,
///               Content = $"Ma xac minh MoneyCare cua ban la: {maOtp}",
///               Brandname = "MoneyCare",
///               SmsType = "2"
///           };
///           var res = await http.PostAsJsonAsync("http://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_get", payload);
///           // ... kiểm tra res, trả về KetQuaGuiSms { DaGuiThat = true }
///       }
///   }
///
/// rồi đổi dòng đăng ký DI trong Program.cs từ DemoSmsService sang ESmsService.
/// </summary>
public class DemoSmsService : ISmsService
{
    private readonly ILogger<DemoSmsService> _logger;

    public DemoSmsService(ILogger<DemoSmsService> logger)
    {
        _logger = logger;
    }

    public Task<KetQuaGuiSms> GuiOtpAsync(string soDienThoai, string maOtp)
    {
        _logger.LogInformation("[DEMO SMS] Gửi OTP {Otp} tới số {SoDienThoai} (chưa kết nối nhà cung cấp SMS thật)", maOtp, soDienThoai);
        return Task.FromResult(new KetQuaGuiSms
        {
            DaGuiThat = false,
            GhiChu = "Chế độ demo: chưa kết nối nhà cung cấp SMS thật, mã OTP được hiển thị trực tiếp trên màn hình."
        });
    }
}
