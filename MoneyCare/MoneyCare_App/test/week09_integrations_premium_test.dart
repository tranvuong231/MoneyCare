// Tuần 9: khung tích hợp Ngân hàng/MoMo/ZaloPay (screens/integrations_screen.dart) và nâng cấp
// Premium qua ZaloPay UAT (screens/premium_screen.dart). Test FinancialLinkModel, PremiumPricing/
// PremiumOrderModel, và 2 model ZaloPay (đơn hàng + trạng thái thanh toán).
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('FinancialLinkModel', () {
    test('fromJson đọc đúng liên kết đã che số tài khoản', () {
      final link = FinancialLinkModel.fromJson({
        "id": 1,
        "nhaCungCap": 0,
        "tenGoiNho": "TK chính",
        "soTaiKhoanAn": "**** 4567",
        "daKetNoi": true,
        "ngayKetNoi": "2026-08-20T00:00:00",
        "dongBoLanCuoi": "2026-08-26T00:00:00",
      });

      expect(link.nhaCungCap, NhaCungCap.nganHang);
      expect(link.soTaiKhoanAn, "**** 4567");
      expect(link.daKetNoi, true);
    });
  });

  group('PremiumPricing & PremiumOrderModel', () {
    test('PremiumPricing.fromJson đọc đúng bảng giá hiện tại', () {
      final gia = PremiumPricing.fromJson({
        "giaThang": 29000,
        "soTaiKhoan": "0071001234567",
        "binNganHang": "970436",
        "tenNganHangHienThi": "Vietcombank",
      });

      expect(gia.giaThang, 29000.0);
      expect(gia.tenNganHangHienThi, "Vietcombank");
    });

    test('PremiumOrderModel.fromJson đọc đúng đơn nâng cấp 3 tháng chưa xác nhận', () {
      final don = PremiumOrderModel.fromJson({
        "id": 1,
        "maGiaoDich": "PREMIUMABC123",
        "soTien": 87000,
        "soThangDangKy": 3,
        "daXacNhan": false,
        "ngayTao": "2026-08-27T10:00:00",
        "soTaiKhoan": "0071001234567",
        "binNganHang": "970436",
        "tenNganHangHienThi": "Vietcombank",
      });

      expect(don.soTien, 87000.0);
      expect(don.soThangDangKy, 3);
      expect(don.daXacNhan, false);
    });
  });

  group('ZaloPay', () {
    test('ZaloPayDonHangModel.fromJson đọc đúng đơn nâng cấp Premium qua ZaloPay', () {
      final don = ZaloPayDonHangModel.fromJson({
        "appTransId": "260827_0000001",
        "soTien": 29000,
        "moTa": "Nâng cấp Premium 1 tháng",
        "urlAnhQr": "/api/zalopay/qr/260827_0000001",
        "orderUrl": "https://sbgateway.zalopay.vn/openinapp?order=xxx",
        "premiumOrderId": 5,
        "soThangDangKy": 1,
      });

      expect(don.appTransId, "260827_0000001");
      expect(don.premiumOrderId, 5);
    });

    test('ZaloPayTrangThaiModel.fromJson ánh xạ đúng chuỗi trạng thái từ server', () {
      final thanhCong = ZaloPayTrangThaiModel.fromJson({
        "appTransId": "260827_0000001",
        "trangThai": "ThanhCong",
        "soTien": 29000,
        "isPremium": true,
        "premiumHetHan": "2026-09-27T00:00:00",
      });
      final choThanhToan = ZaloPayTrangThaiModel.fromJson({
        "appTransId": "260827_0000002",
        "trangThai": "DieuGiKhac",
        "soTien": 29000,
        "isPremium": false,
      });

      expect(thanhCong.trangThai, ZaloPayTrangThai.thanhCong);
      expect(thanhCong.isPremium, true);
      // Bất kỳ chuỗi lạ nào không khớp "ThanhCong"/"ThatBai" đều PHẢI rơi về "chờ thanh toán"
      // (an toàn hơn là mặc định coi như thành công khi gặp giá trị không nhận diện được).
      expect(choThanhToan.trangThai, ZaloPayTrangThai.choThanhToan);
    });
  });
}
