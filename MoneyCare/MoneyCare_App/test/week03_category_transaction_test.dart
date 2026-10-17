// Tuần 3: màn hình Danh mục và Giao dịch (screens/categories_screens.dart, transactions_screens.dart).
// Test CategoryModel/TransactionModel fromJson (nhận dữ liệu từ API) và toJson (gửi lên API khi
// thêm/sửa) - phải khớp CHÍNH XÁC tên trường và kiểu dữ liệu với DTO thật bên MoneyCare.Api.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('CategoryModel', () {
    test('fromJson đọc đúng danh mục Chi', () {
      final c = CategoryModel.fromJson({"id": 1, "tenDanhMuc": "Ăn uống", "loai": 1, "mauSac": "#FF5733"});

      expect(c.id, 1);
      expect(c.tenDanhMuc, "Ăn uống");
      expect(c.loai, LoaiGiaoDich.chi);
      expect(c.mauSac, "#FF5733");
    });

    test('fromJson dùng màu mặc định khi server không trả mauSac', () {
      final c = CategoryModel.fromJson({"id": 2, "tenDanhMuc": "Lương", "loai": 0});

      expect(c.mauSac, "#0d6efd");
    });

    test('toJson KHÔNG gửi id lên server (id do server tự sinh khi tạo mới)', () {
      final c = CategoryModel(id: 1, tenDanhMuc: "Di chuyển", loai: LoaiGiaoDich.chi, mauSac: "#000000");

      final json = c.toJson();

      expect(json.containsKey('id'), false);
      expect(json['tenDanhMuc'], "Di chuyển");
      expect(json['loai'], 1);
    });
  });

  group('TransactionModel', () {
    test('fromJson đọc đầy đủ 1 giao dịch chi từ API', () {
      final t = TransactionModel.fromJson({
        "id": 10,
        "soTien": 150000,
        "loai": 1,
        "ngayGiaoDich": "2026-08-20T00:00:00",
        "ghiChu": "Ăn trưa",
        "categoryId": 3,
        "tenDanhMuc": "Ăn uống",
        "mauDanhMuc": "#FF5733",
        "chiaSeGiaDinh": false,
      });

      expect(t.id, 10);
      expect(t.soTien, 150000.0);
      expect(t.loai, LoaiGiaoDich.chi);
      expect(t.ngayGiaoDich, DateTime.parse("2026-08-20T00:00:00"));
      expect(t.tenDanhMuc, "Ăn uống");
    });

    test('fromJson chuyển đúng số nguyên (int) từ JSON thành double', () {
      // API trả "soTien": 100000 (không có phần thập phân) -> vẫn phải parse được thành double,
      // không được crash vì ép kiểu int -> double sai cách.
      final t = TransactionModel.fromJson({
        "id": 11,
        "soTien": 100000,
        "loai": 0,
        "ngayGiaoDich": "2026-08-01T00:00:00",
        "categoryId": 1,
      });

      expect(t.soTien, isA<double>());
      expect(t.soTien, 100000.0);
    });

    test('toJson gửi đúng định dạng ISO8601 cho ngày giao dịch và giá trị số cho loại', () {
      final t = TransactionModel(
        id: 1,
        soTien: 50000,
        loai: LoaiGiaoDich.chi,
        ngayGiaoDich: DateTime(2026, 8, 27),
        categoryId: 2,
        ghiChu: "Đổ xăng",
      );

      final json = t.toJson();

      expect(json['ngayGiaoDich'], DateTime(2026, 8, 27).toIso8601String());
      expect(json['loai'], 1);
      expect(json['ghiChu'], "Đổ xăng");
      expect(json.containsKey('id'), false);
    });
  });
}
