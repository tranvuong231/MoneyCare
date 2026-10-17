import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/api_client.dart';
import '../services/services.dart';
import '../services/navigation_service.dart';

enum TrangThaiXacThuc { dangTai, daDangNhap, chuaDangNhap }

class AuthProvider extends ChangeNotifier {
  TrangThaiXacThuc trangThai = TrangThaiXacThuc.dangTai;
  UserModel? nguoiDung;

  AuthProvider() {
    ApiClient.onPhienHetHan = _xuLyHetPhien;
    _khoiTao();
  }

  // Gọi khi phát hiện lỗi 401 GIỮA LÚC đang dùng app (token còn lưu trên máy nhưng server không
  // còn chấp nhận) - khác với AuthProvider._khoiTao() vốn chỉ chạy 1 lần lúc mở app. Nếu không xử
  // lý việc này, người dùng sẽ bị kẹt ở màn hình cũ với thông báo lỗi lặp lại mỗi khi thao tác,
  // phải tự vào Cá nhân > Đăng xuất mới đăng nhập lại được.
  Future<void> _xuLyHetPhien() async {
    await ApiClient.xoaToken();
    nguoiDung = null;
    trangThai = TrangThaiXacThuc.chuaDangNhap;
    notifyListeners();
    // Bật hết mọi màn hình đang mở (VD đang ở giữa màn Thêm giao dịch) để lộ ra màn Đăng nhập
    // (lúc này đã trở thành "màn gốc" sau khi trangThai đổi, nhờ _CongXacThuc trong main.dart).
    navigatorKey.currentState?.popUntil((route) => route.isFirst);
  }

  Future<void> _khoiTao() async {
    final token = await ApiClient.layToken();
    if (token == null) {
      trangThai = TrangThaiXacThuc.chuaDangNhap;
      notifyListeners();
      return;
    }
    try {
      nguoiDung = await AuthService.layThongTinToi();
      trangThai = TrangThaiXacThuc.daDangNhap;
    } catch (_) {
      await ApiClient.xoaToken();
      trangThai = TrangThaiXacThuc.chuaDangNhap;
    }
    notifyListeners();
  }

  Future<void> dangNhap(String email, String matKhau) async {
    await AuthService.dangNhap(email, matKhau);
    nguoiDung = await AuthService.layThongTinToi();
    trangThai = TrangThaiXacThuc.daDangNhap;
    notifyListeners();
  }

  Future<void> dangKy(String hoTen, String email, String matKhau) async {
    await AuthService.dangKy(hoTen, email, matKhau);
    nguoiDung = await AuthService.layThongTinToi();
    trangThai = TrangThaiXacThuc.daDangNhap;
    notifyListeners();
  }

  Future<void> dangNhapGoogle(String idToken) async {
    await AuthService.dangNhapGoogle(idToken);
    nguoiDung = await AuthService.layThongTinToi();
    trangThai = TrangThaiXacThuc.daDangNhap;
    notifyListeners();
  }

  Future<void> lamMoiThongTinNguoiDung() async {
    try {
      nguoiDung = await AuthService.layThongTinToi();
      notifyListeners();
    } catch (_) {}
  }

  Future<void> dangXuat() async {
    await AuthService.dangXuat();
    nguoiDung = null;
    trangThai = TrangThaiXacThuc.chuaDangNhap;
    notifyListeners();
  }
}
