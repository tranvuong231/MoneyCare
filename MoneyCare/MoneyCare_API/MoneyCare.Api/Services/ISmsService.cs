// Giao diện định nghĩa các chức năng gửi tin nhắn SMS.
namespace MoneyCare.Services;

public class KetQuaGuiSms
{
    public bool DaGuiThat { get; set; } // true nếu gọi API SMS thật thành công
    public string? GhiChu { get; set; }
}

public interface ISmsService
{
    Task<KetQuaGuiSms> GuiOtpAsync(string soDienThoai, string maOtp);
}
