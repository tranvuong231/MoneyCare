# MoneyCare App (Flutter)

Ứng dụng di động của hệ thống MoneyCare. Xem README ở thư mục gốc để biết kiến trúc tổng thể.

```bash
flutter pub get
# Chỉnh địa chỉ API trong lib/config/api_config.dart (Android emulator: http://10.0.2.2:5200)
flutter run --dart-define=GOOGLE_WEB_CLIENT_ID=<web-client-id>   # dart-define là tùy chọn
flutter test
```

Android Google Sign-In cần `android/app/google-services.json` của riêng bạn (xem `google-services.json.example`); thiếu file này app vẫn build được.
