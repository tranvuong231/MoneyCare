// Giao diện định nghĩa các chức năng của chatbot.
namespace MoneyCare.Services;

public class CauHoiMau
{
    public string CauHoi { get; set; } = string.Empty;
    public string CauTraLoi { get; set; } = string.Empty;
    public List<string> TuKhoa { get; set; } = new();
}

public interface IChatbotService
{
    /// <summary>Danh sách câu hỏi mẫu để hiển thị dạng gợi ý (chip) trên giao diện chat.</summary>
    List<CauHoiMau> LayCauHoiGoiY();

    /// <summary>Trả lời một câu hỏi tự do của người dùng dựa trên việc so khớp từ khóa với kho câu hỏi mẫu.</summary>
    string TraLoi(string cauHoiNguoiDung);
}
