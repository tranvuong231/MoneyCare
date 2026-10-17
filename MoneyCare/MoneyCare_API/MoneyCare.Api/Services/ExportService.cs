// Cung cấp chức năng xuất dữ liệu ra các định dạng tệp.
using ClosedXML.Excel;
using MoneyCare.Models;
using MoneyCare.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyCare.Services;

public class ExportService : IExportService
{
    public byte[] XuatExcel(List<Transaction> giaoDichs, string tenNguoiDung, DateTime tuNgay, DateTime denNgay)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Giao dịch");

        // Tiêu đề
        sheet.Cell(1, 1).Value = "BÁO CÁO THU CHI CÁ NHÂN";
        sheet.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.SetFontSize(16);
        sheet.Cell(2, 1).Value = $"Người dùng: {tenNguoiDung}";
        sheet.Cell(3, 1).Value = $"Khoảng thời gian: {tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}";
        sheet.Range(2, 1, 3, 1).Style.Font.SetItalic();

        // Header bảng
        int hangHeader = 5;
        string[] tieuDe = { "Ngày", "Loại", "Danh mục", "Ghi chú", "Số tiền (đ)" };
        for (int i = 0; i < tieuDe.Length; i++)
        {
            var cell = sheet.Cell(hangHeader, i + 1);
            cell.Value = tieuDe[i];
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#0d6efd"));
            cell.Style.Font.SetFontColor(XLColor.White);
        }

        int hang = hangHeader + 1;
        foreach (var gd in giaoDichs.OrderBy(g => g.NgayGiaoDich))
        {
            sheet.Cell(hang, 1).Value = gd.NgayGiaoDich;
            sheet.Cell(hang, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(hang, 2).Value = gd.Loai == LoaiGiaoDich.Thu ? "Thu" : "Chi";
            sheet.Cell(hang, 3).Value = gd.Category?.TenDanhMuc ?? "";
            sheet.Cell(hang, 4).Value = gd.GhiChu ?? "";
            sheet.Cell(hang, 5).Value = gd.Loai == LoaiGiaoDich.Thu ? (double)gd.SoTien : -(double)gd.SoTien;
            sheet.Cell(hang, 5).Style.NumberFormat.Format = "#,##0";
            hang++;
        }

        // Tổng kết
        hang += 1;
        var tongThu = giaoDichs.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
        var tongChi = giaoDichs.Where(g => g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien);
        sheet.Cell(hang, 1).Value = "Tổng thu:";
        sheet.Cell(hang, 1).Style.Font.SetBold();
        sheet.Cell(hang, 5).Value = (double)tongThu;
        sheet.Cell(hang, 5).Style.NumberFormat.Format = "#,##0";
        hang++;
        sheet.Cell(hang, 1).Value = "Tổng chi:";
        sheet.Cell(hang, 1).Style.Font.SetBold();
        sheet.Cell(hang, 5).Value = (double)tongChi;
        sheet.Cell(hang, 5).Style.NumberFormat.Format = "#,##0";
        hang++;
        sheet.Cell(hang, 1).Value = "Số dư:";
        sheet.Cell(hang, 1).Style.Font.SetBold();
        sheet.Cell(hang, 5).Value = (double)(tongThu - tongChi);
        sheet.Cell(hang, 5).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(hang, 5).Style.Font.SetBold();

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] XuatPdf(List<Transaction> giaoDichs, string tenNguoiDung, DateTime tuNgay, DateTime denNgay)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var tongThu = giaoDichs.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
        var tongChi = giaoDichs.Where(g => g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("BÁO CÁO THU CHI CÁ NHÂN").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Người dùng: {tenNguoiDung}").FontSize(10);
                    col.Item().Text($"Khoảng thời gian: {tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}").FontSize(10);
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        IContainer CellStyle(IContainer c) => c.Background(Colors.Blue.Medium).Padding(5);
                        header.Cell().Element(c => CellStyle(c).Text("Ngày").FontColor(Colors.White).Bold());
                        header.Cell().Element(c => CellStyle(c).Text("Loại").FontColor(Colors.White).Bold());
                        header.Cell().Element(c => CellStyle(c).Text("Danh mục").FontColor(Colors.White).Bold());
                        header.Cell().Element(c => CellStyle(c).Text("Ghi chú").FontColor(Colors.White).Bold());
                        header.Cell().Element(c => CellStyle(c).AlignRight().Text("Số tiền").FontColor(Colors.White).Bold());
                    });

                    foreach (var gd in giaoDichs.OrderBy(g => g.NgayGiaoDich))
                    {
                        static IContainer CellStyle(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(5);

                        table.Cell().Element(c => CellStyle(c).Text(gd.NgayGiaoDich.ToString("dd/MM/yyyy")));
                        table.Cell().Element(c => CellStyle(c).Text(gd.Loai == LoaiGiaoDich.Thu ? "Thu" : "Chi")
                            .FontColor(gd.Loai == LoaiGiaoDich.Thu ? Colors.Green.Darken1 : Colors.Red.Darken1));
                        table.Cell().Element(c => CellStyle(c).Text(gd.Category?.TenDanhMuc ?? ""));
                        table.Cell().Element(c => CellStyle(c).Text(gd.GhiChu ?? ""));
                        table.Cell().Element(c => CellStyle(c).AlignRight().Text(
                            (gd.Loai == LoaiGiaoDich.Thu ? "+" : "-") + gd.SoTien.ToString("N0") + " đ"));
                    }
                });

                page.Footer().PaddingTop(10).Column(col =>
                {
                    col.Item().LineHorizontal(1);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Tổng thu: {tongThu:N0} đ").FontColor(Colors.Green.Darken1).Bold();
                        row.RelativeItem().AlignCenter().Text($"Tổng chi: {tongChi:N0} đ").FontColor(Colors.Red.Darken1).Bold();
                        row.RelativeItem().AlignRight().Text($"Số dư: {(tongThu - tongChi):N0} đ").Bold();
                    });
                    col.Item().PaddingTop(3).AlignCenter().Text(x =>
                    {
                        x.Span("Trang ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" / ").FontSize(8);
                        x.TotalPages().FontSize(8);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
