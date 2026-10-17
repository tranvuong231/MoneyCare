# 💰 MoneyCare — Ứng dụng quản lý tài chính cá nhân

Hệ thống quản lý thu chi cá nhân & hộ gia đình gồm **Web (ASP.NET Core MVC)**, **Mobile App (Flutter)** và một **REST API (ASP.NET Core Web API)** dùng chung. Có quét hóa đơn bằng OCR, chatbot/gợi ý chi tiêu bằng AI cục bộ, thanh toán Premium qua ZaloPay/VietQR và trang quản trị.

> Đồ án môn học (DACS: API + Web, DACN: Flutter App).

## ✨ Tính năng chính

- **Giao dịch & danh mục**: ghi thu/chi, phân loại, tìm kiếm, lọc.
- **Báo cáo & Dashboard**: biểu đồ theo tuần/tháng/danh mục; xuất **Excel** và **PDF**.
- **Hóa đơn & nhắc nhở**: quản lý hóa đơn định kỳ, thông báo, báo cáo định kỳ qua email (background service).
- **Quét hóa đơn (OCR)**: Tesseract (tiếng Việt + Anh), tự điền thông tin giao dịch.
- **AI**: chatbot tài chính và gợi ý chi tiêu qua LM Studio (model chạy local); tự động chuyển sang trả lời theo kịch bản khi không có AI.
- **Hộ gia đình**: chia sẻ và theo dõi chi tiêu chung.
- **Premium & thanh toán**: ZaloPay (sandbox) và mã QR VietQR.
- **Tài khoản**: đăng ký/đăng nhập JWT, xác minh OTP email/SĐT, quên mật khẩu, đăng nhập Google.
- **Liên kết ngân hàng/ví (demo)**, **trang Admin** quản lý người dùng, đơn Premium, liên hệ.

## 🏗️ Kiến trúc

```
        ┌──────────────────────┐
        │     MoneyCare_API     │  ASP.NET Core 8 Web API + EF Core + SQL Server
        │  (toàn bộ nghiệp vụ)  │  Swagger • JWT • OCR • AI • ZaloPay • Export
        └──────────▲───────────┘
          JSON+JWT │       │ JSON+JWT
   ┌───────────────┴──┐  ┌─┴────────────────┐
   │  MoneyCare_Web   │  │  MoneyCare_App   │
   │ ASP.NET Core MVC │  │     Flutter      │
   └──────────────────┘  └──────────────────┘
```

Chỉ `MoneyCare_API` làm việc với cơ sở dữ liệu; Web và App là client gọi API.

## 🧰 Công nghệ

| Phần | Công nghệ |
|---|---|
| API | C#, .NET 8, ASP.NET Core Web API, Entity Framework Core (Code First + Migrations), ASP.NET Identity, JWT, SQL Server, Swagger |
| Web | ASP.NET Core MVC (Razor), Bootstrap 5, Chart.js |
| App | Flutter / Dart, Provider, fl_chart, flutter_secure_storage, google_sign_in |
| Tích hợp | Tesseract OCR, LM Studio (LLM local), ZaloPay, VietQR, SMTP (Gmail), ClosedXML, QuestPDF |
| Kiểm thử | xUnit, Moq, EF Core InMemory (.NET); flutter_test (Dart) |

## 📁 Cấu trúc thư mục

```
MoneyCare/
├── MoneyCare_API/        # Web API + MoneyCare.Api.Tests
├── MoneyCare_Web/        # MVC web + MoneyCare.Web.Tests
├── MoneyCare_App/        # Flutter app + test/
└── docs/                 # Hướng dẫn cài đặt & kiểm thử, ảnh minh họa
```

## 🚀 Chạy thử nhanh

**Yêu cầu:** .NET SDK 8, SQL Server (Express/LocalDB), Flutter 3.x (nếu chạy app).

```bash
# 1) Tạo file cấu hình cá nhân (không bị đẩy lên Git)
cp MoneyCare_API/MoneyCare.Api/appsettings.Development.example.json MoneyCare_API/MoneyCare.Api/appsettings.Development.json
cp MoneyCare_Web/MoneyCare.Web/appsettings.Development.example.json MoneyCare_Web/MoneyCare.Web/appsettings.Development.json
#    -> mở 2 file vừa tạo, sửa connection string / khóa theo máy bạn
#       (InternalApiKey ở API và Web phải giống nhau)

# 2) (Tùy chọn) tải dữ liệu OCR ~30MB
cd MoneyCare_API/MoneyCare.Api/tessdata && ./download-tessdata.sh && cd -   # Windows: .\download-tessdata.ps1

# 3) Chạy API (tự migrate + tạo DB) -> http://localhost:5200/swagger
cd MoneyCare_API/MoneyCare.Api && dotnet run

# 4) Chạy Web (terminal khác)
cd MoneyCare_Web/MoneyCare.Web && dotnet run

# 5) Chạy App (terminal khác)
cd MoneyCare_App && flutter pub get && flutter run
```

Tài khoản admin demo (tự tạo lần chạy đầu): `admin@moneycare.local` / `Admin@123` — **hãy đổi** khi dùng thật.
Google Sign-In, gửi email thật, AI là **tùy chọn**; bỏ trống thì hệ thống tự chạy ở chế độ demo.

Hướng dẫn chi tiết: [docs/SETUP.md](docs/SETUP.md)

## 🧪 Kiểm thử

```bash
cd MoneyCare_API && dotnet test     # 63 test
cd MoneyCare_Web && dotnet test     # 21 test
cd MoneyCare_App && flutter test    # 53 test
```

Chi tiết: [API](docs/TESTING_API.md) · [Web](docs/TESTING_WEB.md) · [Flutter](docs/TESTING_FLUTTER.md)

## 🔐 Bảo mật & cấu hình

Repo **không chứa** khóa/mật khẩu thật. Mọi giá trị bí mật nằm trong `appsettings.Development.json` (đã `.gitignore`), hoặc dùng biến môi trường / `dotnet user-secrets`. Các khóa ZaloPay trong `appsettings.json` là khóa **sandbox công khai** của ZaloPay.

## 👤 Tác giả

Tran Quoc Vuong - tranquocvuong941@gmail.com
