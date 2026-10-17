// ============================================================================
// LỚP GỌI API DÙNG CHUNG CHO TOÀN BỘ APP
// ============================================================================
// Mọi màn hình đều gọi qua ApiClient này thay vì gọi `http` trực tiếp, để:
//   - Tự động đính kèm JWT token vào header Authorization
//   - Tự động parse lỗi trả về từ backend (field "loi") thành ApiException
//   - Có 1 chỗ duy nhất để đổi cách gọi mạng nếu cần sau này
// ============================================================================
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../config/api_config.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  ApiException(this.message, [this.statusCode]);

  @override
  String toString() => message;
}

class ApiClient {
  static const _storage = FlutterSecureStorage();
  static const _tokenKey = 'moneycare_jwt_token';

  // Được gán 1 lần ở main.dart (AuthProvider) - gọi mỗi khi phát hiện phiên đăng nhập đã hết hạn
  // (lỗi 401) TRONG LÚC đang dùng app (khác với lúc mở app lần đầu, đã được xử lý riêng ở
  // AuthProvider._khoiTao). Nhờ vậy người dùng được tự động đưa thẳng về màn Đăng nhập thay vì
  // phải tự vào Cá nhân > Đăng xuất mới đăng nhập lại được.
  static void Function()? onPhienHetHan;
  static bool _dangXuLyHetPhien = false;

  static Future<void> luuToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  static Future<String?> layToken() async {
    return await _storage.read(key: _tokenKey);
  }

  static Future<void> xoaToken() async {
    await _storage.delete(key: _tokenKey);
  }

  static Future<Map<String, String>> _headers({bool json = true}) async {
    final token = await layToken();
    return {
      if (json) 'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  static dynamic _xuLyPhanHoi(http.Response res) {
    if (res.statusCode >= 200 && res.statusCode < 300) {
      if (res.body.isEmpty) return null;
      return jsonDecode(utf8.decode(res.bodyBytes));
    }

    String? thongBaoTuServer;
    try {
      final body = jsonDecode(utf8.decode(res.bodyBytes));
      if (body is Map && body['loi'] != null) {
        thongBaoTuServer = body['loi'].toString();
      } else if (body is Map && body['errors'] != null && body['errors'] is Map) {
        // Lỗi validate tự động của ASP.NET Core (dạng { "errors": { "TenTruong": ["..."] } })
        final errors = (body['errors'] as Map).values.expand((v) => (v as List)).map((e) => e.toString());
        if (errors.isNotEmpty) thongBaoTuServer = errors.join('; ');
      } else if (body is Map && body['title'] != null) {
        thongBaoTuServer = body['title'].toString();
      }
    } catch (_) {}

    // CHỈ dùng thông báo chung chung khi server không trả về lý do cụ thể nào -
    // tránh che mất lỗi thật (VD: lỗi xác thực Google, lỗi validate cụ thể...).
    String thongBaoLoi = thongBaoTuServer ?? "Đã có lỗi xảy ra (mã ${res.statusCode})";
    if (thongBaoTuServer == null) {
      if (res.statusCode == 401) thongBaoLoi = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại";
      if (res.statusCode == 402) thongBaoLoi = "Tính năng này chỉ dành cho tài khoản Premium";
    }

    if (res.statusCode == 401 && !_dangXuLyHetPhien) {
      _dangXuLyHetPhien = true;
      onPhienHetHan?.call();
      // Cho phép gọi lại lần sau (VD người dùng đăng nhập lại rồi lại hết phiên lần nữa)
      Future.delayed(const Duration(seconds: 2), () => _dangXuLyHetPhien = false);
    }

    throw ApiException(thongBaoLoi, res.statusCode);
  }

  static Future<dynamic> get(String path) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final res = await http.get(uri, headers: await _headers());
    return _xuLyPhanHoi(res);
  }

  static Future<dynamic> post(String path, [Object? body]) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final res = await http.post(uri, headers: await _headers(), body: body != null ? jsonEncode(body) : null);
    return _xuLyPhanHoi(res);
  }

  static Future<dynamic> put(String path, [Object? body]) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final res = await http.put(uri, headers: await _headers(), body: body != null ? jsonEncode(body) : null);
    return _xuLyPhanHoi(res);
  }

  static Future<dynamic> delete(String path) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final res = await http.delete(uri, headers: await _headers());
    return _xuLyPhanHoi(res);
  }

  // Tải file nhị phân (ảnh QR, file Excel/PDF...) kèm xác thực + báo lỗi rõ ràng
  // (dùng thay cho Image.network/CachedNetworkImage để không bị "nuốt" lỗi thật từ server).
  static Future<List<int>> getBytes(String path) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final res = await http.get(uri, headers: await _headers(json: false));
    if (res.statusCode >= 200 && res.statusCode < 300) {
      return res.bodyBytes;
    }
    // Lỗi trả về dạng JSON dù đang gọi API ảnh/file -> parse như bình thường để lấy thông báo
    _xuLyPhanHoi(res);
    throw ApiException("Không tải được dữ liệu");
  }

  // Gửi file (ảnh hóa đơn OCR, ảnh đại diện, ảnh nền...) dạng multipart/form-data
  static Future<dynamic> uploadFile(String path, String fieldName, File file, {Map<String, String>? extraFields}) async {
    final uri = Uri.parse(ApiConfig.api(path));
    final request = http.MultipartRequest('POST', uri);
    final headers = await _headers(json: false);
    request.headers.addAll(headers);
    request.files.add(await http.MultipartFile.fromPath(fieldName, file.path));
    if (extraFields != null) request.fields.addAll(extraFields);

    final streamed = await request.send();
    final res = await http.Response.fromStream(streamed);
    return _xuLyPhanHoi(res);
  }

  // Trả về URL đầy đủ kèm token dạng query - dùng để hiển thị ảnh (Image.network)
  // cho các endpoint trả file trực tiếp (VD ảnh QR thanh toán).
  static String urlAnhCoThamSo(String path) => ApiConfig.api(path);
}
