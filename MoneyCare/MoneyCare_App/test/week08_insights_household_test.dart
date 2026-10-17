// Tuần 8: màn hình Phân tích & Gợi ý AI (screens/insights_screen.dart) và Gia đình
// (screens/household_screen.dart). Test InsightsModel/InsightItem/DuDoanSoDu.fromJson và
// HouseholdModel/ThanhVienChiTieu.fromJson.
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('InsightItem & DuDoanSoDu', () {
    test('InsightItem.fromJson đọc đúng 1 nhận xét cảnh báo', () {
      final item = InsightItem.fromJson({
        "icon": "bi-graph-up-arrow",
        "mucDo": "warning",
        "tieuDe": "Chi tiêu Ăn uống tăng bất thường",
        "noiDung": "Tăng 45% so với tháng trước",
      });

      expect(item.mucDo, "warning");
      expect(item.tieuDe, "Chi tiêu Ăn uống tăng bất thường");
    });

    test('InsightItem.fromJson dùng giá trị mặc định khi thiếu trường', () {
      final item = InsightItem.fromJson({});

      expect(item.icon, "bi-lightbulb");
      expect(item.mucDo, "info");
      expect(item.tieuDe, "");
    });

    test('DuDoanSoDu.fromJson đọc đúng số liệu dự đoán số dư cuối tháng', () {
      final duDoan = DuDoanSoDu.fromJson({
        "soDuHienTai": 8000000,
        "chiTieuTrungBinhNgay": 200000,
        "thuNhapTrungBinhNgay": 0,
        "soNgayConLaiTrongThang": 5,
        "soDuDuDoanCuoiThang": 7000000,
        "xuHuong": "giam",
      });

      expect(duDoan.soDuHienTai, 8000000.0);
      expect(duDoan.xuHuong, "giam");
    });

    test('InsightsModel.fromJson gộp đúng cả 2 danh sách + dự đoán', () {
      final insights = InsightsModel.fromJson({
        "thoiQuenChiTieu": [
          {"tieuDe": "Danh mục chi nhiều nhất: Ăn uống", "noiDung": "Chiếm 40%"},
        ],
        "goiYTietKiem": [],
        "duDoan": {"soDuHienTai": 1000000, "xuHuong": "on_dinh"},
      });

      expect(insights.thoiQuenChiTieu, hasLength(1));
      expect(insights.goiYTietKiem, isEmpty);
      expect(insights.duDoan.xuHuong, "on_dinh");
    });
  });

  group('HouseholdModel & ThanhVienChiTieu', () {
    test('fromJson đọc đầy đủ thông tin hộ gia đình gồm chi tiêu từng thành viên', () {
      final household = HouseholdModel.fromJson({
        "id": 1,
        "tenHoGiaDinh": "Gia đình Vương",
        "maMoi": "ABC123",
        "tenChuHo": "Trần Quốc Vương",
        "laChuHo": true,
        "tongThuGiaDinh": 20000000,
        "tongChiGiaDinh": 12000000,
        "giaoDichGanDay": [],
        "thanhVienChiTieu": [
          {"tenThanhVien": "Trần Quốc Vương", "tongThu": 15000000, "tongChi": 8000000},
          {"tenThanhVien": "Nguyễn Anh Vũ", "tongThu": 5000000, "tongChi": 4000000},
        ],
      });

      expect(household.tenHoGiaDinh, "Gia đình Vương");
      expect(household.laChuHo, true);
      expect(household.thanhVienChiTieu, hasLength(2));
      expect(household.thanhVienChiTieu.first.tenThanhVien, "Trần Quốc Vương");
    });

    test('laChuHo mặc định false khi server không trả về (thành viên thường)', () {
      final household = HouseholdModel.fromJson({
        "id": 2,
        "tenHoGiaDinh": "Gia đình khác",
        "maMoi": "XYZ789",
        "tongThuGiaDinh": 0,
        "tongChiGiaDinh": 0,
        "giaoDichGanDay": [],
        "thanhVienChiTieu": [],
      });

      expect(household.laChuHo, false);
    });
  });
}
