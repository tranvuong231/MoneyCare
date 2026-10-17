// Tuần 4: màn hình Dashboard tổng quan và Báo cáo (screens/dashboard_screen.dart, reports_screen.dart).
// Test ReportModel/ReportCategorySlice.fromJson - dữ liệu vẽ biểu đồ tròn thu/chi theo danh mục.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('ReportCategorySlice', () {
    test('fromJson đọc đúng 1 lát biểu đồ', () {
      final slice = ReportCategorySlice.fromJson({"ten": "Ăn uống", "soTien": 2000000, "mauSac": "#FF5733"});

      expect(slice.ten, "Ăn uống");
      expect(slice.soTien, 2000000.0);
      expect(slice.mauSac, "#FF5733");
    });

    test('fromJson dùng màu mặc định khi thiếu mauSac', () {
      final slice = ReportCategorySlice.fromJson({"ten": "Khác", "soTien": 0});

      expect(slice.mauSac, "#0d6efd");
    });
  });

  group('ReportModel', () {
    test('fromJson đọc đầy đủ báo cáo gồm cả 2 danh sách danh mục thu và chi', () {
      final report = ReportModel.fromJson({
        "tuNgay": "2026-08-01T00:00:00",
        "denNgay": "2026-08-31T00:00:00",
        "tongThu": 15000000,
        "tongChi": 8000000,
        "danhMucChi": [
          {"ten": "Ăn uống", "soTien": 5000000, "mauSac": "#FF5733"},
          {"ten": "Di chuyển", "soTien": 3000000, "mauSac": "#3357FF"},
        ],
        "danhMucThu": [
          {"ten": "Lương", "soTien": 15000000, "mauSac": "#33FF57"},
        ],
      });

      expect(report.tongThu, 15000000.0);
      expect(report.tongChi, 8000000.0);
      expect(report.danhMucChi, hasLength(2));
      expect(report.danhMucThu, hasLength(1));
      expect(report.danhMucChi.first.ten, "Ăn uống");
    });

    test('fromJson xử lý đúng khi danh sách danh mục rỗng (chưa có giao dịch trong kỳ)', () {
      final report = ReportModel.fromJson({
        "tuNgay": "2026-09-01T00:00:00",
        "denNgay": "2026-09-30T00:00:00",
        "tongThu": 0,
        "tongChi": 0,
        "danhMucChi": [],
        "danhMucThu": [],
      });

      expect(report.danhMucChi, isEmpty);
      expect(report.danhMucThu, isEmpty);
    });
  });
}
