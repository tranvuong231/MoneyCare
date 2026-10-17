# HƯỚNG DẪN CÀI ĐẶT & CHẠY MONEYCARE (API + Web + App)

Tài liệu này dành cho người **mới bắt đầu** - làm theo đúng thứ tự từ trên xuống là chạy được toàn bộ hệ thống.

---

## 1. Tổng quan kiến trúc mới

```
                    ┌───────────────────────┐
                    │     MoneyCare_API      │   ← Chỉ project này chạm vào Database
                    │  (ASP.NET Core Web API)│   ← Toàn bộ nghiệp vụ (OCR, AI, ZaloPay,
                    │      cổng 5200/5201    │      Premium, Admin...) nằm ở đây
                    └───────────▲────────────┘
                     JSON + JWT │  JSON + JWT
                    ┌───────────┴────────┐   ┌──────────────────┴──┐
                    │   MoneyCare_Web     │   │    MoneyCare_App     │
                    │ (ASP.NET Core MVC)  │   │      (Flutter)        │
                    │   Chỉ hiển thị UI    │   │   Chỉ hiển thị UI      │
                    │  + gọi API bằng      │   │  + gọi API bằng        │
                    │    HttpClient        │   │    http package        │
                    └──────────────────────┘   └────────────────────────┘
```

**Vì sao tách như vậy:**
- `MoneyCare_API` là "bộ não" duy nhất mọi validate, tính toán, đọc/ghi SQL Server đều nằm ở đây.
- `MoneyCare_Web` và `MoneyCare_App` là **Frontend** — chúng vẫn có logic riêng (validate form, điều
  hướng trang, quản lý trạng thái đăng nhập, gọi API...) nhưng **không** truy cập Database trực tiếp.
- Bạn có thể sửa giao diện Web/App thoải mái mà không đụng vào nghiệp vụ, và ngược lại.

---

## 2. Cài đặt công cụ cần thiết (làm 1 lần)

| Công cụ | Phiên bản | Tải tại |
|---|---|---|
| **.NET SDK** | 8.0 | https://dotnet.microsoft.com/download/dotnet/8.0 |
| **SQL Server** | Express / Developer / LocalDB đều được | https://www.microsoft.com/sql-server/sql-server-downloads |
| **SQL Server Management Studio (SSMS)** | mới nhất (tùy chọn, để xem DB trực quan) | https://aka.ms/ssmsfullsetup |
| **Flutter SDK** | 3.x | https://docs.flutter.dev/get-started/install |
| **Visual Studio 2022** (khuyên dùng cho 2 project .NET) hoặc VS Code | mới nhất | — |
| **Android Studio** (để chạy Flutter emulator) | mới nhất | https://developer.android.com/studio |

Kiểm tra đã cài đúng chưa (mở Terminal/CMD):
```bash
dotnet --version     # phải hiện 8.0.x
flutter doctor        # kiểm tra Flutter, sửa theo các dấu ✗ nếu có
```

---

## 3. MoneyCare_API làm trước tiên (2 project kia phụ thuộc vào nó)

### 3.0. Cách quản lý cấu hình bí mật (đọc trước khi chạy)
Repo này **không chứa** mật khẩu/khóa thật. Mỗi project có:
- `appsettings.json` cấu hình mặc định, giá trị bí mật để trống hoặc dạng `CHANGE_ME_...` (được commit).
- `appsettings.Development.example.json` file mẫu (được commit).
- `appsettings.Development.json` **file của riêng bạn**, tạo bằng cách copy từ file `.example` rồi điền giá trị thật. File này đã bị `.gitignore`, sẽ **không** bị đẩy lên GitHub.

```bash
cp MoneyCare_API/MoneyCare.Api/appsettings.Development.example.json MoneyCare_API/MoneyCare.Api/appsettings.Development.json
cp MoneyCare_Web/MoneyCare.Web/appsettings.Development.example.json MoneyCare_Web/MoneyCare.Web/appsettings.Development.json
```
(Windows CMD dùng `copy` thay cho `cp`.) Giá trị `InternalApiKey` ở API và Web phải **giống hệt nhau**.

### 3.1. Mở project
Mở `MoneyCare_API/MoneyCare.Api.sln` bằng Visual Studio, hoặc `cd MoneyCare_API/MoneyCare.Api` rồi
dùng dòng lệnh.

### 3.2. Đổi Connection String sang Database mới "MoneyCare"
Mở (hoặc ghi đè trong `appsettings.Development.json`) `MoneyCare_API/MoneyCare.Api/appsettings.json`, sửa dòng `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=MoneyCare;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

- Nếu bạn chỉ có **LocalDB** (đi kèm Visual Studio), đổi thành:
  `Server=(localdb)\\MSSQLLocalDB;Database=MoneyCare;Trusted_Connection=True;TrustServerCertificate=True;`
- Tên instance của bạn có thể khác (vd `ADMIN\\SQLEXPRESS`, `.`): mở SSMS, xem "Server name" lúc đăng nhập rồi điền vào.
- Nếu bạn dùng **SQL Server Authentication** (user/password thay vì Windows):
  `Server=localhost;Database=MoneyCare;User Id=sa;Password=MatKhauCuaBan;TrustServerCertificate=True;`

Bạn **không cần** tự tạo Database "MoneyCare" trước bước tiếp theo sẽ tự tạo.

### 3.3. Chạy Migration để tạo bảng
Migration đã có sẵn trong project (thư mục `Migrations/`), bạn chỉ cần áp dụng chúng:

```bash
cd MoneyCare_API/MoneyCare.Api
dotnet restore
dotnet ef database update
```

> Nếu báo lỗi `dotnet ef: command not found`, cài công cụ EF Core CLI 1 lần:
> `dotnet tool install --global dotnet-ef`

**Cách khác (không cần dotnet ef):** chỉ cần chạy `dotnet run` - `Program.cs` đã được cấu hình
**tự động migrate + tạo bảng khi khởi động** (`db.Database.Migrate()`), nên thật ra bước
`dotnet ef database update` là tùy chọn, bạn có thể bỏ qua và chạy thẳng bước 3.4.

### 3.4. Chạy thử
```bash
dotnet run
```
Mặc định API chạy ở `http://localhost:5200` (xem `Properties/launchSettings.json` nếu muốn đổi cổng).
Mở trình duyệt vào `http://localhost:5200/swagger` để xem toàn bộ danh sách API và test thử trực tiếp.

Lúc khởi động lần đầu, hệ thống sẽ tự động:
- Tạo Database `MoneyCare` + toàn bộ bảng (Users, Transactions, Categories, Bills, Households...)
- Tạo Role "Admin"
- Tạo sẵn 1 tài khoản quản trị demo: **email `admin@moneycare.local`, mật khẩu `Admin@123`**
  (đổi 2 giá trị này trong `appsettings.json` mục `AdminSeed` trước khi chạy nếu muốn dùng email/mật
  khẩu khác hoặc đổi mật khẩu sau khi đăng nhập).

### 3.5. Các cấu hình tùy chọn (không bắt buộc để chạy demo, nhưng nên biết)

| Mục trong `appsettings.json` | Dùng để làm gì | Nếu để trống thì sao |
|---|---|---|
| `EmailSettings` (SMTP) | Gửi email thật (mã OTP xác minh, đặt lại mật khẩu...) | Hệ thống tự chuyển sang **chế độ demo**: trả thẳng mã OTP về cho Web/App hiển thị luôn trên màn hình, không cần đọc email |
| `Authentication:Google:ClientId` (+ `ClientSecret` bên `MoneyCare_Web`) | Đăng nhập bằng Google | Nút "Đăng nhập với Google" sẽ báo lỗi nếu bấm vào ẩn bớt phần này nếu không dùng |
| `LmStudio` | Chatbot & gợi ý chi tiêu bằng AI cục bộ (chạy [LM Studio](https://lmstudio.ai/) trên máy bạn) | Chatbot tự chuyển sang trả lời theo kịch bản có sẵn (rule-based), Insights vẫn hoạt động bình thường chỉ là không có thêm gợi ý AI |
| `ZaloPay` | Thanh toán QR ZaloPay (đã để sẵn key môi trường **sandbox công khai** của ZaloPay để test) | Vẫn chạy được ở chế độ sandbox, không cần đổi gì để demo |
| `Jwt:Key` | Khóa ký JWT | **Bắt buộc đổi** thành chuỗi ngẫu nhiên riêng của bạn (đặt trong `appsettings.Development.json`) trước khi triển khai thật |

---

## 4. MoneyCare_Web - chạy sau khi API đã chạy

### 4.1. Trỏ đúng địa chỉ API
Mở `MoneyCare_Web/MoneyCare.Web/appsettings.json`:
```json
"ApiBaseUrl": "http://localhost:5200"
```
Dùng đúng cổng **HTTP** (không phải HTTPS) mà `MoneyCare_API` đang chạy ở bước 3.4 — mặc định là
`http://localhost:5200`. API cố tình không ép chuyển hướng sang HTTPS (để tránh lỗi bắt tay SSL khi
Web gọi sang), nên cứ dùng `http://` cho việc chạy ở máy cá nhân là ổn nhất, đỡ phải lo chứng chỉ tự ký.

### 4.2. (Tùy chọn) Bật đăng nhập Google
Nếu muốn dùng, điền cả 2 nơi:
- `MoneyCare_API/appsettings.json` → `Authentication:Google:ClientId`
- `MoneyCare_Web/appsettings.json` → `Authentication:Google:ClientId` **và** `ClientSecret`

(Lấy 2 giá trị này tại https://console.cloud.google.com/apis/credentials, tạo OAuth Client ID loại
"Web application", Authorized redirect URI đặt là `https://localhost:5201/signin-google` theo đúng
cổng Web của bạn.)

### 4.3. Chạy
```bash
cd MoneyCare_Web/MoneyCare.Web
dotnet restore
dotnet run
```
Mở trình duyệt vào địa chỉ hiện trong Terminal (mặc định `https://localhost:7xxx` hoặc theo
`Properties/launchSettings.json`). Đăng ký tài khoản mới hoặc đăng nhập bằng tài khoản Admin ở bước
3.4 để vào trang quản trị tại đường dẫn `/Admin`.

---

## 5. MoneyCare_App (Flutter) — chạy song song hoặc sau cùng

### 5.1. Trỏ đúng địa chỉ API
Mở `MoneyCare_App/lib/config/api_config.dart`:

```dart
static const String baseUrl = "http://10.0.2.2:5200";
```

**Google Sign-In (tùy chọn)**: truyền Web Client ID khi chạy, không hard-code vào mã nguồn:
```bash
flutter run --dart-define=GOOGLE_WEB_CLIENT_ID=xxxx.apps.googleusercontent.com
```
Trên Android cần thêm file `google-services.json` của riêng bạn (từ Firebase Console) vào `MoneyCare_App/android/app/` - xem `google-services.json.example`. Không có file này app vẫn build và chạy bình thường, chỉ thiếu Google Sign-In.

- **Android Emulator**: giữ nguyên `10.0.2.2` (địa chỉ đặc biệt trỏ về máy tính thật của bạn).
- **Điện thoại thật** (USB hoặc cùng Wifi với máy tính chạy API): đổi thành IP LAN máy tính, ví dụ
  `http://192.168.1.5:5200`. Lấy IP bằng lệnh `ipconfig` (Windows) → tìm dòng "IPv4 Address".
- **iOS Simulator**: dùng `http://localhost:5200`.
- Nhớ đổi đúng **số cổng** khớp với cổng API đang chạy thật (bước 3.4).

> Ghi chú: file `AndroidManifest.xml` đã có sẵn `usesCleartextTraffic="true"` để cho phép gọi API qua
> `http://` (không mã hóa) khi test — khi triển khai thật, hãy chạy API qua HTTPS và bỏ cấu hình này.

### 5.2. Cài package & chạy
```bash
cd MoneyCare_App
flutter pub get
flutter run
```
Chọn thiết bị/emulator khi được hỏi. Đăng ký tài khoản mới ngay trên app hoặc đăng nhập bằng tài
khoản đã tạo từ Web - **vì cả 2 dùng chung 1 Database/API**, tài khoản và dữ liệu đồng bộ ngay lập tức
giữa Web và App.

---

## 6. Kiểm tra hệ thống hoạt động đúng (checklist nhanh)

1. ✅ `dotnet run` ở `MoneyCare_API` không báo lỗi, `swagger` mở được.
2. ✅ Đăng ký 1 tài khoản mới trên `MoneyCare_Web` → đăng nhập được → vào Dashboard thấy biểu đồ (rỗng vì chưa có giao dịch).
3. ✅ Thêm 1 giao dịch Thu/Chi trên Web → mở `MoneyCare_App`, đăng nhập **cùng tài khoản đó** → giao dịch hiện ra trên App (chứng minh 2 app đang dùng chung 1 API/Database).
4. ✅ Đăng nhập bằng tài khoản Admin (`admin@moneycare.local` / `Admin@123`) trên Web → vào `/Admin` → thấy danh sách người dùng.
5. ✅ Mở SSMS, kết nối vào SQL Server, thấy Database `MoneyCare` với đầy đủ bảng `AspNetUsers`, `Transactions`, `Categories`, `Bills`, `Households`, `PremiumOrders`, `Notifications`...

---

## 7. Những thay đổi so với bản gốc (nên biết để không bỡ ngỡ)

Trong lúc tách Backend, một vài chỗ được **đơn giản hóa/thống nhất lại** giữa Web và App cho gọn và dễ bảo trì hơn hành vi cuối cùng người dùng thấy gần như không đổi:

- **Xác minh Email**: trước đây Web gửi **link** để bấm, giờ gửi **mã OTP 6 số** để nhập tay (giống hệt cách xác minh Số điện thoại đã có) đồng bộ cách làm với App di động và không cần lo việc mở link phải đúng domain.
- **Quên mật khẩu**: tương tự, chuyển từ link sang mã OTP 6 số.
- **Liên kết Ngân hàng/MoMo (demo)**: trước đây bấm "Kết nối" sẽ thử chuyển hướng OAuth trước rồi mới rơi về form nhập tay; giờ vào thẳng form nhập tay luôn (vì đây vốn là tính năng minh họa, chưa có hợp đồng đối tác thật).
- **Đăng nhập Google trên Web**: vẫn giữ nguyên trải nghiệm bấm nút → chuyển sang Google → quay lại là đăng nhập xong, chỉ khác là ở "hậu trường" Web gửi token Google nhận được cho `MoneyCare_API` xác thực (dùng chung 1 endpoint với App di động) thay vì tự xử lý.
- **Đổi tên đăng nhập lúc đăng ký**: bản mới dùng **Email làm định danh duy nhất** (giống ứng dụng di động) trường "Tên đăng nhập" trên form Đăng nhập dùng để nhập Email.

Không có tính năng nào bị cắt bỏ toàn bộ nghiệp vụ gốc (OCR hóa đơn, Insight AI, Chatbot, ZaloPay, Premium, Hộ gia đình, Xuất Excel/PDF, trang Admin...) đều được giữ nguyên và chuyển đầy đủ sang `MoneyCare_API`.

---

## 8. Dọn dẹp đã thực hiện

- Gỡ toàn bộ code truy cập Database (`ApplicationDbContext`, `UserManager`, `SignInManager`) ra khỏi `MoneyCare_Web` - Web giờ chỉ còn 1 cửa ngõ duy nhất ra bên ngoài là `Services/MoneyCareApiClient.cs`.
- Gộp các Controller MVC (vốn vừa xử lý nghiệp vụ vừa render View) thành 2 lớp rõ ràng: API Controller (trả JSON, có validate + business rule) và Web Controller (chỉ gọi API + đổ dữ liệu ra View).
- Xóa thư mục `wwwroot/uploads` cũ trong Web (ảnh đại diện/ảnh nền giờ được `MoneyCare_API` lưu trữ và phục vụ, Web không tự lưu file nữa).

---

## 9. Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| `Cannot open database "MoneyCare"` | Chưa migrate, hoặc sai Connection String | Kiểm tra lại mục 3.2, đảm bảo SQL Server đang chạy (`services.msc` → tìm "SQL Server") |
| Web/App gọi API báo lỗi kết nối (Connection refused) | `MoneyCare_API` chưa chạy, hoặc sai `ApiBaseUrl`/`baseUrl` | Chạy `MoneyCare_API` trước, kiểm tra đúng cổng ở mục 4.1 / 5.1 |
| App Flutter không gọi được API dù Web gọi được | Dùng `localhost` trong `api_config.dart` khi chạy Android Emulator | Đổi thành `10.0.2.2` (xem mục 5.1) |
| `dotnet ef: command not found` | Chưa cài công cụ CLI | `dotnet tool install --global dotnet-ef` |
| Đăng nhập Google báo lỗi "Unknown scheme" | Chưa cấu hình `Authentication:Google` ở `MoneyCare_Web` | Xem mục 4.2, hoặc tạm thời không dùng nút này |
| Quét hóa đơn OCR báo lỗi thiếu tessdata | Thư mục `tessdata` không được copy ra thư mục chạy | Chạy `MoneyCare_API/MoneyCare.Api/tessdata/download-tessdata.ps1` (Windows) hoặc `.sh` (macOS/Linux) để tải 2 file `.traineddata`, rồi build lại (`dotnet build`) |
| Chatbot/Insight không có gợi ý AI, chỉ trả lời mẫu có sẵn | Chưa chạy LM Studio ở `localhost:1234` | Cài & chạy [LM Studio](https://lmstudio.ai/) với 1 model bất kỳ, hoặc bỏ qua — tính năng cốt lõi vẫn hoạt động bình thường |

---

## 10. Thứ tự chạy khi làm việc hàng ngày

1. Bật SQL Server (thường tự chạy nền sẵn nếu cài local).
2. `cd MoneyCare_API/MoneyCare.Api && dotnet run`
3. `cd MoneyCare_Web/MoneyCare.Web && dotnet run` (terminal khác)
4. `cd MoneyCare_App && flutter run` (terminal khác, nếu cần test app)

Chúc bạn triển khai thành công! Nếu build báo lỗi cụ thể ở bước nào, gửi lại nguyên văn lỗi để được hỗ trợ tiếp.
