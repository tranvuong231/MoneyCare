// ============================================================================
// CẤU HÌNH ĐỊA CHỈ API BACKEND
// ============================================================================
// Đây là nơi DUY NHẤT bạn cần sửa địa chỉ IP/host của backend ASP.NET Core.
//
// - Nếu chạy Android Emulator (máy ảo Android trong Android Studio):
//     dùng 10.0.2.2 thay cho "localhost" (đây là địa chỉ đặc biệt trỏ về
//     máy tính thật của bạn khi đứng từ trong máy ảo).
//
// - Nếu chạy trên điện thoại thật (qua dây USB hoặc cùng mạng Wifi với máy
//     tính đang chạy backend): dùng địa chỉ IP LAN của máy tính, ví dụ
//     "192.168.1.5". Xem hướng dẫn lấy IP trong file HUONG_DAN_CAI_DAT.md.
//
// - Nếu dùng iOS Simulator (chạy trên máy Mac): dùng "localhost" là được.
// ============================================================================
class ApiConfig {
  // ĐỔI DÒNG NÀY theo máy của bạn (cổng mặc định của MoneyCare_API khi chạy "dotnet run" là 5200):
  static const String baseUrl = "http://10.0.2.2:5200";

  // Nếu backend chạy HTTPS (ví dụ https://localhost:5001) và bạn gặp lỗi
  // SSL chứng chỉ tự ký khi test, hãy tạm dùng cổng HTTP ở trên cho dễ,
  // hoặc cấu hình chứng chỉ hợp lệ khi triển khai thật.

  static String api(String path) => "$baseUrl/api$path";
}
