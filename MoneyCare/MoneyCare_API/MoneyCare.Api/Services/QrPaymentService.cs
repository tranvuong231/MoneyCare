// Cung cấp chức năng tạo và xử lý thanh toán bằng mã QR.
using System.Text;
using QRCoder;

namespace MoneyCare.Services;

/// <summary>
/// Sinh mã QR theo chuẩn VietQR (dựa trên đặc tả EMVCo QR Code for Payment Systems,
/// được NAPAS/Ngân hàng Nhà nước Việt Nam áp dụng cho chuyển khoản liên ngân hàng).
/// Đây là chuẩn mở, không cần đăng ký API - bất kỳ app ngân hàng nào hỗ trợ VietQR
/// (hầu hết ngân hàng tại VN hiện nay) đều quét và điền sẵn thông tin chuyển khoản.
/// Muốn xác nhận thanh toán tự động (webhook) thì mới cần API đối tác của ngân hàng/cổng thanh toán.
/// </summary>
public class QrPaymentService : IQrPaymentService
{
    public byte[] TaoAnhVietQR(string soTaiKhoan, string binNganHang, decimal soTien, string noiDung)
    {
        var payload = TaoChuoiVietQR(soTaiKhoan, binNganHang, soTien, noiDung);
        return TaoAnhTuChuoi(payload);
    }

    public byte[] TaoAnhTuChuoi(string noiDungPayload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(noiDungPayload, QRCodeGenerator.ECCLevel.M);
        var pngQr = new PngByteQRCode(qrData);
        return pngQr.GetGraphic(10);
    }

    private static string TaoChuoiVietQR(string soTaiKhoan, string binNganHang, decimal soTien, string noiDung)
    {
        var sb = new StringBuilder();

        Tlv(sb, "00", "01"); // Payload Format Indicator
        Tlv(sb, "01", "12"); // Point of Initiation Method: 12 = dynamic (có số tiền cố định)

        // ==== Field 38: Thông tin tài khoản thụ hưởng (NAPAS) ====
        var thongTinTaiKhoan = new StringBuilder();
        Tlv(thongTinTaiKhoan, "00", "A000000727"); // GUID của NAPAS

        var chiTietTaiKhoan = new StringBuilder();
        Tlv(chiTietTaiKhoan, "00", binNganHang); // Mã BIN ngân hàng (VD: Vietcombank = 970436)
        Tlv(chiTietTaiKhoan, "01", soTaiKhoan);  // Số tài khoản thụ hưởng
        Tlv(thongTinTaiKhoan, "01", chiTietTaiKhoan.ToString());

        Tlv(thongTinTaiKhoan, "02", "QRIBFTTA"); // Dịch vụ: chuyển khoản tới số tài khoản
        Tlv(sb, "38", thongTinTaiKhoan.ToString());

        Tlv(sb, "53", "704"); // Mã tiền tệ: VND
        if (soTien > 0)
        {
            Tlv(sb, "54", ((long)soTien).ToString()); // Số tiền giao dịch
        }
        Tlv(sb, "58", "VN"); // Mã quốc gia

        // ==== Field 62: Dữ liệu bổ sung (nội dung chuyển khoản) ====
        if (!string.IsNullOrWhiteSpace(noiDung))
        {
            var duLieuBoSung = new StringBuilder();
            Tlv(duLieuBoSung, "08", ChuanHoaNoiDung(noiDung));
            Tlv(sb, "62", duLieuBoSung.ToString());
        }

        // ==== CRC checksum (field 63), tính trên toàn bộ chuỗi + "6304" ====
        sb.Append("6304");
        var crc = TinhCRC16(sb.ToString());
        sb.Append(crc);

        return sb.ToString();
    }

    private static void Tlv(StringBuilder sb, string id, string value)
    {
        sb.Append(id).Append(value.Length.ToString("D2")).Append(value);
    }

    private static string ChuanHoaNoiDung(string noiDung)
    {
        // Nội dung chuyển khoản VietQR thường yêu cầu không dấu, không ký tự đặc biệt
        var boDauTiengViet = System.Text.RegularExpressions.Regex.Replace(
            noiDung.Normalize(NormalizationForm.FormD), @"\p{Mn}", "");
        return System.Text.RegularExpressions.Regex.Replace(boDauTiengViet, @"[^a-zA-Z0-9 ]", "").Trim();
    }

    private static string TinhCRC16(string input)
    {
        ushort crc = 0xFFFF;
        var bytes = Encoding.ASCII.GetBytes(input);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc.ToString("X4");
    }
}
