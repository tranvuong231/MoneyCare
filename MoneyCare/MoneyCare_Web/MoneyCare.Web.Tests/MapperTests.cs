// Các Mapper chuyển DTO (JSON từ API) sang Model cục bộ mà Views/*.cshtml đang dùng - sai ở đây thì
// giao diện hiển thị sai mà không có exception nào báo, rất khó phát hiện bằng mắt khi chỉ test tay.
using MoneyCare.Models;
using MoneyCare.Web.Dtos;
using Xunit;

namespace MoneyCare.Web.Tests;

public class MapperTests
{
    [Fact]
    public void UserMapper_GhepThanhUrlDayDu_KhiAnhDaiDienLaDuongDanTuongDoi()
    {
        var dto = new UserDto { Id = "u1", DuongDanAnhDaiDien = "/uploads/avatars/u1.jpg" };

        var user = UserMapper.TuDto(dto, "https://api.moneycare.vn/");

        Assert.Equal("https://api.moneycare.vn/uploads/avatars/u1.jpg", user.DuongDanAnhDaiDien);
    }

    [Fact]
    public void UserMapper_GiuNguyenUrl_KhiAnhDaiDienDaLaUrlTuyetDoi()
    {
        // VD: ảnh đăng nhập bằng Google trả về sẵn 1 URL đầy đủ (googleusercontent.com...), KHÔNG
        // được ghép thêm apiBaseUrl vào trước, nếu không sẽ tạo ra 1 URL sai/hỏng.
        var dto = new UserDto { Id = "u1", DuongDanAnhDaiDien = "https://lh3.googleusercontent.com/a/anh.jpg" };

        var user = UserMapper.TuDto(dto, "https://api.moneycare.vn/");

        Assert.Equal("https://lh3.googleusercontent.com/a/anh.jpg", user.DuongDanAnhDaiDien);
    }

    [Fact]
    public void UserMapper_TraVeNull_KhiKhongCoAnhDaiDien()
    {
        var dto = new UserDto { Id = "u1", DuongDanAnhDaiDien = null };

        var user = UserMapper.TuDto(dto, "https://api.moneycare.vn/");

        Assert.Null(user.DuongDanAnhDaiDien);
    }

    [Fact]
    public void UserMapper_VanHoatDongDung_KhiKhongTruyenApiBaseUrl()
    {
        // Vài nơi gọi UserMapper.TuDto(dto) không kèm apiBaseUrl (tham số có giá trị mặc định null) -
        // phải giữ nguyên đường dẫn tương đối thay vì crash hay ghép ra chuỗi rác.
        var dto = new UserDto { Id = "u1", DuongDanAnhDaiDien = "/uploads/avatars/u1.jpg" };

        var user = UserMapper.TuDto(dto);

        Assert.Equal("/uploads/avatars/u1.jpg", user.DuongDanAnhDaiDien);
    }

    [Fact]
    public void UserMapper_SaoChepDungCacTruongCoBan()
    {
        var dto = new UserDto
        {
            Id = "u1",
            HoTen = "Trần Quốc Vương",
            Email = "vuong@moneycare.vn",
            IsPremium = true,
            HouseholdId = 5
        };

        var user = UserMapper.TuDto(dto);

        Assert.Equal("u1", user.Id);
        Assert.Equal("Trần Quốc Vương", user.HoTen);
        Assert.Equal("vuong@moneycare.vn", user.UserName); // UserName lấy từ Email (Web không có UserName riêng)
        Assert.True(user.IsPremium);
        Assert.Equal(5, user.HouseholdId);
    }

    [Fact]
    public void CategoryMapper_SaoChepDungCacTruong()
    {
        var dto = new CategoryDto { Id = 1, TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi, MauSac = "#FF5733" };

        var category = CategoryMapper.TuDto(dto);

        Assert.Equal(1, category.Id);
        Assert.Equal("Ăn uống", category.TenDanhMuc);
        Assert.Equal(LoaiGiaoDich.Chi, category.Loai);
        Assert.Equal("#FF5733", category.MauSac);
    }

    [Fact]
    public void TransactionMapper_SaoChepDungCacTruong()
    {
        var dto = new TransactionDto
        {
            Id = 10,
            SoTien = 150000,
            Loai = LoaiGiaoDich.Chi,
            NgayGiaoDich = new DateTime(2026, 8, 27),
            CategoryId = 2,
            GhiChu = "Ăn trưa"
        };

        var transaction = TransactionMapper.TuDto(dto);

        Assert.Equal(150000, transaction.SoTien);
        Assert.Equal(LoaiGiaoDich.Chi, transaction.Loai);
        Assert.Equal(new DateTime(2026, 8, 27), transaction.NgayGiaoDich);
        Assert.Equal("Ăn trưa", transaction.GhiChu);
    }
}
