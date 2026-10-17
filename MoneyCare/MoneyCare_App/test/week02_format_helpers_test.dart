// Tuần 2: đăng nhập/đăng ký (AuthProvider + JWT lưu bằng flutter_secure_storage), đăng nhập Google,
// và các hàm ĐỊNH DẠNG DÙNG CHUNG (widgets/common_widgets.dart) được viết ngay từ tuần này vì mọi màn
// hình sau đó (Giao dịch, Hóa đơn, Dashboard...) đều cần hiển thị tiền/ngày/màu theo đúng chuẩn Việt Nam.
// (AuthProvider/ApiClient phụ thuộc flutter_secure_storage - 1 plugin gọi platform channel thật - nên
// không unit-test trực tiếp ở đây; các hàm định dạng thuần Dart dưới đây mới là phần logic tách bạch
// được và cần bảo vệ bằng test tự động.)
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/widgets/common_widgets.dart';

void main() {
  group('tienVnd', () {
    test('Định dạng số tiền có phân cách hàng nghìn kiểu Việt Nam + ký hiệu ₫', () {
      expect(tienVnd(1000000), "1.000.000 ₫");
    });

    test('Định dạng đúng số tiền nhỏ hơn 1000', () {
      expect(tienVnd(500), "500 ₫");
    });

    test('Định dạng đúng số 0', () {
      expect(tienVnd(0), "0 ₫");
    });
  });

  group('ngayVn / gioVn / gioNgayVn', () {
    final moc = DateTime(2026, 8, 27, 14, 5);

    test('ngayVn trả về đúng định dạng dd/MM/yyyy', () {
      expect(ngayVn(moc), "27/08/2026");
    });

    test('gioVn trả về đúng định dạng HH:mm', () {
      expect(gioVn(moc), "14:05");
    });

    test('gioNgayVn kết hợp đúng cả giờ và ngày', () {
      expect(gioNgayVn(moc), "14:05-27/08/2026");
    });
  });

  group('mauTuHex', () {
    test('Chuyển đúng mã hex 6 ký tự (không có alpha) sang Color', () {
      expect(mauTuHex('#14B8A6'), const Color(0xFF14B8A6));
    });

    test('Vẫn hoạt động khi hex không có dấu #', () {
      expect(mauTuHex('16A34A'), const Color(0xFF16A34A));
    });

    test('Rơi về màu xanh dương mặc định khi chuỗi hex không hợp lệ', () {
      expect(mauTuHex('khong-phai-hex'), Colors.blue);
    });
  });
}
