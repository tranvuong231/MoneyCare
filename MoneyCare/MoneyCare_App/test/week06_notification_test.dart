// Tuần 6: chuông thông báo trong app (screens/notifications_screen.dart) và quét hóa đơn OCR
// (screens/ocr_screen.dart - kết quả OCR/AI được xử lý hoàn toàn phía server rồi trả về dạng Map
// động, không có model Dart riêng nên không nằm trong phạm vi test tuần này).
// Test ThongBaoModel.fromJson + LoaiThongBaoX.fromValue.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('LoaiThongBaoX', () {
    test('fromValue ánh xạ đúng thứ tự enum với backend (0=hoaDon .. 7=heThong)', () {
      expect(LoaiThongBaoX.fromValue(0), LoaiThongBao.hoaDon);
      expect(LoaiThongBaoX.fromValue(1), LoaiThongBao.phanTichAI);
      expect(LoaiThongBaoX.fromValue(6), LoaiThongBao.premium);
      expect(LoaiThongBaoX.fromValue(7), LoaiThongBao.heThong);
    });
  });

  group('ThongBaoModel', () {
    test('fromJson đọc đầy đủ 1 thông báo chưa đọc', () {
      final tb = ThongBaoModel.fromJson({
        "id": 1,
        "tieuDe": "Nhắc hóa đơn: Tiền điện",
        "noiDung": "Hóa đơn sắp đến hạn thanh toán trong 3 ngày",
        "loai": 0,
        "duongDanDieuHuong": "/bills",
        "daXem": false,
        "ngayTao": "2026-08-27T09:00:00",
      });

      expect(tb.tieuDe, "Nhắc hóa đơn: Tiền điện");
      expect(tb.loai, LoaiThongBao.hoaDon);
      expect(tb.daXem, false);
      expect(tb.duongDanDieuHuong, "/bills");
    });

    test('fromJson mặc định loai = heThong (7) khi server không trả trường loai', () {
      final tb = ThongBaoModel.fromJson({
        "id": 2,
        "tieuDe": "Thông báo hệ thống",
        "noiDung": "Bảo trì hệ thống lúc 23h hôm nay",
        "daXem": true,
        "ngayTao": "2026-08-27T08:00:00",
      });

      expect(tb.loai, LoaiThongBao.heThong);
    });

    test('fromJson mặc định daXem = false khi chưa từng đọc', () {
      final tb = ThongBaoModel.fromJson({
        "id": 3,
        "tieuDe": "Chào mừng",
        "noiDung": "Chào mừng bạn đến với MoneyCare!",
        "loai": 4,
        "ngayTao": "2026-08-27T07:00:00",
      });

      expect(tb.daXem, false);
    });
  });
}
