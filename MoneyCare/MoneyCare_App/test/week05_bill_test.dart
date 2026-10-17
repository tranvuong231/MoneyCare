// Tuần 5: xuất báo cáo Excel/PDF (tải file trực tiếp bằng bytes trả về từ API, không có model riêng)
// và màn hình Hóa đơn (screens/bills_screens.dart). Test BillModel.fromJson/toJson.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('BillModel', () {
    test('fromJson đọc đầy đủ 1 hóa đơn sắp đến hạn', () {
      final bill = BillModel.fromJson({
        "id": 1,
        "tenHoaDon": "Tiền điện",
        "soTien": 350000,
        "ngayDenHan": "2026-09-01T00:00:00",
        "daThanhToan": false,
        "ghiChu": "Kỳ tháng 8",
        "soNgayConLai": 5,
      });

      expect(bill.tenHoaDon, "Tiền điện");
      expect(bill.soTien, 350000.0);
      expect(bill.daThanhToan, false);
      expect(bill.soNgayConLai, 5);
    });

    test('fromJson mặc định soNgayConLai = 0 và daThanhToan = false khi server không trả về', () {
      final bill = BillModel.fromJson({
        "id": 2,
        "tenHoaDon": "Internet",
        "soTien": 220000,
        "ngayDenHan": "2026-09-10T00:00:00",
      });

      expect(bill.daThanhToan, false);
      expect(bill.soNgayConLai, 0);
    });

    test('soNgayConLai ÂM đúng nghĩa là đã quá hạn (dữ liệu do server tính sẵn)', () {
      final bill = BillModel.fromJson({
        "id": 3,
        "tenHoaDon": "Đã quá hạn",
        "soTien": 100000,
        "ngayDenHan": "2026-08-20T00:00:00",
        "soNgayConLai": -7,
      });

      expect(bill.soNgayConLai, lessThan(0));
    });

    test('toJson KHÔNG gửi soNgayConLai lên server (trường chỉ tính toán phía server)', () {
      final bill = BillModel(
        id: 1,
        tenHoaDon: "Tiền nước",
        soTien: 150000,
        ngayDenHan: DateTime(2026, 9, 5),
        daThanhToan: false,
        soNgayConLai: 9,
      );

      final json = bill.toJson();

      expect(json.containsKey('soNgayConLai'), false);
      expect(json.containsKey('id'), false);
      expect(json['tenHoaDon'], "Tiền nước");
    });
  });
}
