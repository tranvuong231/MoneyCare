// CurrentUserService thay thế UserManager/DbContext trong _Layout.cshtml - MỌI trang đều gọi hàm này
// để biết ai đang đăng nhập. Quan trọng nhất cần đảm bảo: (1) chỉ gọi API 1 LẦN dù được hỏi nhiều lần
// trong cùng 1 request (tránh gọi API thừa làm chậm trang), và (2) không được crash cả trang khi API
// lỗi/hết hạn đăng nhập - phải rơi về "chưa đăng nhập" (null) một cách êm ái.
using System.Net;
using MoneyCare.Web.Services;
using Xunit;

namespace MoneyCare.Web.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public async Task GetAppUserAsync_TraVeNull_KhiApiTraVeLoi401ChuaDangNhap()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.Unauthorized, "{\"loi\":\"Chưa đăng nhập\"}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        var user = await service.GetAppUserAsync();

        Assert.Null(user);
    }

    [Fact]
    public async Task GetAppUserAsync_TraVeDungThongTin_KhiApiThanhCong()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK,
            "{\"id\":\"u1\",\"hoTen\":\"Nguyễn Anh Vũ\",\"email\":\"vu@moneycare.vn\",\"duongDanAnhDaiDien\":\"/uploads/avatars/u1.jpg\"}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler, "https://api.moneycare.vn/"));
        var service = new CurrentUserService(api);

        var user = await service.GetAppUserAsync();

        Assert.NotNull(user);
        Assert.Equal("Nguyễn Anh Vũ", user!.HoTen);
        // Ảnh đại diện là đường dẫn tương đối trên API -> phải được UserMapper ghép thành URL đầy đủ.
        Assert.Equal("https://api.moneycare.vn/uploads/avatars/u1.jpg", user.DuongDanAnhDaiDien);
    }

    [Fact]
    public async Task GetAppUserAsync_ChiGoiApiDungMotLan_DuDuocHoiNhieuLanTrongCungMotRequest()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK, "{\"id\":\"u1\",\"hoTen\":\"Trần Quốc Vương\"}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        await service.GetAppUserAsync();
        await service.GetAppUserAsync();
        await service.GetAppUserAsync();

        Assert.Equal(1, handler.SoLanGoi);
    }

    [Fact]
    public async Task LaAdminAsync_TraVeTrue_KhiApiBaoLaAdmin()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK, "{\"id\":\"u1\",\"laAdmin\":true}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        Assert.True(await service.LaAdminAsync());
    }

    [Fact]
    public async Task LaAdminAsync_TraVeFalse_KhiChuaDangNhap()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.Unauthorized, "{\"loi\":\"Chưa đăng nhập\"}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        Assert.False(await service.LaAdminAsync());
    }

    [Fact]
    public async Task SoChuaXemThongBaoAsync_TraVeDungSoLuong_KhiApiThanhCong()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK, "{\"soChuaXem\":7}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        Assert.Equal(7, await service.SoChuaXemThongBaoAsync());
    }

    [Fact]
    public async Task SoChuaXemThongBaoAsync_TraVe0_KhiApiLoi_KhongDeVoTrangChu()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.InternalServerError, "{\"loi\":\"Lỗi máy chủ\"}");
        var api = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));
        var service = new CurrentUserService(api);

        // Đây là chuông thông báo hiện ở MỌI trang (_Layout.cshtml) - nếu ném exception thay vì trả
        // về 0, một lỗi tạm thời của API "so-chua-xem" sẽ làm SẬP toàn bộ trang chứ không chỉ mỗi chuông.
        Assert.Equal(0, await service.SoChuaXemThongBaoAsync());
    }
}
