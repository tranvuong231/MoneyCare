// Tuần 10: hoàn thiện Trang cá nhân/Cài đặt và khu vực Admin (screens/admin_screens.dart) - trang
// quản trị dành cho vai trò Admin: thống kê tổng quan, quản lý người dùng/hộ gia đình/đơn Premium/tin
// nhắn liên hệ. Cũng là tuần chạy lại toàn bộ 10 file test (Week01..Week10) trước khi bàn giao.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('AdminDashboardModel', () {
    test('fromJson đọc đúng số liệu tổng quan + 2 dãy dữ liệu biểu đồ 6 tháng', () {
      final dashboard = AdminDashboardModel.fromJson({
        "tongNguoiDung": 1200,
        "tongNguoiDungPremium": 340,
        "nguoiDungMoiThangNay": 58,
        "tongGiaoDich": 45210,
        "tongHoGiaDinh": 90,
        "tongDoanhThu": 9860000,
        "donChoXacNhan": 4,
        "nhanThang": ["Th3/26", "Th4/26", "Th5/26", "Th6/26", "Th7/26", "Th8/26"],
        "doanhThuTheoThang": [1000000, 1200000, 1500000, 1800000, 2000000, 2360000],
        "nguoiDungMoiTheoThang": [30, 35, 40, 45, 50, 58],
      });

      expect(dashboard.tongNguoiDung, 1200);
      expect(dashboard.nhanThang, hasLength(6));
      expect(dashboard.doanhThuTheoThang, hasLength(6));
      expect(dashboard.donChoXacNhan, 4);
    });
  });

  group('AdminUserModel & AdminUserDetailModel', () {
    test('AdminUserModel.fromJson đọc đúng thông tin rút gọn 1 người dùng', () {
      final user = AdminUserModel.fromJson({
        "id": "user-001",
        "hoTen": "Trần Quốc Vương",
        "email": "vuong@moneycare.vn",
        "ngayTao": "2026-07-20T00:00:00",
        "isPremium": true,
        "premiumConHieuLuc": true,
        "laAdmin": false,
        "biKhoa": false,
      });

      expect(user.hoTen, "Trần Quốc Vương");
      expect(user.biKhoa, false);
    });

    test('AdminUserDetailModel kế thừa đúng các trường của AdminUserModel và bổ sung thống kê chi tiết', () {
      final chiTiet = AdminUserDetailModel.fromJson({
        "id": "user-001",
        "hoTen": "Trần Quốc Vương",
        "email": "vuong@moneycare.vn",
        "ngayTao": "2026-07-20T00:00:00",
        "isPremium": true,
        "premiumConHieuLuc": true,
        "laAdmin": false,
        "biKhoa": false,
        "tongThu": 50000000,
        "tongChi": 30000000,
        "soGiaoDich": 120,
        "soDonPremium": 2,
        "premiumHetHan": "2026-12-31T00:00:00",
      });

      // Là AdminUserModel (kế thừa) NÊN vẫn dùng được ở bất kỳ đâu nhận AdminUserModel.
      expect(chiTiet, isA<AdminUserModel>());
      expect(chiTiet.hoTen, "Trần Quốc Vương");
      expect(chiTiet.tongThu, 50000000.0);
      expect(chiTiet.soGiaoDich, 120);
    });
  });

  group('AdminHouseholdModel, AdminPremiumOrderModel, AdminContactMessageModel', () {
    test('AdminHouseholdModel.fromJson đọc đúng thông tin hộ gia đình cho trang quản trị', () {
      final ho = AdminHouseholdModel.fromJson({
        "id": 1,
        "tenHoGiaDinh": "Gia đình Vương",
        "maMoi": "ABC123",
        "tenChuHo": "Trần Quốc Vương",
        "soThanhVien": 3,
        "ngayTao": "2026-08-01T00:00:00",
      });

      expect(ho.soThanhVien, 3);
      expect(ho.tenChuHo, "Trần Quốc Vương");
    });

    test('AdminPremiumOrderModel.fromJson đọc đúng đơn Premium đã xác nhận', () {
      final don = AdminPremiumOrderModel.fromJson({
        "id": 1,
        "maGiaoDich": "PREMIUMABC123",
        "soTien": 29000,
        "soThangDangKy": 1,
        "daXacNhan": true,
        "ngayTao": "2026-08-20T00:00:00",
        "emailNguoiDung": "vuong@moneycare.vn",
      });

      expect(don.daXacNhan, true);
      expect(don.emailNguoiDung, "vuong@moneycare.vn");
    });

    test('AdminContactMessageModel.fromJson đọc đúng tin nhắn liên hệ chưa xem', () {
      final tinNhan = AdminContactMessageModel.fromJson({
        "id": 1,
        "hoTen": "Nguyễn Văn A",
        "email": "a@gmail.com",
        "noiDung": "Ứng dụng rất hữu ích, mong bổ sung thêm tính năng ngân sách!",
        "ngayGui": "2026-08-25T00:00:00",
        "daXem": false,
      });

      expect(tinNhan.daXem, false);
      expect(tinNhan.noiDung, contains("ngân sách"));
    });
  });
}
