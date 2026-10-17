// Tuần 1: khởi tạo project Flutter, thiết kế các Model dữ liệu cơ bản (khớp 1-1 với DTO backend)
// và cấu hình flutter_test. Test dưới đây kiểm tra các enum dùng chung (LoaiGiaoDich, GioiTinh,
// TanSuatBaoCao, NhaCungCap) và UserModel.fromJson - model được dùng ngay từ màn hình đăng nhập đầu tiên.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('Enum dùng chung', () {
    test('LoaiGiaoDich: value và fromValue đi khứ hồi đúng', () {
      expect(LoaiGiaoDich.thu.value, 0);
      expect(LoaiGiaoDich.chi.value, 1);
      expect(LoaiGiaoDichX.fromValue(0), LoaiGiaoDich.thu);
      expect(LoaiGiaoDichX.fromValue(1), LoaiGiaoDich.chi);
      expect(LoaiGiaoDich.thu.nhan, "Thu");
      expect(LoaiGiaoDich.chi.nhan, "Chi");
    });

    test('GioiTinh: value và fromValue đi khứ hồi đúng', () {
      for (final g in GioiTinh.values) {
        expect(GioiTinhX.fromValue(g.value), g);
      }
      expect(GioiTinh.nam.nhan, "Nam");
      expect(GioiTinh.nu.nhan, "Nữ");
      expect(GioiTinh.khac.nhan, "Khác");
    });

    test('NhaCungCap: value và fromValue đi khứ hồi đúng', () {
      for (final n in NhaCungCap.values) {
        expect(NhaCungCapX.fromValue(n.value), n);
      }
      expect(NhaCungCap.nganHang.nhan, "Ngân hàng");
      expect(NhaCungCap.moMo.nhan, "MoMo");
      expect(NhaCungCap.zaloPay.nhan, "ZaloPay");
    });

    test('TanSuatBaoCao: value và fromValue đi khứ hồi đúng', () {
      expect(TanSuatBaoCaoX.fromValue(0), TanSuatBaoCao.hangNgay);
      expect(TanSuatBaoCaoX.fromValue(1), TanSuatBaoCao.hangTuan);
    });
  });

  group('UserModel.fromJson', () {
    test('Đọc đầy đủ thông tin cơ bản từ JSON server trả về', () {
      final user = UserModel.fromJson({
        "id": "user-001",
        "hoTen": "Trần Quốc Vương",
        "email": "vuong@moneycare.vn",
        "phoneNumber": "0912345678",
        "daXacMinhDanhTinh": true,
        "emailConfirmed": true,
        "isPremium": true,
        "premiumConHieuLuc": true,
        "premiumHetHan": "2026-12-31T00:00:00",
        "householdId": 5,
        "laAdmin": false,
      });

      expect(user.id, "user-001");
      expect(user.hoTen, "Trần Quốc Vương");
      expect(user.daXacMinhDanhTinh, true);
      expect(user.premiumHetHan, DateTime.parse("2026-12-31T00:00:00"));
      expect(user.householdId, 5);
    });

    test('Áp dụng đúng giá trị mặc định khi server không trả về trường tùy chọn', () {
      final user = UserModel.fromJson({"id": "user-002"});

      expect(user.hoTen, isNull);
      expect(user.daXacMinhDanhTinh, false);
      expect(user.isPremium, false);
      expect(user.premiumHetHan, isNull);
      expect(user.tanSuatBaoCao, TanSuatBaoCao.hangTuan);
      expect(user.gioBaoCao, "08:00");
    });

    test('Phân giải đúng giới tính từ mã số nguyên', () {
      final user = UserModel.fromJson({"id": "user-003", "gioiTinh": 1});

      expect(user.gioiTinh, GioiTinh.nu);
    });
  });
}
