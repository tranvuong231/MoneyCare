// Giao diện định nghĩa các chức năng xuất dữ liệu.
using MoneyCare.Models;

namespace MoneyCare.Services;

public interface IExportService
{
    byte[] XuatExcel(List<Transaction> giaoDichs, string tenNguoiDung, DateTime tuNgay, DateTime denNgay);
    byte[] XuatPdf(List<Transaction> giaoDichs, string tenNguoiDung, DateTime tuNgay, DateTime denNgay);
}
