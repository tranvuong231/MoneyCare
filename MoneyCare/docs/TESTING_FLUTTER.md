# HƯỚNG DẪN CHẠY KIỂM THỬ TỰ ĐỘNG — MoneyCare_App (DACN - Flutter)

> Hướng dẫn này viết cho người **mới bắt đầu với kiểm thử tự động (automated testing)** trên Flutter, đi từng bước một. Đọc từ trên xuống, làm theo đúng thứ tự.

---

## 1. Kiểm thử tự động trong Flutter là gì?

Flutter có sẵn 3 loại test, từ nhỏ đến lớn:

| Loại | Test cái gì | Có cần màn hình/emulator không |
|---|---|---|
| **Unit test** | 1 hàm/1 class thuần Dart (ví dụ: parse JSON, tính toán) | ❌ Không cần |
| **Widget test** | 1 giao diện (widget) hiển thị đúng, bấm nút có phản ứng đúng | ❌ Không cần (giả lập màn hình ảo trong bộ nhớ) |
| **Integration test** | Chạy cả app thật trên điện thoại/emulator, giả lập thao tác người dùng | ✅ Cần thiết bị/emulator |

Bộ 10 file test được viết cho project này thuộc loại **unit test**, tập trung kiểm tra các **Model** trong `lib/models/models.dart` — đây là lớp chuyển đổi dữ liệu JSON (mà server trả về) thành object Dart để hiển thị lên màn hình, và ngược lại (từ object Dart thành JSON gửi lên server). Vì sao chọn phần này để test:

- Đây là phần **thuần Dart, không đụng tới máy ảnh, GPS, lưu trữ bảo mật (flutter_secure_storage), hay mạng Internet thật** → chạy test cực nhanh (dưới 1 giây cho cả bộ), không cần cài emulator/điện thoại, không cần backend đang chạy.
- Đây cũng là phần **dễ sai và khó phát hiện bằng mắt nhất**: nếu tên trường JSON gõ sai 1 chữ (ví dụ server trả về `"soTien"` nhưng code Flutter đọc `"soTien "` dư 1 dấu cách, hoặc gõ nhầm `"SoTien"` viết hoa), app sẽ không báo lỗi rõ ràng mà chỉ hiển thị `0` hoặc trống — rất khó nhận ra khi test tay.

---

## 2. Chuẩn bị máy tính

### Bước 1: Cài Flutter SDK

1. Vào trang: https://docs.flutter.dev/get-started/install
2. Chọn đúng hệ điều hành của bạn, làm theo hướng dẫn cài đặt (tải file zip, giải nén, thêm vào biến môi trường PATH).
3. Mở **Terminal**/**Command Prompt**, gõ:
   ```
   flutter --version
   ```
   Nếu hiện ra số phiên bản (ví dụ `Flutter 3.x.x`) là cài xong phần cơ bản.
4. Chạy tiếp lệnh kiểm tra sức khỏe cài đặt:
   ```
   flutter doctor
   ```
   Lệnh này liệt kê những gì còn thiếu (Android Studio, Xcode...). **Với việc chạy `flutter test` cho các file trong hướng dẫn này, bạn KHÔNG bắt buộc phải cài Android Studio/Xcode/emulator** — chỉ cần dòng `[✓] Flutter` là đủ để bắt đầu.

### Bước 2: Lấy code về máy

Đảm bảo bạn đã có sẵn thư mục `MoneyCare_App` (project Flutter gốc), và:
1. Copy toàn bộ 10 file `.dart` (được gửi kèm) vào thư mục `MoneyCare_App/test/`.
2. **Xóa file `widget_test.dart` cũ** nếu còn tồn tại trong `MoneyCare_App/test/` — đây là file mẫu Flutter tự sinh ra lúc tạo project (test 1 app đếm số bấm nút "+"), không còn phù hợp với app thật và sẽ báo lỗi nếu để lại.

Cấu trúc thư mục đúng phải như sau:

```
MoneyCare_App/
├── pubspec.yaml
├── lib/
│   ├── models/models.dart
│   ├── widgets/common_widgets.dart
│   └── ... (các thư mục khác)
└── test/                                  <-- chỉ giữ 10 file bên dưới, không còn widget_test.dart
    ├── week01_user_model_test.dart
    ├── week02_format_helpers_test.dart
    ├── week03_category_transaction_test.dart
    ├── week04_report_test.dart
    ├── week05_bill_test.dart
    ├── week06_notification_test.dart
    ├── week07_chatbot_test.dart
    ├── week08_insights_household_test.dart
    ├── week09_integrations_premium_test.dart
    └── week10_admin_test.dart
```

---

## 3. Chạy toàn bộ test lần đầu tiên

Mở **Terminal**, di chuyển vào đúng thư mục `MoneyCare_App` (thư mục chứa file `pubspec.yaml`):

```bash
cd đường-dẫn-tới/MoneyCare_App
```

### Bước 1 — Tải các gói thư viện (chỉ cần làm 1 lần, hoặc khi `pubspec.yaml` thay đổi)

```bash
flutter pub get
```

Chờ đến khi hiện dòng `Got dependencies!` là xong.

### Bước 2 — Chạy toàn bộ test

```bash
flutter test
```

Kết quả trông giống thế này khi tất cả đều đúng:

```
00:02 +58: All tests passed!
```

Con số `+58` nghĩa là 58 test đã chạy và **đều pass**. Nếu có test sai, sẽ hiện dạng:

```
00:01 +42 -1: week03_category_transaction_test.dart: TransactionModel toJson gửi đúng định dạng ISO8601 cho ngày giao dịch và giá trị số cho loại [E]
  Expected: '2026-08-27T00:00:00.000'
  Actual: '2026-08-27 00:00:00.000'
  ...
```

- Dấu `-1` nghĩa là có 1 test **thất bại (failed)**.
- Bên dưới luôn in ra **giá trị mong đợi (Expected)** và **giá trị thực tế (Actual)** để bạn so sánh — đọc kỹ 2 dòng này để biết chỗ sai nằm ở đâu (trong code thật hay trong bài test).

---

## 4. Đọc hiểu 1 file test mẫu

Mở file `week03_category_transaction_test.dart`, cấu trúc như sau:

```dart
void main() {
  group('CategoryModel', () {                 // "group" gộp các test liên quan vào 1 nhóm cho dễ đọc
    test('fromJson đọc đúng danh mục Chi', () {   // "test" là 1 bài kiểm tra cụ thể
      // Arrange (chuẩn bị) + Act (hành động): gọi hàm cần test với dữ liệu giả
      final c = CategoryModel.fromJson({"id": 1, "tenDanhMuc": "Ăn uống", "loai": 1, "mauSac": "#FF5733"});

      // Assert (khẳng định): so sánh kết quả với kỳ vọng
      expect(c.id, 1);
      expect(c.tenDanhMuc, "Ăn uống");
      expect(c.loai, LoaiGiaoDich.chi);
    });
  });
}
```

Giải thích:
- `void main()` — mọi file test Flutter đều bắt buộc phải có hàm `main()` này, đây là điểm bắt đầu chạy test của file đó.
- `group('Tên nhóm', () { ... })` — gom nhiều `test(...)` liên quan lại, chỉ để hiển thị đẹp hơn khi xem kết quả, không bắt buộc.
- `test('Mô tả bằng tiếng Việt', () { ... })` — 1 bài test, mô tả nên viết rõ ràng vì đây là dòng chữ hiện ra khi test fail, giúp bạn biết ngay đang sai chỗ nào mà không cần mở code.
- `expect(giá_trị_thực_tế, giá_trị_mong_đợi)` — hàm khẳng định chính, tương đương `Assert.Equal` bên C#.
- Một số matcher hay gặp trong bộ test này:
  - `expect(x, isNull)` / `expect(x, isNotNull)` — kiểm tra có null hay không.
  - `expect(list, hasLength(2))` — danh sách phải có đúng 2 phần tử.
  - `expect(list, isEmpty)` — danh sách rỗng.
  - `expect(x, isA<KieuDuLieu>())` — `x` đúng là kiểu `KieuDuLieu`.
  - `expect(json.containsKey('id'), false)` — object JSON KHÔNG được chứa khóa `'id'` (dùng để kiểm tra `toJson()` không gửi thừa trường lên server).
  - `expect(text, contains("từ khóa"))` — chuỗi `text` có chứa từ khóa.

---

## 5. Nội dung 10 file test (theo đúng 10 tuần trong phiếu tiến độ)

| File | Kiểm tra Model nào | Vì sao quan trọng |
|---|---|---|
| `week01_user_model_test.dart` | Các enum dùng chung (LoaiGiaoDich, GioiTinh...) và `UserModel` | Sai enum → hiển thị nhầm Thu thành Chi, nhầm giới tính |
| `week02_format_helpers_test.dart` | Hàm định dạng `tienVnd`, `ngayVn`, `mauTuHex` | Dùng ở KHẮP nơi trong app — sai 1 chỗ là sai toàn bộ màn hình hiển thị tiền/ngày |
| `week03_category_transaction_test.dart` | `CategoryModel`, `TransactionModel` | Đây là 2 model quan trọng nhất của cả app |
| `week04_report_test.dart` | `ReportModel`, `ReportCategorySlice` | Sai → biểu đồ báo cáo vẽ sai số liệu |
| `week05_bill_test.dart` | `BillModel` | Sai `soNgayConLai` → nhắc thanh toán sai ngày |
| `week06_notification_test.dart` | `ThongBaoModel`, `LoaiThongBaoX` | Sai → thông báo hiện sai loại, sai icon |
| `week07_chatbot_test.dart` | `CauHoiMau`, `ChatMessage` | Sai → giao diện chat hiển thị lộn ai là người dùng/ai là bot |
| `week08_insights_household_test.dart` | `InsightsModel`, `HouseholdModel` | Sai → gợi ý AI hoặc số liệu hộ gia đình hiển thị sai |
| `week09_integrations_premium_test.dart` | `FinancialLinkModel`, `PremiumOrderModel`, `ZaloPay*Model` | Sai → tính sai tiền Premium, hoặc đọc sai trạng thái thanh toán ZaloPay (rất nhạy cảm vì liên quan tiền thật) |
| `week10_admin_test.dart` | Các model trang Admin | Sai → trang quản trị hiển thị sai số liệu cho Admin |

> **Lưu ý:** phần OCR (quét hóa đơn) và QR (ảnh thanh toán) không có file test riêng trên Flutter, vì 2 tính năng này server trả về **byte ảnh thô** hoặc **JSON động không có model cố định** — toàn bộ logic xử lý nằm bên `MoneyCare.Api` (đã có test ở phiếu DACS). Bên Flutter chỉ đơn thuần hiển thị lại, không có logic để test riêng.

---

## 6. Chạy 1 phần nhỏ thay vì chạy hết

```bash
# Chỉ chạy 1 file
flutter test test/week03_category_transaction_test.dart

# Chỉ chạy các test có tên (mô tả) chứa 1 từ khóa nào đó
flutter test --plain-name "toJson"

# Chạy toàn bộ nhưng in chi tiết hơn (hữu ích khi debug)
flutter test --reporter expanded
```

Nếu dùng **VS Code** với extension Flutter/Dart: mở file test, sẽ thấy chữ `RUN` nhỏ màu xám hiện ngay phía trên mỗi `test(...)` — bấm vào để chạy riêng đúng test đó, rất tiện khi đang sửa lỗi 1 chỗ.

---

## 7. Xem "độ bao phủ" (code coverage) — không bắt buộc, nhưng nên biết

```bash
flutter test --coverage
```

Lệnh này tạo file `coverage/lcov.info`. Để xem báo cáo dạng HTML dễ đọc:

**Trên macOS/Linux** (cần cài `lcov` trước — macOS: `brew install lcov`, Ubuntu: `sudo apt install lcov`):
```bash
genhtml coverage/lcov.info -o coverage/html
open coverage/html/index.html    # macOS
xdg-open coverage/html/index.html   # Linux
```

**Trên Windows**: cài extension **"Coverage Gutters"** trong VS Code, mở 1 file `.dart` bất kỳ, bấm `Watch` ở thanh trạng thái dưới cùng — VS Code sẽ tô màu xanh (đã test)/đỏ (chưa test) ngay trên từng dòng code.

---

## 8. Xử lý lỗi thường gặp

| Lỗi gặp phải | Nguyên nhân | Cách sửa |
|---|---|---|
| `flutter: command not found` | Chưa cài Flutter SDK hoặc chưa thêm vào PATH | Cài lại theo mục 2, khởi động lại Terminal |
| `Target of URI doesn't exist: 'package:moneycare_app/models/models.dart'` | Chạy lệnh `flutter test` ở SAI thư mục (không phải thư mục gốc chứa `pubspec.yaml`), hoặc chưa `flutter pub get` | `cd` đúng vào thư mục `MoneyCare_App`, chạy lại `flutter pub get` rồi `flutter test` |
| `The getter 'xyz' isn't defined for the type 'AbcModel'` | Model thật trong `lib/models/models.dart` đã đổi tên trường sau khi test được viết | Mở `models.dart`, tìm đúng tên trường hiện tại, sửa lại trong file test cho khớp |
| `type 'Null' is not a subtype of type 'String'` khi chạy test | Test đưa 1 JSON thiếu trường bắt buộc (không có giá trị mặc định trong model) | Đọc lại constructor/`fromJson` của model đó xem trường nào bắt buộc, bổ sung vào JSON giả trong test |
| Test dùng `tienVnd`/`ngayVn` báo lỗi liên quan đến "locale" | Rất hiếm gặp — 1 số phiên bản gói `intl` yêu cầu gọi `initializeDateFormatting()` trước khi dùng | Thêm dòng `await initializeDateFormatting();` đầu hàm test đó (cần `import 'package:intl/date_symbol_data_local.dart';`) |
| `flutter pub get` báo lỗi version xung đột (`version solving failed`) | 2 gói trong `pubspec.yaml` yêu cầu version khác nhau của 1 gói con | Chạy `flutter pub upgrade` hoặc xem kỹ thông báo lỗi để biết gói nào cần hạ/nâng version |

---

## 9. Muốn tự viết thêm 1 test mới thì làm sao?

Tạo file mới trong thư mục `test/` (tên file phải kết thúc bằng `_test.dart` thì Flutter mới nhận diện), copy khuôn mẫu:

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('TenModelCanTest', () {
    test('Mô tả rõ ràng test đang kiểm tra cái gì', () {
      // Arrange + Act
      final ketQua = TenModelCanTest.fromJson({"truong1": "gia_tri_gia"});

      // Assert
      expect(ketQua.truong1, "gia_tri_gia");
    });
  });
}
```

Chạy lại `flutter test` để xem test mới có pass không.

---

## 10. Tóm tắt lệnh hay dùng nhất

```bash
cd MoneyCare_App                          # vào đúng thư mục có file pubspec.yaml
flutter pub get                           # chỉ cần 1 lần đầu (hoặc khi pubspec.yaml đổi)
flutter test                              # chạy TOÀN BỘ test — dùng lệnh này thường xuyên nhất
flutter test test/tenfile_test.dart       # chạy riêng 1 file
flutter test --plain-name "từ khóa"       # chạy riêng các test có tên chứa từ khóa
```

Chúc bạn kiểm thử vui vẻ — mỗi khi thấy dòng `All tests passed!` màu xanh là các Model trong app đang hoạt động đúng như kỳ vọng! 🎉
