// ============================================================================
// CÁC SERVICE GỌI API - MỖI SERVICE TƯƠNG ỨNG 1 NHÓM CHỨC NĂNG BACKEND
// ============================================================================
import 'dart:io';
import '../models/models.dart';
import 'api_client.dart';

class AuthService {
  static Future<Map<String, dynamic>> dangKy(String hoTen, String email, String matKhau) async {
    final res = await ApiClient.post('/auth/dang-ky', {
      "hoTen": hoTen,
      "email": email,
      "matKhau": matKhau,
    });
    await ApiClient.luuToken(res['token']);
    return res;
  }

  static Future<Map<String, dynamic>> dangNhap(String email, String matKhau) async {
    final res = await ApiClient.post('/auth/dang-nhap', {
      "email": email,
      "matKhau": matKhau,
    });
    await ApiClient.luuToken(res['token']);
    return res;
  }

  static Future<Map<String, dynamic>> dangNhapGoogle(String idToken) async {
    final res = await ApiClient.post('/auth/google', {"idToken": idToken});
    await ApiClient.luuToken(res['token']);
    return res;
  }

  static Future<UserModel> layThongTinToi() async {
    final res = await ApiClient.get('/auth/toi');
    return UserModel.fromJson(res);
  }

  static Future<Map<String, dynamic>> quenMatKhau(String email) async {
    return await ApiClient.post('/auth/quen-mat-khau', {"email": email});
  }

  static Future<void> datLaiMatKhau(String email, String ma, String matKhauMoi) async {
    await ApiClient.post('/auth/dat-lai-mat-khau', {
      "email": email,
      "ma": ma,
      "matKhauMoi": matKhauMoi,
    });
  }

  static Future<void> dangXuat() async => ApiClient.xoaToken();
}

class CategoryService {
  static Future<List<CategoryModel>> danhSach({LoaiGiaoDich? loai}) async {
    final query = loai != null ? '?loai=${loai.value}' : '';
    final res = await ApiClient.get('/categories$query');
    return (res as List).map((e) => CategoryModel.fromJson(e)).toList();
  }

  static Future<CategoryModel> tao(CategoryModel c) async {
    final res = await ApiClient.post('/categories', c.toJson());
    return CategoryModel.fromJson(res);
  }

  static Future<CategoryModel> sua(int id, CategoryModel c) async {
    final res = await ApiClient.put('/categories/$id', c.toJson());
    return CategoryModel.fromJson(res);
  }

  static Future<void> xoa(int id) async => ApiClient.delete('/categories/$id');
}

class TransactionService {
  static Future<Map<String, dynamic>> danhSach({
    LoaiGiaoDich? loai,
    int? categoryId,
    DateTime? tuNgay,
    DateTime? denNgay,
  }) async {
    final params = <String, String>{};
    if (loai != null) params['loai'] = loai.value.toString();
    if (categoryId != null) params['categoryId'] = categoryId.toString();
    if (tuNgay != null) params['tuNgay'] = tuNgay.toIso8601String();
    if (denNgay != null) params['denNgay'] = denNgay.toIso8601String();
    final query = params.isEmpty ? '' : '?${Uri(queryParameters: params).query}';

    final res = await ApiClient.get('/transactions$query');
    return {
      "danhSach": (res['danhSach'] as List).map((e) => TransactionModel.fromJson(e)).toList(),
      "tongThu": (res['tongThu'] as num).toDouble(),
      "tongChi": (res['tongChi'] as num).toDouble(),
    };
  }

  static Future<TransactionModel> tao(TransactionModel t) async {
    final res = await ApiClient.post('/transactions', t.toJson());
    return TransactionModel.fromJson(res);
  }

  static Future<TransactionModel> sua(int id, TransactionModel t) async {
    final res = await ApiClient.put('/transactions/$id', t.toJson());
    return TransactionModel.fromJson(res);
  }

  static Future<void> xoa(int id) async => ApiClient.delete('/transactions/$id');
}

class BillService {
  static Future<List<BillModel>> danhSach({String? trangThai}) async {
    final query = trangThai != null ? '?trangThai=$trangThai' : '';
    final res = await ApiClient.get('/bills$query');
    return (res as List).map((e) => BillModel.fromJson(e)).toList();
  }

  static Future<BillModel> tao(BillModel b) async {
    final res = await ApiClient.post('/bills', b.toJson());
    return BillModel.fromJson(res);
  }

  static Future<BillModel> sua(int id, BillModel b) async {
    final res = await ApiClient.put('/bills/$id', b.toJson());
    return BillModel.fromJson(res);
  }

  static Future<BillModel> danhDauThanhToan(int id) async {
    final res = await ApiClient.post('/bills/$id/danh-dau-thanh-toan');
    return BillModel.fromJson(res);
  }

  static Future<void> xoa(int id) async => ApiClient.delete('/bills/$id');
}

class ReportService {
  static Future<ReportModel> layBaoCao({DateTime? tuNgay, DateTime? denNgay}) async {
    final params = <String, String>{};
    if (tuNgay != null) params['tuNgay'] = tuNgay.toIso8601String();
    if (denNgay != null) params['denNgay'] = denNgay.toIso8601String();
    final query = params.isEmpty ? '' : '?${Uri(queryParameters: params).query}';
    final res = await ApiClient.get('/reports$query');
    return ReportModel.fromJson(res);
  }
}

class InsightsService {
  static Future<InsightsModel> phanTich() async {
    final res = await ApiClient.get('/insights');
    return InsightsModel.fromJson(res);
  }
}

class ChatbotService {
  static Future<List<CauHoiMau>> goiY() async {
    final res = await ApiClient.get('/chatbot/goi-y');
    return (res as List).map((e) => CauHoiMau.fromJson(e)).toList();
  }

  static Future<String> hoi(String cauHoi) async {
    final res = await ApiClient.post('/chatbot/hoi', {"cauHoi": cauHoi});
    return res['traLoi'];
  }
}

class HouseholdService {
  static Future<HouseholdModel?> thongTin() async {
    final res = await ApiClient.get('/household');
    if (res == null) return null;
    return HouseholdModel.fromJson(res);
  }

  static Future<String> tao(String ten) async {
    final res = await ApiClient.post('/household/tao', ten);
    return res['thongBao'];
  }

  static Future<String> thamGia(String maMoi) async {
    final res = await ApiClient.post('/household/tham-gia', maMoi);
    return res['thongBao'];
  }

  static Future<void> roiHo() async => ApiClient.post('/household/roi-ho');

  static Future<void> xoaHoGiaDinh() async => ApiClient.delete('/household');
}

class IntegrationService {
  static Future<List<FinancialLinkModel>> danhSach() async {
    final res = await ApiClient.get('/integrations');
    return (res as List).map((e) => FinancialLinkModel.fromJson(e)).toList();
  }

  static Future<void> tao(NhaCungCap nhaCungCap, String tenGoiNho, String soTaiKhoan, String? ghiChu) async {
    await ApiClient.post('/integrations', {
      "nhaCungCap": nhaCungCap.value,
      "tenGoiNho": tenGoiNho,
      "soTaiKhoanDayDu": soTaiKhoan,
      "ghiChu": ghiChu,
    });
  }

  static Future<String> dongBo(int id) async {
    final res = await ApiClient.post('/integrations/$id/dong-bo');
    return res['thongBao'];
  }

  static Future<void> huy(int id) async => ApiClient.delete('/integrations/$id');
}

// Liên kết ZaloPay THẬT (UAT/sandbox) + thanh toán Premium bằng QR ZaloPay - xem ZaloPayApiController.cs
class ZaloPayService {
  // Tạo đơn xác thực 1.000đ để liên kết tài khoản ZaloPay UAT (chỉ dành cho Premium, giống các nhà cung cấp khác)
  static Future<ZaloPayDonHangModel> taoDonLienKet() async {
    final res = await ApiClient.post('/zalopay/lien-ket/tao-don');
    return ZaloPayDonHangModel.fromJson(res);
  }

  // Tạo đơn thanh toán ZaloPay UAT để nâng cấp Premium - KHÔNG cần đang là Premium
  static Future<ZaloPayDonHangModel> taoDonPremium(int soThang) async {
    final res = await ApiClient.post('/zalopay/premium/tao-don?soThang=$soThang');
    return ZaloPayDonHangModel.fromJson(res);
  }

  // Gọi định kỳ (mỗi ~3 giây) trong lúc chờ người dùng quét & thanh toán trên app ZaloPay UAT
  static Future<ZaloPayTrangThaiModel> trangThai(String appTransId) async {
    final res = await ApiClient.get('/zalopay/don/$appTransId/trang-thai');
    return ZaloPayTrangThaiModel.fromJson(res);
  }

  // Ảnh QR (PNG) - dùng với ApiClient.getBytes(...) rồi Image.memory(...) để tự đính kèm JWT token
  static String duongDanAnhQr(String appTransId) => '/zalopay/don/$appTransId/anh-qr';
}

class PremiumService {
  static Future<PremiumPricing> bangGia() async {
    final res = await ApiClient.get('/premium/bang-gia');
    return PremiumPricing.fromJson(res);
  }

  static Future<PremiumOrderModel> dangKyNangCap(int soThang) async {
    final res = await ApiClient.post('/premium/nang-cap?soThang=$soThang');
    return PremiumOrderModel.fromJson(res);
  }

  static Future<String> xacNhanThanhToan(int donId) async {
    final res = await ApiClient.post('/premium/xac-nhan-thanh-toan/$donId');
    return res['thongBao'];
  }
}

class PaymentService {
  // Trả về ĐƯỜNG DẪN (path) tương đối, dùng với ApiClient.getBytes(...) để tự động
  // đính kèm JWT token và bắt lỗi thật từ server (thay vì Image.network dễ nuốt lỗi).
  static String duongDanAnhQr(String soTaiKhoan, String binNganHang, double soTien, String noiDung) {
    final params = Uri(queryParameters: {
      "soTaiKhoan": soTaiKhoan,
      "binNganHang": binNganHang,
      "soTien": soTien.toStringAsFixed(0),
      "noiDung": noiDung,
    }).query;
    return '/payment/anh-qr?$params';
  }
}

class OcrService {
  static Future<OcrKetQuaDto> quet(File anh) async {
    final res = await ApiClient.uploadFile('/ocr/quet', 'anhHoaDon', anh);
    return OcrKetQuaDto.fromJsonDynamic(res);
  }

  static Future<void> xacNhanLuu({
    required double soTien,
    required DateTime ngayGiaoDich,
    String? ghiChu,
    required int categoryId,
    required LoaiGiaoDich loai,
  }) async {
    await ApiClient.post('/ocr/xac-nhan-luu', {
      "soTien": soTien,
      "ngayGiaoDich": ngayGiaoDich.toIso8601String(),
      "ghiChu": ghiChu,
      "categoryId": categoryId,
      "loai": loai.value,
    });
  }
}

// DTO nhỏ dùng riêng cho kết quả OCR (không đưa vào models.dart cho gọn)
class OcrKetQuaDto {
  final String vanBanGoc;
  final double? soTienNhanDien;
  final DateTime? ngayNhanDien;

  // ==== Các trường AI (LM Studio) phân tích thêm - dùng để tự động điền form ====
  final String? tenCuaHang;
  final String? ghiChuGoiY;
  final int? danhMucGoiYId;
  final String? danhMucGoiYTen;
  final LoaiGiaoDich loaiGoiY;
  final String nguonPhanTich; // "AI" hoặc "QuyTac"

  OcrKetQuaDto({
    required this.vanBanGoc,
    this.soTienNhanDien,
    this.ngayNhanDien,
    this.tenCuaHang,
    this.ghiChuGoiY,
    this.danhMucGoiYId,
    this.danhMucGoiYTen,
    this.loaiGoiY = LoaiGiaoDich.chi,
    this.nguonPhanTich = "QuyTac",
  });

  bool get phanTichBoiAi => nguonPhanTich == "AI";

  factory OcrKetQuaDto.fromJsonDynamic(Map<String, dynamic> j) => OcrKetQuaDto(
        vanBanGoc: j['vanBanGoc'] ?? '',
        soTienNhanDien: j['soTienNhanDien'] != null ? (j['soTienNhanDien'] as num).toDouble() : null,
        ngayNhanDien: j['ngayNhanDien'] != null ? DateTime.parse(j['ngayNhanDien']) : null,
        tenCuaHang: j['tenCuaHang'],
        ghiChuGoiY: j['ghiChuGoiY'],
        danhMucGoiYId: j['danhMucGoiYId'],
        danhMucGoiYTen: j['danhMucGoiYTen'],
        loaiGoiY: LoaiGiaoDichX.fromValue(j['loaiGoiY'] ?? 1),
        nguonPhanTich: j['nguonPhanTich'] ?? "QuyTac",
      );
}

class ProfileService {
  static Future<void> suaHoSo({
    required String userName,
    String? hoTen,
    DateTime? ngaySinh,
    GioiTinh? gioiTinh,
    String? canCuocCongDan,
  }) async {
    await ApiClient.put('/profile', {
      "userName": userName,
      "hoTen": hoTen,
      "ngaySinh": ngaySinh?.toIso8601String(),
      "gioiTinh": gioiTinh?.value,
      "canCuocCongDan": canCuocCongDan,
    });
  }

  static Future<void> doiMatKhau(String matKhauHienTai, String matKhauMoi) async {
    await ApiClient.post('/profile/doi-mat-khau', {
      "matKhauHienTai": matKhauHienTai,
      "matKhauMoi": matKhauMoi,
    });
  }

  static Future<Map<String, dynamic>> themSoDienThoai(String soDienThoai) async {
    return await ApiClient.post('/profile/them-so-dien-thoai', {"soDienThoai": soDienThoai});
  }

  static Future<void> xacMinhSoDienThoai(String soDienThoai, String maOtp) async {
    await ApiClient.post('/profile/xac-minh-so-dien-thoai?soDienThoai=$soDienThoai', {"maOtp": maOtp});
  }

  static Future<Map<String, dynamic>> guiXacMinhEmail() async {
    return await ApiClient.post('/profile/gui-xac-minh-email');
  }

  // Xác nhận email bằng mã OTP 6 số gửi qua email thật (hoặc otpDemo nếu SMTP chưa cấu hình)
  static Future<void> xacNhanEmail(String maOtp) async {
    await ApiClient.post('/profile/xac-nhan-email', {"maOtp": maOtp});
  }

  static Future<String> doiAnhDaiDien(File anh) async {
    final res = await ApiClient.uploadFile('/profile/doi-anh-dai-dien', 'anhDaiDien', anh);
    return res['duongDan'];
  }
}

class SettingsService {
  static Future<void> capNhatGiaoDien({required bool giaoDienToi, bool xoaAnhNen = false}) async {
    await ApiClient.put('/settings/giao-dien', {
      "giaoDienToi": giaoDienToi,
      "xoaAnhNen": xoaAnhNen,
    });
  }

  static Future<void> capNhatThongBao({
    required bool batBaoCaoTaiChinh,
    required bool batNhacNhoNhapLieu,
    required TanSuatBaoCao tanSuatBaoCao,
    required String gioBaoCao,
  }) async {
    await ApiClient.put('/settings/thong-bao', {
      "batBaoCaoTaiChinh": batBaoCaoTaiChinh,
      "batNhacNhoNhapLieu": batNhacNhoNhapLieu,
      "tanSuatBaoCao": tanSuatBaoCao.value,
      "gioBaoCao": gioBaoCao,
    });
  }
}

class ExportService {
  // Trả về đường dẫn API (dùng với ApiClient.getBytes) để tải file Excel/PDF báo cáo
  static String duongDanExcel(DateTime tuNgay, DateTime denNgay) =>
      '/export/excel?tuNgay=${tuNgay.toIso8601String()}&denNgay=${denNgay.toIso8601String()}';

  static String duongDanPdf(DateTime tuNgay, DateTime denNgay) =>
      '/export/pdf?tuNgay=${tuNgay.toIso8601String()}&denNgay=${denNgay.toIso8601String()}';
}

class ContactService {
  static Future<void> gui(String hoTen, String email, String noiDung) async {
    await ApiClient.post('/contact', {
      "hoTen": hoTen,
      "email": email,
      "noiDung": noiDung,
    });
  }
}

class ThongBaoService {
  static Future<Map<String, dynamic>> danhSach() async {
    final res = await ApiClient.get('/thong-bao');
    return {
      "danhSach": (res['danhSach'] as List).map((e) => ThongBaoModel.fromJson(e)).toList(),
      "soChuaXem": res['soChuaXem'] as int,
    };
  }

  static Future<int> soChuaXem() async {
    final res = await ApiClient.get('/thong-bao/so-chua-xem');
    return res['soChuaXem'] as int;
  }

  static Future<void> danhDauDaXem(int id) async => ApiClient.post('/thong-bao/$id/danh-dau-da-xem');
  static Future<void> danhDauChuaXem(int id) async => ApiClient.post('/thong-bao/$id/danh-dau-chua-xem');
  static Future<void> danhDauTatCaDaXem() async => ApiClient.post('/thong-bao/danh-dau-tat-ca-da-xem');
  static Future<void> xoa(int id) async => ApiClient.delete('/thong-bao/$id');
}

class AdminService {
  static Future<AdminDashboardModel> dashboard() async {
    final res = await ApiClient.get('/admin/dashboard');
    return AdminDashboardModel.fromJson(res);
  }

  static Future<List<AdminUserModel>> danhSachUsers({String? tuKhoa}) async {
    final query = tuKhoa != null && tuKhoa.isNotEmpty ? '?tuKhoa=$tuKhoa' : '';
    final res = await ApiClient.get('/admin/users$query');
    return (res as List).map((e) => AdminUserModel.fromJson(e)).toList();
  }

  static Future<AdminUserDetailModel> chiTietUser(String id) async {
    final res = await ApiClient.get('/admin/users/$id');
    return AdminUserDetailModel.fromJson(res);
  }

  static Future<void> capNhatPremium(String id, int soThang) async {
    await ApiClient.post('/admin/users/$id/premium', {"soThang": soThang});
  }

  static Future<void> huyPremium(String id) async => ApiClient.delete('/admin/users/$id/premium');
  static Future<void> khoaTaiKhoan(String id) async => ApiClient.post('/admin/users/$id/khoa');
  static Future<void> moKhoaTaiKhoan(String id) async => ApiClient.post('/admin/users/$id/mo-khoa');
  static Future<void> capQuyenAdmin(String id) async => ApiClient.post('/admin/users/$id/cap-quyen-admin');
  static Future<void> thuHoiQuyenAdmin(String id) async => ApiClient.post('/admin/users/$id/thu-hoi-quyen-admin');
  static Future<void> capXacMinh(String id) async => ApiClient.post('/admin/users/$id/xac-minh');
  static Future<void> huyXacMinh(String id) async => ApiClient.delete('/admin/users/$id/xac-minh');
  static Future<void> xoaNguoiDung(String id) async => ApiClient.delete('/admin/users/$id');

  static Future<List<AdminHouseholdModel>> danhSachHouseholds({String? tuKhoa}) async {
    final query = tuKhoa != null && tuKhoa.isNotEmpty ? '?tuKhoa=$tuKhoa' : '';
    final res = await ApiClient.get('/admin/households$query');
    return (res as List).map((e) => AdminHouseholdModel.fromJson(e)).toList();
  }

  static Future<void> xoaHouseholdAdmin(int id) async => ApiClient.delete('/admin/households/$id');

  static Future<List<AdminPremiumOrderModel>> danhSachPremiumOrders({bool? chuaXacNhan, String? tuKhoa}) async {
    final params = <String, String>{};
    if (chuaXacNhan == true) params['chuaXacNhan'] = 'true';
    if (tuKhoa != null && tuKhoa.isNotEmpty) params['tuKhoa'] = tuKhoa;
    final query = params.isEmpty ? '' : '?${Uri(queryParameters: params).query}';
    final res = await ApiClient.get('/admin/premium-orders$query');
    return (res as List).map((e) => AdminPremiumOrderModel.fromJson(e)).toList();
  }

  static Future<void> xacNhanDon(int id) async => ApiClient.post('/admin/premium-orders/$id/xac-nhan');
  static Future<void> xoaDon(int id) async => ApiClient.delete('/admin/premium-orders/$id');

  static Future<List<AdminContactMessageModel>> danhSachContactMessages({String? tuKhoa}) async {
    final query = tuKhoa != null && tuKhoa.isNotEmpty ? '?tuKhoa=$tuKhoa' : '';
    final res = await ApiClient.get('/admin/contact-messages$query');
    return (res as List).map((e) => AdminContactMessageModel.fromJson(e)).toList();
  }

  static Future<void> danhDauDaXem(int id) async => ApiClient.post('/admin/contact-messages/$id/danh-dau-da-xem');
  static Future<void> xoaTinNhan(int id) async => ApiClient.delete('/admin/contact-messages/$id');
}
