// Tuần 6: hệ thống thông báo trong app (ThongBaoHelper - dùng chung cho mọi Controller) và quét hóa
// đơn bằng OCR (Tesseract) + làm giàu kết quả bằng AI cục bộ LM Studio, có lưới an toàn bằng quy tắc
// (regex) khi AI không khả dụng (OcrAiHelper). Dùng Moq để giả lập ILlmService, không cần LM Studio thật.
using Moq;
using MoneyCare.Models;
using MoneyCare.Services;
using Xunit;

namespace MoneyCare.Api.Tests;

public class Week06_ThongBaoAndOcrTests
{
    private const string UserId = "user-006";

    // ==== ThongBaoHelper ====

    [Fact]
    public async Task TaoAsync_LuuThongBaoMoiVaoDb()
    {
        await using var db = TestHelpers.TaoDbContext();

        await ThongBaoHelper.TaoAsync(db, UserId, "Vượt ngân sách", "Bạn đã chi vượt 20% ngân sách tháng này", LoaiThongBao.PhanTichAI);

        var thongBao = Assert.Single(db.ThongBaos);
        Assert.Equal(UserId, thongBao.UserId);
        Assert.Equal(LoaiThongBao.PhanTichAI, thongBao.Loai);
        Assert.False(thongBao.DaXem);
    }

    [Fact]
    public async Task DaCoHomNayAsync_TraVeTrue_KhiDaTaoCungTieuDeTrongNgay()
    {
        await using var db = TestHelpers.TaoDbContext();
        await ThongBaoHelper.TaoAsync(db, UserId, "Nhắc hóa đơn: Tiền điện", "Sắp đến hạn", LoaiThongBao.HoaDon);

        var daCo = await ThongBaoHelper.DaCoHomNayAsync(db, UserId, "Nhắc hóa đơn: Tiền điện");

        Assert.True(daCo);
    }

    [Fact]
    public async Task DaCoHomNayAsync_TraVeFalse_KhiTieuDeKhacHoacNguoiDungKhac()
    {
        await using var db = TestHelpers.TaoDbContext();
        await ThongBaoHelper.TaoAsync(db, UserId, "Nhắc hóa đơn: Tiền nước", "Sắp đến hạn", LoaiThongBao.HoaDon);

        Assert.False(await ThongBaoHelper.DaCoHomNayAsync(db, UserId, "Nhắc hóa đơn: Tiền điện"));
        Assert.False(await ThongBaoHelper.DaCoHomNayAsync(db, "nguoi-khac", "Nhắc hóa đơn: Tiền nước"));
    }

    // ==== OcrAiHelper.NgayHopLyHoacNull ====

    [Fact]
    public void NgayHopLyHoacNull_TraVeNull_KhiNgayOTuongLai()
    {
        Assert.Null(OcrAiHelper.NgayHopLyHoacNull(DateTime.Today.AddDays(1)));
    }

    [Fact]
    public void NgayHopLyHoacNull_TraVeNull_KhiNgayQuaCu_TrenHaiNam()
    {
        Assert.Null(OcrAiHelper.NgayHopLyHoacNull(DateTime.Today.AddYears(-3)));
    }

    [Fact]
    public void NgayHopLyHoacNull_GiuNguyenNgay_KhiHopLy()
    {
        var ngay = DateTime.Today.AddDays(-2);
        Assert.Equal(ngay, OcrAiHelper.NgayHopLyHoacNull(ngay));
    }

    // ==== OcrAiHelper.TaoGhiChuMacDinh ====

    [Fact]
    public void TaoGhiChuMacDinh_UuTienGhiChuGoiYCuaAi()
    {
        var ketQua = new KetQuaOcrHoaDon { GhiChuGoiY = "Mua sắm tại Coopmart", TenCuaHang = "Coopmart" };

        Assert.Equal("Mua sắm tại Coopmart", OcrAiHelper.TaoGhiChuMacDinh(ketQua));
    }

    [Fact]
    public void TaoGhiChuMacDinh_RoiVeTenCuaHang_KhiKhongCoGhiChuGoiY()
    {
        var ketQua = new KetQuaOcrHoaDon { TenCuaHang = "Điện Máy Xanh" };

        Assert.Equal("Hóa đơn Điện Máy Xanh", OcrAiHelper.TaoGhiChuMacDinh(ketQua));
    }

    [Fact]
    public void TaoGhiChuMacDinh_RoiVeChungChungTheoNgay_KhiKhongCoGiCa()
    {
        var ketQua = new KetQuaOcrHoaDon { NgayNhanDien = new DateTime(2026, 8, 20) };

        Assert.Equal("Hóa đơn quét ngày 20/08/2026", OcrAiHelper.TaoGhiChuMacDinh(ketQua));
    }

    // ==== OcrAiHelper.LamGiauKetQuaBangAiAsync ====

    [Fact]
    public async Task LamGiauKetQuaBangAiAsync_GhiDeBangKetQuaAi_KhiAiTraVeThanhCong()
    {
        var danhMuc = new List<Category> { new() { Id = 1, TenDanhMuc = "Ăn uống", Loai = LoaiGiaoDich.Chi } };
        var ketQua = new KetQuaOcrHoaDon { VanBanGoc = "SIÊU THỊ COOPMART\nTổng cộng: 150.000đ" };

        var llmGia = new Mock<ILlmService>();
        llmGia.Setup(x => x.PhanTichHoaDonAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new KetQuaPhanTichHoaDonAi
            {
                SoTien = 150000,
                Ngay = DateTime.Today,
                TenCuaHang = "Coopmart",
                DanhMucGoiY = "Ăn uống",
                Loai = "Chi"
            });

        await OcrAiHelper.LamGiauKetQuaBangAiAsync(llmGia.Object, ketQua, danhMuc);

        Assert.Equal("AI", ketQua.NguonPhanTich);
        Assert.Equal(150000, ketQua.SoTienNhanDien);
        Assert.Equal("Coopmart", ketQua.TenCuaHang);
        Assert.Equal(1, ketQua.DanhMucGoiYId);
    }

    [Fact]
    public async Task LamGiauKetQuaBangAiAsync_RoiVeQuyTac_KhiAiKhongKhaDung()
    {
        var danhMuc = new List<Category>();
        // Dòng 2 (mã số thuế) CỐ Ý có từ 4 chữ số trở lên để KHÔNG bị ghép vào dòng 1 - theo đúng quy
        // tắc thật trong DoTenCuaHangBangQuyTac (chỉ ghép dòng kế tiếp khi dòng đó có ÍT HƠN 4 chữ số,
        // vì nhiều số thường là địa chỉ/mã số thuế chứ không phải phần tiếp nối của tên đơn vị).
        var ketQua = new KetQuaOcrHoaDon
        {
            VanBanGoc = "CÔNG TY TNHH ABC\nMã số thuế: 0312345678\nTổng cộng: 50.000đ"
        };

        var llmGia = new Mock<ILlmService>();
        llmGia.Setup(x => x.PhanTichHoaDonAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((KetQuaPhanTichHoaDonAi?)null); // LM Studio chưa bật / lỗi mạng

        await OcrAiHelper.LamGiauKetQuaBangAiAsync(llmGia.Object, ketQua, danhMuc);

        Assert.Equal("QuyTac", ketQua.NguonPhanTich);
        Assert.Equal("CÔNG TY TNHH ABC", ketQua.TenCuaHang);
    }

    [Fact]
    public async Task LamGiauKetQuaBangAiAsync_GhepDongTenDonVi_KhiDongDauNganVaDongKeItSo()
    {
        // Khi dòng có từ khóa đơn vị NGẮN (<=30 ký tự) và dòng kế tiếp cũng ngắn (<=45 ký tự) và có
        // ÍT HƠN 4 chữ số (không phải địa chỉ/mã số thuế) -> quy tắc chủ động ghép 2 dòng lại cho đủ
        // nghĩa (OCR hay ngắt tên đơn vị dài thành 2 dòng, VD "CÔNG TY TNHH" xuống dòng "ABC Thương Mại").
        var ketQua = new KetQuaOcrHoaDon
        {
            VanBanGoc = "CÔNG TY TNHH ABC\nĐịa chỉ: 123 Nguyễn Trãi\nTổng cộng: 50.000đ"
        };
        var llmGia = new Mock<ILlmService>();
        llmGia.Setup(x => x.PhanTichHoaDonAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((KetQuaPhanTichHoaDonAi?)null);

        await OcrAiHelper.LamGiauKetQuaBangAiAsync(llmGia.Object, ketQua, new List<Category>());

        Assert.Equal("CÔNG TY TNHH ABC Địa chỉ: 123 Nguyễn Trãi", ketQua.TenCuaHang);
    }

    [Fact]
    public async Task LamGiauKetQuaBangAiAsync_KhongLamGi_KhiVanBanOcrRong()
    {
        var ketQua = new KetQuaOcrHoaDon { VanBanGoc = "" };
        var llmGia = new Mock<ILlmService>();

        await OcrAiHelper.LamGiauKetQuaBangAiAsync(llmGia.Object, ketQua, new List<Category>());

        llmGia.Verify(x => x.PhanTichHoaDonAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }
}
