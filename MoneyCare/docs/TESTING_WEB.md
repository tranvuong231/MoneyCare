# HƯỚNG DẪN CHẠY KIỂM THỬ TỰ ĐỘNG — MoneyCare.Web (DACS - phần Web/MVC)

> Hướng dẫn này viết cho người **mới bắt đầu với kiểm thử tự động**, dành riêng cho project `MoneyCare.Web` (khác với `MoneyCare.Api` đã có hướng dẫn riêng ở file `TESTING_API.md`). Đọc từ trên xuống, làm theo đúng thứ tự.

---

## 1. Vì sao Web cần 1 bộ test RIÊNG, khác với API?

Câu hỏi rất hay — trả lời thẳng: **có, nên test cả 2, nhưng test 2 THỨ KHÁC NHAU**, vì `MoneyCare.Web` và `MoneyCare.Api` đóng 2 vai trò hoàn toàn khác nhau trong hệ thống:

| | MoneyCare.Api | MoneyCare.Web |
|---|---|---|
| Vai trò | Nơi giữ **toàn bộ nghiệp vụ thật**: tính tiền, kiểm tra hợp lệ, đọc/ghi database | Chỉ là **1 khách hàng gọi API** (client) — nhận dữ liệu từ API rồi hiển thị ra trang HTML |
| Có đụng Database không | Có (Entity Framework Core + SQL Server) | **Không** — Web không kết nối Database, không dùng `UserManager`/`ApplicationDbContext` trực tiếp |
| Nếu có bug nghiêm trọng | Tính sai tiền, mất dữ liệu, bảo mật | Hiển thị sai giao diện, thông báo lỗi khó hiểu, ảnh vỡ link |
| Vì vậy cần test | Rất nhiều: mọi Controller, Service, Model (đã có 10 file `Week01..10`) | Ít hơn nhiều, nhưng vẫn có vài chỗ **logic thật** đáng để test (xem mục 2) |

Nói cách khác: **`MoneyCare.Web` không phải "không cần test" — chỉ là phần LỚN của nó (Controller mỏng, giao diện Razor `.cshtml`) không phù hợp để viết unit test**, còn phần code xử lý logic thật của nó (dưới đây) thì vẫn rất nên test, và mình đã bổ sung 3 file test mới cho đúng những phần đó.

### Vậy phần nào của Web viết được unit test, phần nào không?

- ✅ **`Services/MoneyCareApiClient.cs`** — đây là NƠI DUY NHẤT Web gọi sang API (mọi Controller của Web đều đi qua đây, không tự gọi `HttpClient` riêng). Nó có logic thật: parse JSON, xử lý các dạng lỗi khác nhau trả về từ API, ghép URL. → **Test được, và rất nên test.**
- ✅ **`Services/CurrentUserService.cs`** — dùng ở MỌI trang để biết ai đang đăng nhập, có Admin không, có bao nhiêu thông báo chưa đọc. Có logic **cache trong 1 request** (chỉ gọi API 1 lần) cần đảm bảo đúng. → **Test được.**
- ✅ **`Dtos/Mappers.cs`** (`UserMapper`, `CategoryMapper`, `TransactionMapper`) — chuyển dữ liệu JSON từ API sang Model để đưa vào View. Có 1 chỗ dễ sai: ghép URL ảnh đại diện. → **Test được.**
- ❌ **Các Controller** (`AccountController`, `TransactionsController`...) — hầu hết chỉ gọi `_api.GetAsync(...)` rồi trả `View(ketQua)`, gần như không có "quyết định" nào để test (nếu API đúng thì Controller chắc chắn đúng). Muốn test tầng này cần loại test khác (**integration test** dùng `WebApplicationFactory`, giả lập cả pipeline HTTP) — phức tạp hơn nhiều và ngoài phạm vi unit test.
- ❌ **Các file `.cshtml`** (giao diện Razor) — kiểm tra bằng mắt hoặc bằng **UI test** (Selenium/Playwright, bấm chuột thật trên trình duyệt giả lập) — cũng là loại test khác, không phải unit test.

→ Bộ 3 file test mới (`MoneyCareApiClientTests.cs`, `CurrentUserServiceTests.cs`, `MapperTests.cs`) đã che phủ đúng phần logic thật, đáng test nhất của `MoneyCare.Web`.

---

## 2. Chuẩn bị

Bạn đã cài .NET 8 SDK ở phần trước (hướng dẫn DACS API) — dùng lại được, không cần cài thêm gì.

### Copy code vào đúng vị trí

Giải nén file `MoneyCare_Web_KiemThuTuDong_Moi.zip`, bạn sẽ có:

```
MoneyCare_Web_Tests_Moi/
└── MoneyCare_Web/
    ├── MoneyCare.Web.sln                 <-- ghi đè file .sln cũ
    └── MoneyCare.Web.Tests/              <-- project mới, copy vào
        ├── FakeHttpMessageHandler.cs
        ├── MoneyCareApiClientTests.cs
        ├── CurrentUserServiceTests.cs
        ├── MapperTests.cs
        └── MoneyCare.Web.Tests.csproj
```

Copy đúng vào thư mục `MoneyCare_Web` gốc của bạn, cấu trúc cuối cùng phải là:

```
MoneyCare_Web/
├── MoneyCare.Web.sln              <-- đã ghi đè, giờ có 2 project
├── MoneyCare.Web/                 <-- project chính (Controllers, Views, Services...)
└── MoneyCare.Web.Tests/           <-- project test (mới thêm)
```

> ⚠️ Đây là project **RIÊNG**, khác với `MoneyCare.Api.Tests` (nằm trong thư mục `MoneyCare_API`). Bạn sẽ có tổng cộng **2 project test độc lập** — mỗi cái build/chạy riêng, ở 2 thư mục khác nhau.

---

## 3. Chạy test

Mở Terminal, di chuyển vào thư mục `MoneyCare_Web` (nơi có file `MoneyCare.Web.sln`):

```bash
cd đường-dẫn-tới/MoneyCare_Web
dotnet restore
dotnet test
```

Kết quả mong đợi:

```
Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21
```

(21 test: 7 ở `MoneyCareApiClientTests`, 7 ở `CurrentUserServiceTests`, 7 ở `MapperTests` — quan trọng nhất là **Failed: 0**.)

Các lệnh khác (giống hệt bên API, xem lại `TESTING_API.md` mục 6-9 nếu cần nhắc lại):

```bash
# Chạy riêng 1 file
dotnet test --filter "FullyQualifiedName~MoneyCareApiClientTests"

# Chạy riêng 1 test
dotnet test --filter "FullyQualifiedName~BaseUrl_LuonBoDauGachCheoCuoiChuoi"
```

---

## 4. Ý tưởng cốt lõi: "giả lập API" bằng `FakeHttpMessageHandler`

Điểm khác biệt lớn nhất so với bộ test bên API: `MoneyCare.Web` **không có Database** để tạo dữ liệu giả (không dùng EF Core InMemory được, vì Web không hề dùng EF Core). Thay vào đó, `MoneyCareApiClient` nhận 1 `HttpClient` qua constructor — và `HttpClient` cho phép "cắm" vào 1 bộ xử lý giả (gọi là `HttpMessageHandler`) thay cho việc gửi request đi Internet thật.

File `FakeHttpMessageHandler.cs` chính là "bộ đóng vai API" đó:

```csharp
var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK, "{\"id\":\"u1\",\"hoTen\":\"Trần Quốc Vương\"}");
var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

var ketQua = await client.GetAsync<NguoiDungGia>("/api/auth/toi");
```

Đọc theo thứ tự:
1. `TraVeJson(HttpStatusCode.OK, "...")` — nói với handler giả: "bất kể ai gọi gì, cứ trả về mã 200 kèm đúng JSON này".
2. `TaoHttpClient(handler)` — bọc handler giả vào trong 1 `HttpClient` thật (để `MoneyCareApiClient` dùng như bình thường, không biết là giả).
3. `client.GetAsync<NguoiDungGia>(...)` — gọi hàm THẬT trong `MoneyCareApiClient`, nhưng vì `HttpClient` bên trong đã bị "đánh lận" nên không hề có request nào bay ra Internet, chỉ nhận đúng JSON mình đã định nghĩa sẵn.

Cách này gọi là **giả lập (mocking) ở tầng HTTP** — kỹ thuật rất phổ biến khi test bất kỳ đoạn code nào gọi API bên ngoài, không riêng gì ASP.NET Core.

Với những trường hợp phức tạp hơn (muốn kiểm tra request gửi đi đúng như thế nào, hoặc trả lời khác nhau tùy request), dùng dạng đầy đủ hơn:

```csharp
var handler = new FakeHttpMessageHandler(request =>
{
    // "request" chính là những gì MoneyCareApiClient VỪA gửi đi - có thể kiểm tra request.RequestUri, v.v.
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = ... };
});
```

---

## 5. Nội dung 3 file test

| File | Kiểm tra cái gì | Vì sao quan trọng |
|---|---|---|
| `MoneyCareApiClientTests.cs` | Gọi API thành công parse đúng JSON; gọi API lỗi ném đúng `ApiException` với thông báo lấy từ `{"loi":...}` hoặc gộp từ `{"errors":{...}}`; xử lý đúng khi API trả 204 (không nội dung) hoặc body không phải JSON hợp lệ; tải đúng file nhị phân (ảnh/Excel/PDF); `BaseUrl` bỏ đúng dấu `/` cuối | Đây là **CỔNG DUY NHẤT** Web nói chuyện với API — sai ở đây thì TOÀN BỘ trang web bị ảnh hưởng, không riêng 1 chức năng nào |
| `CurrentUserServiceTests.cs` | Trả về `null` khi chưa đăng nhập (không crash trang); **chỉ gọi API 1 lần** dù được hỏi nhiều lần trong 1 trang; `LaAdminAsync`/`SoChuaXemThongBaoAsync` trả về đúng giá trị, và **không làm sập cả trang** khi API bị lỗi tạm thời | Hàm này chạy ở **MỌI trang** (`_Layout.cshtml`) — sai 1 chỗ nhỏ ở đây ảnh hưởng toàn bộ ứng dụng, kể cả trang không liên quan |
| `MapperTests.cs` | `UserMapper` ghép đúng URL ảnh đại diện khi là đường dẫn tương đối, GIỮ NGUYÊN khi đã là URL đầy đủ (ảnh Google); `CategoryMapper`/`TransactionMapper` sao chép đúng từng trường | Sai ở đây → ảnh vỡ link, hoặc hiển thị nhầm dữ liệu mà không có lỗi nào báo ra |

---

## 6. Đọc hiểu nhanh 1 test mẫu (nếu bạn chưa đọc phần này ở hướng dẫn API)

```csharp
[Fact]
public async Task GetAsync_NemApiException_VoiThongBaoTuTruong_loi_KhiApiTraVeLoiNghiepVu()
{
    // Arrange: giả lập API trả lỗi 400 kèm JSON {"loi": "Danh mục không hợp lệ"}
    var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.BadRequest, "{\"loi\":\"Danh mục không hợp lệ\"}");
    var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

    // Act + Assert cùng lúc: Assert.ThrowsAsync kiểm tra ĐÚNG kiểu exception phải được ném ra
    var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<NguoiDungGia>("/api/categories/999"));

    Assert.Equal("Danh mục không hợp lệ", ex.Message);   // thông báo lỗi phải lấy đúng từ JSON
    Assert.Equal(400, ex.StatusCode);                      // mã lỗi phải giữ đúng 400
}
```

- `Assert.ThrowsAsync<TenException>(() => hàm_bất_đồng_bộ())` — khẳng định 1 hàm **phải ném ra đúng loại exception** này, và cho phép kiểm tra tiếp nội dung bên trong exception đó (`ex.Message`, `ex.StatusCode`).

---

## 7. Xử lý lỗi thường gặp (đặc thù riêng cho project Web)

| Lỗi gặp phải | Nguyên nhân | Cách sửa |
|---|---|---|
| `error CS0122: 'MoneyCareApiClient' is inaccessible due to its protection level` | Rất hiếm, nhưng nếu gặp: class chưa khai báo `public` | Mở `Services/MoneyCareApiClient.cs`, đảm bảo `public class MoneyCareApiClient` |
| `Unable to resolve service for type 'IHttpContextAccessor'` | Chỉ xảy ra nếu bạn TỰ viết thêm test cho `JwtForwardingHandler` (không có trong 3 file gửi kèm, vì nó cần `HttpContext` giả — phức tạp hơn, thuộc dạng integration test) | Nếu chưa cần test phần này, bỏ qua; nếu cần, phải tự dựng `DefaultHttpContext` giả và `IHttpContextAccessor` giả bằng Moq |
| Build lỗi `Microsoft.NET.Sdk.Web` liên quan đến `wwwroot`/View compilation khi build project Test | `MoneyCare.Web.Tests` tham chiếu (`ProjectReference`) tới `MoneyCare.Web` — quá trình build sẽ build luôn `MoneyCare.Web` (kể cả build Razor Views). Nếu `MoneyCare.Web` đang có lỗi biên dịch (view lỗi cú pháp...), test cũng sẽ báo lỗi kèm theo | Chạy `dotnet build` riêng cho `MoneyCare.Web` trước để chắc chắn project chính không lỗi, rồi mới `dotnet test` bên `MoneyCare.Web.Tests` |
| 2 project `MoneyCare.Api.Tests` và `MoneyCare.Web.Tests` bị lẫn, chạy `dotnet test` không thấy gì | Đang đứng SAI thư mục — mỗi project test nằm trong 1 thư mục `.sln` riêng (`MoneyCare_API` và `MoneyCare_Web`), phải `cd` đúng vào từng thư mục rồi chạy `dotnet test` riêng cho từng cái | Chạy `dotnet test` trong `MoneyCare_API` cho bộ 10 file Week01-10; chạy `dotnet test` trong `MoneyCare_Web` cho bộ 3 file Web |

---

## 8. Tóm tắt — bạn hiện có TỔNG CỘNG mấy bộ test?

Đến thời điểm này, cả đồ án của bạn có **3 bộ test độc lập**, mỗi bộ chạy riêng bằng 1 lệnh, ở 3 thư mục khác nhau:

| Bộ test | Ở đâu | Chạy bằng | Xem hướng dẫn |
|---|---|---|---|
| 1. Backend API (10 file Week01-10) | `MoneyCare_API/MoneyCare.Api.Tests/` | `cd MoneyCare_API && dotnet test` | `TESTING_API.md` |
| 2. Web/MVC client (3 file mới) | `MoneyCare_Web/MoneyCare.Web.Tests/` | `cd MoneyCare_Web && dotnet test` | File này (`TESTING_WEB.md`) |
| 3. App Flutter (10 file week01-10) | `MoneyCare_App/test/` | `cd MoneyCare_App && flutter test` | `TESTING_FLUTTER.md` |

Cả 3 bộ đều là **unit test** (chạy độc lập, không cần backend/database/emulator thật đang chạy) — đúng tinh thần kiểm thử tự động: viết 1 lần, chạy lại bất cứ khi nào sửa code để chắc chắn không làm hỏng thứ đang hoạt động đúng.

Chúc bạn kiểm thử vui vẻ! 🎉
