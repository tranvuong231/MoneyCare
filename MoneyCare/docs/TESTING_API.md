# HƯỚNG DẪN CHẠY KIỂM THỬ TỰ ĐỘNG — MoneyCare.Api (DACS)

> Hướng dẫn này viết cho người **mới bắt đầu với kiểm thử tự động (automated testing)**, đi từng bước một, không bỏ qua bước nào. Đọc từ trên xuống, làm theo đúng thứ tự.

---

## 1. Kiểm thử tự động là gì, tại sao cần?

Bình thường bạn kiểm tra chương trình bằng cách bấm chạy app, đăng nhập, thử thêm 1 giao dịch, xem có ra kết quả đúng không. Cách đó gọi là **kiểm thử thủ công (manual test)** — chậm, dễ quên kiểm tra lại khi sửa code chỗ khác.

**Kiểm thử tự động (automated test)** là bạn viết sẵn các đoạn code nhỏ, mỗi đoạn gọi thẳng vào 1 hàm/1 API trong project của bạn, tự đưa dữ liệu giả vào, rồi tự so sánh kết quả trả về có đúng như mong đợi không — không cần bấm chuột, không cần chạy cả ứng dụng lên. Chỉ cần gõ 1 lệnh, máy tự chạy hết vài chục test trong vài giây và báo cái nào đúng (✅ Passed), cái nào sai (❌ Failed).

Project của bạn dùng bộ công cụ:
- **xUnit** — framework viết test cho C#/.NET (giống như "luật chơi" quy định cách viết 1 bài test).
- **Moq** — thư viện tạo "vật giả" (mock) thay cho những thứ khó test thật (ví dụ: giả lập AI LM Studio mà không cần AI thật đang chạy).
- **EF Core InMemory** — tạo 1 cơ sở dữ liệu giả nằm tạm trong RAM, không cần cài SQL Server để test.

---

## 2. Chuẩn bị máy tính

### Bước 1: Cài .NET 8 SDK

1. Vào trang: https://dotnet.microsoft.com/download/dotnet/8.0
2. Tải bản **SDK** (không phải Runtime) cho đúng hệ điều hành (Windows/macOS/Linux).
3. Cài đặt như phần mềm bình thường (Next → Next → Finish).
4. Mở **Command Prompt** (Windows) hoặc **Terminal** (macOS/Linux), gõ:
   ```
   dotnet --version
   ```
   Nếu hiện ra số như `8.0.xxx` là cài thành công. Nếu báo lỗi "không tìm thấy lệnh dotnet", khởi động lại máy rồi thử lại.

### Bước 2: Lấy code về máy

Đảm bảo bạn đã có sẵn thư mục `MoneyCare_API` (chứa `MoneyCare.Api` và `MoneyCare.Api.sln`), và đã copy thư mục `MoneyCare.Api.Tests` (được gửi kèm) vào **cùng cấp** với `MoneyCare.Api`, đồng thời ghi đè file `MoneyCare.Api.sln`. Cấu trúc thư mục đúng phải như sau:

```
MoneyCare_API/
├── MoneyCare.Api.sln              <-- file đã được ghi đè, giờ có 2 project
├── MoneyCare.Api/                 <-- project chính (code thật của app)
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   └── MoneyCare.Api.csproj
└── MoneyCare.Api.Tests/           <-- project test (mới thêm)
    ├── TestHelpers.cs
    ├── Week01_ApplicationUserTests.cs
    ├── Week02_JwtTokenServiceTests.cs
    ├── ... (đến Week10)
    └── MoneyCare.Api.Tests.csproj
```

Nếu bạn dùng **Visual Studio 2022**: mở file `MoneyCare.Api.sln`, bạn sẽ thấy 2 project hiện lên trong cửa sổ **Solution Explorer** bên phải: `MoneyCare.Api` và `MoneyCare.Api.Tests`.

---

## 3. Chạy toàn bộ test lần đầu tiên

Mở **Terminal** (trong Visual Studio: menu `View` → `Terminal`; hoặc mở Command Prompt/Terminal thường), di chuyển vào đúng thư mục `MoneyCare_API` (thư mục chứa file `.sln`):

```bash
cd đường-dẫn-tới/MoneyCare_API
```

### Bước 1 — Tải các gói thư viện cần thiết (chỉ cần làm 1 lần)

```bash
dotnet restore
```

Lệnh này đọc file `.csproj`, tự tải về máy các gói như `xunit`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`... (cần có mạng Internet). Chờ đến khi thấy dòng `Restore complete` là xong.

> ⚠️ Nếu báo lỗi liên quan đến version gói (package version conflict), xem mục **8. Xử lý lỗi thường gặp** bên dưới.

### Bước 2 — Build (biên dịch) thử

```bash
dotnet build
```

Nếu có lỗi cú pháp/kiểu dữ liệu, nó sẽ hiện ra ở bước này, dạng:
```
Error CS0246: The type or namespace name 'XYZ' could not be found
```
Chụp lại đoạn lỗi này gửi để được hỗ trợ sửa nếu gặp phải.

Nếu thấy dòng `Build succeeded` — tức là code test đã biên dịch được, sẵn sàng chạy.

### Bước 3 — Chạy tất cả test

```bash
dotnet test
```

Đợi vài giây đến vài chục giây (build lại rồi chạy toàn bộ ~70 test). Kết quả cuối cùng trông giống thế này:

```
Passed!  - Failed:     0, Passed:    72, Skipped:     0, Total:    72, Duration: 3 s
```

- **Passed: 72** → 72 test đều đúng như kỳ vọng. 🎉
- Nếu có dòng `Failed:` khác 0, kéo lên trên sẽ thấy chi tiết từng test bị sai, dạng:
  ```
  Failed MoneyCare.Api.Tests.Week03_CategoriesAndTransactionsControllerTests.Transactions_Tao_ThanhCong_LuuDungThongTin
  Error Message: Assert.Equal() Failure
  Expected: 30000
  Actual:   0
  ```
  Nghĩa là: test tên `Transactions_Tao_ThanhCong_LuuDungThongTin` mong đợi giá trị `30000` nhưng code thật trả về `0` → có khả năng logic trong `TransactionsController` bị sai (hoặc test viết sai giả định — cần đọc lại code thật để xác định bên nào sai).

---

## 4. Đọc hiểu 1 file test mẫu

Mở file `Week01_ApplicationUserTests.cs`, bạn sẽ thấy cấu trúc lặp lại như sau — đây là khuôn mẫu chuẩn của **mọi** bài test (gọi là mẫu **AAA — Arrange, Act, Assert**):

```csharp
[Fact]
public void CapNhatTichXanh_BatTichXanh_KhiCaEmailVaSdtDaXacNhan()
{
    // ARRANGE (chuẩn bị): tạo dữ liệu giả để test
    var user = new ApplicationUser { EmailConfirmed = true, PhoneNumberConfirmed = true };

    // ACT (hành động): gọi đúng hàm cần kiểm tra
    user.CapNhatTichXanh();

    // ASSERT (khẳng định): so sánh kết quả thực tế với kỳ vọng
    Assert.True(user.DaXacMinhDanhTinh);
}
```

Giải thích từng phần:
- `[Fact]` — đánh dấu đây là 1 bài test độc lập, không cần tham số đầu vào.
- `[Theory]` (dùng ở vài chỗ khác, ví dụ `Week01_ApplicationUserTests`, `Week05...`) — giống `[Fact]` nhưng chạy LẶP LẠI nhiều lần với các bộ dữ liệu khác nhau, khai báo bằng `[InlineData(...)]` phía trên. Ví dụ:
  ```csharp
  [Theory]
  [InlineData(true, false)]
  [InlineData(false, true)]
  public void CapNhatTichXanh_KhongBatTichXanh_KhiThieuMotTrongHaiDieuKien(bool email, bool sdt)
  ```
  → hàm này thực chất chạy 2 lần, lần 1 với `email=true, sdt=false`, lần 2 với `email=false, sdt=true`.
- Tên hàm test được đặt theo công thức: **`[Hàm_đang_test]_[Kết_quả_mong_đợi]_[Điều_kiện]`** — chỉ cần đọc tên hàm là hiểu ngay test đang kiểm tra cái gì, không cần đọc code bên trong.
- `Assert.Equal(a, b)` — khẳng định `a` phải bằng `b`.
- `Assert.True(x)` / `Assert.False(x)` — khẳng định `x` phải đúng/sai.
- `Assert.Single(list)` — khẳng định danh sách chỉ có đúng 1 phần tử.
- `Assert.Empty(list)` / `Assert.NotEmpty(list)` — danh sách rỗng / không rỗng.
- `Assert.Contains(list, x => ...)` — trong danh sách có ít nhất 1 phần tử thỏa điều kiện.
- `Assert.IsType<T>(obj)` — khẳng định `obj` đúng là kiểu `T` (ví dụ kiểm tra API trả về đúng `OkObjectResult` — HTTP 200 — chứ không phải lỗi).

---

## 5. Nội dung 10 file test (theo đúng 10 tuần trong phiếu tiến độ)

| File | Kiểm tra cái gì | Vì sao quan trọng |
|---|---|---|
| `Week01_ApplicationUserTests.cs` | Tự động bật "tích xanh" khi xác minh đủ email+SĐT; kiểm tra Premium còn hạn hay không | Đây là logic nền tảng, nhiều màn hình khác dựa vào |
| `Week02_JwtTokenServiceTests.cs` | Token đăng nhập (JWT) sinh ra đúng định dạng, đúng người dùng, đúng quyền, đúng hạn dùng | Nếu sai, người dùng có thể đăng nhập nhầm tài khoản hoặc bị Admin giả |
| `Week03_CategoriesAndTransactionsControllerTests.cs` | Không cho xóa danh mục đang có giao dịch; lọc đúng giao dịch theo loại/danh mục | Tránh mất dữ liệu, tránh hiện sai số liệu |
| `Week04_DashboardControllerTests.cs` | Tính đúng tổng thu/tổng chi hiển thị ở trang chủ | Sai chỗ này thì cả app "nói dối" về tài chính người dùng |
| `Week05_ExportAndBillsTests.cs` | File Excel/PDF xuất ra hợp lệ; đánh dấu hóa đơn đã thanh toán tự tạo đúng giao dịch | Tránh xuất file hỏng, tránh trùng/thiếu giao dịch |
| `Week06_ThongBaoAndOcrTests.cs` | Không gửi trùng thông báo trong ngày; quét hóa đơn OCR đọc đúng ngày/tên cửa hàng | Tránh spam thông báo, tránh OCR đọc sai số tiền |
| `Week07_QrAndChatbotTests.cs` | Ảnh mã QR sinh ra hợp lệ; chatbot trả lời đúng câu hỏi | Người dùng quét được QR, chatbot không trả lời linh tinh |
| `Week08_AiInsightAndHouseholdTests.cs` | Phân tích chi tiêu thống kê đúng; tạo/tham gia hộ gia đình bằng đúng mã mời | Gợi ý AI phải dựa trên số liệu đúng |
| `Week09_IntegrationsAndPremiumTests.cs` | Che đúng số tài khoản khi hiển thị; tính đúng tiền + đúng hạn khi mua Premium | Bảo mật số tài khoản, tránh tính sai tiền khách hàng |
| `Week10_AdminXoaHelperTests.cs` | Xóa dữ liệu Admin không làm hỏng liên kết dữ liệu khác | Tránh Admin xóa nhầm làm sập dữ liệu liên quan |

---

## 6. Chạy 1 phần nhỏ thay vì chạy hết

Khi đang sửa code và chỉ muốn test nhanh 1 phần, dùng bộ lọc `--filter`:

```bash
# Chỉ chạy các test trong 1 file/class
dotnet test --filter "FullyQualifiedName~Week03_CategoriesAndTransactionsControllerTests"

# Chỉ chạy đúng 1 hàm test
dotnet test --filter "FullyQualifiedName~Transactions_Tao_ThanhCong_LuuDungThongTin"
```

Nếu dùng **Visual Studio**: mở menu `Test` → `Test Explorer`, danh sách toàn bộ test hiện ra dạng cây, bấm chuột phải vào 1 test hoặc 1 file → `Run` để chạy riêng, hoặc bấm nút ▶ màu xanh bên cạnh mỗi test ngay trong code.

---

## 7. Xem "độ bao phủ" (code coverage) — không bắt buộc, nhưng nên biết

Code coverage cho biết bao nhiêu % dòng code thật đã được test "chạm" tới.

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Lệnh này tạo ra 1 file báo cáo dạng `.xml` trong thư mục `MoneyCare.Api.Tests/TestResults/<mã-số-ngẫu-nhiên>/coverage.cobertura.xml`. Để xem báo cáo dễ đọc hơn (dạng HTML, có màu xanh/đỏ từng dòng code):

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"MoneyCare.Api.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Sau đó mở file `coveragereport/index.html` bằng trình duyệt để xem.

---

## 8. Xử lý lỗi thường gặp

| Lỗi gặp phải | Nguyên nhân | Cách sửa |
|---|---|---|
| `dotnet: command not found` | Chưa cài .NET SDK hoặc chưa khởi động lại máy | Cài lại theo mục 2, khởi động lại Terminal/máy |
| `NU1101: Unable to find package` khi `dotnet restore` | Không có mạng, hoặc mạng chặn NuGet | Kiểm tra kết nối Internet, thử lại; nếu dùng mạng công ty/trường có thể bị chặn, thử mạng khác |
| `error CS0246: The type or namespace name 'XYZ' could not be found` | Test tham chiếu đến 1 class/hàm không khớp tên với code thật (do code đã đổi tên sau khi test được viết) | Mở đúng file `.cs` chứa class đó trong `MoneyCare.Api`, so tên chính xác, sửa lại tên trong file test cho khớp |
| `error CS1061: 'X' does not contain a definition for 'Y'` | Tương tự trên — tên thuộc tính/hàm bị lệch | Mở model/service thật, xem tên đúng, sửa lại test |
| Test bị `Failed` với `Assert.Equal` báo `Expected != Actual` | Logic code thật đã thay đổi so với khi viết test, HOẶC code thật đang có lỗi | Đọc kỹ thông báo lỗi (nó luôn in ra giá trị mong đợi và giá trị thực tế), đối chiếu lại code — đây chính là lúc kiểm thử tự động phát huy tác dụng: phát hiện được chỗ sai! |
| `System.InvalidOperationException: Transactions are not supported by the in-memory store` | Có đoạn code gọi `Database.BeginTransactionAsync()` mà EF Core InMemory không hỗ trợ transaction | Bộ test hiện tại đã tránh gọi vào những hàm dùng transaction; nếu bạn tự viết thêm test cho hàm đó, cần đổi sang dùng gói `Microsoft.EntityFrameworkCore.Sqlite` (SQLite In-Memory) thay vì `InMemory` |
| Build chậm lần đầu | Bình thường — lần đầu build sẽ tải thêm gói, các lần sau nhanh hơn nhiều | Kiên nhẫn chờ, không cần làm gì thêm |

---

## 9. Muốn tự viết thêm 1 test mới thì làm sao?

Copy khuôn mẫu này, đổi tên hàm và nội dung bên trong:

```csharp
[Fact]
public async Task TenHam_KetQuaMongDoi_DieuKien()
{
    // Arrange
    await using var db = TestHelpers.TaoDbContext();      // tạo DB giả
    var controller = new TenControllerCanTest(db);         // khởi tạo controller cần test
    TestHelpers.DangNhapGia(controller, "user-id-bat-ky");  // giả lập đã đăng nhập

    // Act
    var ketQua = await controller.TenHamCanGoi(thamSo);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(ketQua);
    // ... kiểm tra thêm nội dung ok.Value nếu cần
}
```

Đặt file mới trong cùng thư mục `MoneyCare.Api.Tests/`, chạy lại `dotnet test` để xem test mới có pass không.

---

## 10. Tóm tắt lệnh hay dùng nhất

```bash
cd MoneyCare_API              # vào đúng thư mục có file .sln
dotnet restore                # chỉ cần 1 lần đầu (hoặc khi thêm gói mới)
dotnet test                   # chạy TOÀN BỘ test — dùng lệnh này thường xuyên nhất
dotnet test --filter "FullyQualifiedName~TenFileHoacTenHam"   # chạy riêng 1 phần
```

Chúc bạn kiểm thử vui vẻ — mỗi khi thấy dòng `Passed!` màu xanh là code của bạn đang chạy đúng như kỳ vọng! 🎉
