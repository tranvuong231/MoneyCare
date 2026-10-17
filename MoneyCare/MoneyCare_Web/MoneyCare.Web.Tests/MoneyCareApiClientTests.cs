// MoneyCareApiClient là NƠI DUY NHẤT MoneyCare_Web giao tiếp với MoneyCare_API. Vì Web không còn tự
// đụng Database/Identity, đây là phần code quan trọng nhất bên Web cần kiểm thử: gọi API thành công
// phải parse đúng JSON, gọi API thất bại phải ném đúng ApiException với thông báo lỗi dễ hiểu để hiển
// thị lại cho người dùng.
using System.Net;
using System.Text;
using MoneyCare.Web.Services;
using Xunit;

namespace MoneyCare.Web.Tests;

public class MoneyCareApiClientTests
{
    private record NguoiDungGia(string Id, string? HoTen);

    [Fact]
    public async Task GetAsync_TraVeDungDoiTuong_KhiApiThanhCong()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.OK, "{\"id\":\"u1\",\"hoTen\":\"Trần Quốc Vương\"}");
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var ketQua = await client.GetAsync<NguoiDungGia>("/api/auth/toi");

        Assert.NotNull(ketQua);
        Assert.Equal("Trần Quốc Vương", ketQua!.HoTen);
    }

    [Fact]
    public async Task GetAsync_TraVeMacDinh_KhiApiTraVe204KhongCoNoiDung()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var ketQua = await client.GetAsync<NguoiDungGia>("/api/khong-co-gi");

        Assert.Null(ketQua);
    }

    [Fact]
    public async Task GetAsync_NemApiException_VoiThongBaoTuTruong_loi_KhiApiTraVeLoiNghiepVu()
    {
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.BadRequest, "{\"loi\":\"Danh mục không hợp lệ\"}");
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<NguoiDungGia>("/api/categories/999"));

        Assert.Equal("Danh mục không hợp lệ", ex.Message);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task GetAsync_GopDungThongBaoLoi_TuTruong_errors_KhiApiTraVeLoiValidate()
    {
        var json = "{\"errors\":{\"SoTien\":[\"Số tiền phải lớn hơn 0\"],\"CategoryId\":[\"Vui lòng chọn danh mục\"]}}";
        var handler = FakeHttpMessageHandler.TraVeJson(HttpStatusCode.BadRequest, json);
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.PostAsync<NguoiDungGia>("/api/transactions", new { }));

        Assert.Contains("Số tiền phải lớn hơn 0", ex.Message);
        Assert.Contains("Vui lòng chọn danh mục", ex.Message);
    }

    [Fact]
    public async Task GetAsync_DungThongBaoMacDinhTheoMaLoi_KhiNoiDungLoiKhongPhaiJsonHopLe()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>500 - Internal Server Error</html>", Encoding.UTF8, "text/html")
        });
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAsync<NguoiDungGia>("/api/loi"));

        Assert.Equal("Yêu cầu thất bại (mã 500)", ex.Message);
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task GetBytesAsync_TraVeDungByteVaContentType_ChoFileTaiVe()
    {
        var duLieuGia = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // chữ ký .xlsx
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(duLieuGia)
            };
            res.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats");
            return res;
        });
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler));

        var (bytes, contentType, _) = await client.GetBytesAsync("/api/export/excel");

        Assert.Equal(duLieuGia, bytes);
        Assert.Equal("application/vnd.openxmlformats", contentType);
    }

    [Fact]
    public void BaseUrl_LuonBoDauGachCheoCuoiChuoi()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new MoneyCareApiClient(FakeHttpMessageHandler.TaoHttpClient(handler, "https://api.moneycare.vn/"));

        Assert.Equal("https://api.moneycare.vn", client.BaseUrl);
    }
}
