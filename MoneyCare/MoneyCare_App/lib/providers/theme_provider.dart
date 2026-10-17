import 'package:flutter/material.dart';

// Trước đây app có LƯU cài đặt "Giao diện tối" xuống server, nhưng chưa bao giờ
// thực sự ÁP DỤNG nó lên giao diện - đây là provider quản lý việc đó.
class ThemeProvider extends ChangeNotifier {
  ThemeMode _themeMode = ThemeMode.light;
  ThemeMode get themeMode => _themeMode;

  void dat(bool giaoDienToi) {
    final moi = giaoDienToi ? ThemeMode.dark : ThemeMode.light;
    if (moi != _themeMode) {
      _themeMode = moi;
      notifyListeners();
    }
  }
}
