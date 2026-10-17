// ============================================================================
// CÁC MODEL DỮ LIỆU - tương ứng 1-1 với các DTO bên backend (Controllers/Api/Dtos.cs)
// ============================================================================

enum LoaiGiaoDich { thu, chi }

extension LoaiGiaoDichX on LoaiGiaoDich {
  int get value => this == LoaiGiaoDich.thu ? 0 : 1;
  String get nhan => this == LoaiGiaoDich.thu ? "Thu" : "Chi";
  static LoaiGiaoDich fromValue(int v) => v == 0 ? LoaiGiaoDich.thu : LoaiGiaoDich.chi;
}

enum GioiTinh { nam, nu, khac }

extension GioiTinhX on GioiTinh {
  int get value => index;
  String get nhan => switch (this) {
        GioiTinh.nam => "Nam",
        GioiTinh.nu => "Nữ",
        GioiTinh.khac => "Khác",
      };
  static GioiTinh fromValue(int v) => GioiTinh.values[v];
}

enum TanSuatBaoCao { hangNgay, hangTuan }

extension TanSuatBaoCaoX on TanSuatBaoCao {
  int get value => index;
  String get nhan => this == TanSuatBaoCao.hangNgay ? "Hàng ngày" : "Hàng tuần";
  static TanSuatBaoCao fromValue(int v) => TanSuatBaoCao.values[v];
}

enum NhaCungCap { nganHang, moMo, zaloPay }

extension NhaCungCapX on NhaCungCap {
  int get value => index;
  String get nhan => switch (this) {
        NhaCungCap.nganHang => "Ngân hàng",
        NhaCungCap.moMo => "MoMo",
        NhaCungCap.zaloPay => "ZaloPay",
      };
  static NhaCungCap fromValue(int v) => NhaCungCap.values[v];
}

// ================== USER ==================
class UserModel {
  final String id;
  final String? hoTen;
  final String? email;
  final String? phoneNumber;
  final bool daXacMinhDanhTinh;
  final bool emailConfirmed;
  final String? duongDanAnhDaiDien;
  final String? duongDanAnhNen;
  final bool giaoDienToi;
  final bool isPremium;
  final bool premiumConHieuLuc;
  final DateTime? premiumHetHan;
  final int? householdId;
  final bool laAdmin;
  final bool dangNhapBangGoogle;
  final bool phoneNumberConfirmed;
  final DateTime? ngaySinh;
  final GioiTinh? gioiTinh;
  final String? canCuocCongDan;
  final bool batBaoCaoTaiChinh;
  final bool batNhacNhoNhapLieu;
  final TanSuatBaoCao tanSuatBaoCao;
  final String gioBaoCao;

  UserModel({
    required this.id,
    this.hoTen,
    this.email,
    this.phoneNumber,
    this.daXacMinhDanhTinh = false,
    this.emailConfirmed = false,
    this.duongDanAnhDaiDien,
    this.duongDanAnhNen,
    this.giaoDienToi = false,
    this.isPremium = false,
    this.premiumConHieuLuc = false,
    this.premiumHetHan,
    this.householdId,
    this.laAdmin = false,
    this.dangNhapBangGoogle = false,
    this.phoneNumberConfirmed = false,
    this.ngaySinh,
    this.gioiTinh,
    this.canCuocCongDan,
    this.batBaoCaoTaiChinh = false,
    this.batNhacNhoNhapLieu = false,
    this.tanSuatBaoCao = TanSuatBaoCao.hangTuan,
    this.gioBaoCao = "08:00",
  });

  factory UserModel.fromJson(Map<String, dynamic> j) => UserModel(
        id: j['id'],
        hoTen: j['hoTen'],
        email: j['email'],
        phoneNumber: j['phoneNumber'],
        daXacMinhDanhTinh: j['daXacMinhDanhTinh'] ?? false,
        emailConfirmed: j['emailConfirmed'] ?? false,
        duongDanAnhDaiDien: j['duongDanAnhDaiDien'],
        duongDanAnhNen: j['duongDanAnhNen'],
        giaoDienToi: j['giaoDienToi'] ?? false,
        isPremium: j['isPremium'] ?? false,
        premiumConHieuLuc: j['premiumConHieuLuc'] ?? false,
        premiumHetHan: j['premiumHetHan'] != null ? DateTime.parse(j['premiumHetHan']) : null,
        householdId: j['householdId'],
        laAdmin: j['laAdmin'] ?? false,
        dangNhapBangGoogle: j['dangNhapBangGoogle'] ?? false,
        phoneNumberConfirmed: j['phoneNumberConfirmed'] ?? false,
        ngaySinh: j['ngaySinh'] != null ? DateTime.parse(j['ngaySinh']) : null,
        gioiTinh: j['gioiTinh'] != null ? GioiTinhX.fromValue(j['gioiTinh']) : null,
        canCuocCongDan: j['canCuocCongDan'],
        batBaoCaoTaiChinh: j['batBaoCaoTaiChinh'] ?? false,
        batNhacNhoNhapLieu: j['batNhacNhoNhapLieu'] ?? false,
        tanSuatBaoCao: TanSuatBaoCaoX.fromValue(j['tanSuatBaoCao'] ?? 1),
        gioBaoCao: j['gioBaoCao'] ?? "08:00",
      );
}

// ================== CATEGORY ==================
class CategoryModel {
  final int id;
  final String tenDanhMuc;
  final LoaiGiaoDich loai;
  final String mauSac;

  CategoryModel({required this.id, required this.tenDanhMuc, required this.loai, required this.mauSac});

  factory CategoryModel.fromJson(Map<String, dynamic> j) => CategoryModel(
        id: j['id'],
        tenDanhMuc: j['tenDanhMuc'],
        loai: LoaiGiaoDichX.fromValue(j['loai']),
        mauSac: j['mauSac'] ?? "#0d6efd",
      );

  Map<String, dynamic> toJson() => {
        "tenDanhMuc": tenDanhMuc,
        "loai": loai.value,
        "mauSac": mauSac,
      };
}

// ================== TRANSACTION ==================
class TransactionModel {
  final int id;
  final double soTien;
  final LoaiGiaoDich loai;
  final DateTime ngayGiaoDich;
  final String? ghiChu;
  final int categoryId;
  final String? tenDanhMuc;
  final String? mauDanhMuc;
  final bool chiaSeGiaDinh;
  final String? tenNguoiTao;
  final DateTime? thoiGianTao;

  TransactionModel({
    required this.id,
    required this.soTien,
    required this.loai,
    required this.ngayGiaoDich,
    this.ghiChu,
    required this.categoryId,
    this.tenDanhMuc,
    this.mauDanhMuc,
    this.chiaSeGiaDinh = false,
    this.tenNguoiTao,
    this.thoiGianTao,
  });

  factory TransactionModel.fromJson(Map<String, dynamic> j) => TransactionModel(
        id: j['id'],
        soTien: (j['soTien'] as num).toDouble(),
        loai: LoaiGiaoDichX.fromValue(j['loai']),
        ngayGiaoDich: DateTime.parse(j['ngayGiaoDich']),
        ghiChu: j['ghiChu'],
        categoryId: j['categoryId'],
        tenDanhMuc: j['tenDanhMuc'],
        mauDanhMuc: j['mauDanhMuc'],
        chiaSeGiaDinh: j['chiaSeGiaDinh'] ?? false,
        tenNguoiTao: j['tenNguoiTao'],
        thoiGianTao: j['thoiGianTao'] != null ? DateTime.parse(j['thoiGianTao']) : null,
      );

  Map<String, dynamic> toJson() => {
        "soTien": soTien,
        "loai": loai.value,
        "ngayGiaoDich": ngayGiaoDich.toIso8601String(),
        "ghiChu": ghiChu,
        "categoryId": categoryId,
        "chiaSeGiaDinh": chiaSeGiaDinh,
      };
}

// ================== BILL ==================
class BillModel {
  final int id;
  final String tenHoaDon;
  final double soTien;
  final DateTime ngayDenHan;
  final bool daThanhToan;
  final String? ghiChu;
  final int soNgayConLai;

  BillModel({
    required this.id,
    required this.tenHoaDon,
    required this.soTien,
    required this.ngayDenHan,
    required this.daThanhToan,
    this.ghiChu,
    required this.soNgayConLai,
  });

  factory BillModel.fromJson(Map<String, dynamic> j) => BillModel(
        id: j['id'],
        tenHoaDon: j['tenHoaDon'],
        soTien: (j['soTien'] as num).toDouble(),
        ngayDenHan: DateTime.parse(j['ngayDenHan']),
        daThanhToan: j['daThanhToan'] ?? false,
        ghiChu: j['ghiChu'],
        soNgayConLai: j['soNgayConLai'] ?? 0,
      );

  Map<String, dynamic> toJson() => {
        "tenHoaDon": tenHoaDon,
        "soTien": soTien,
        "ngayDenHan": ngayDenHan.toIso8601String(),
        "daThanhToan": daThanhToan,
        "ghiChu": ghiChu,
      };
}

// ================== REPORT ==================
class ReportCategorySlice {
  final String ten;
  final double soTien;
  final String mauSac;

  ReportCategorySlice({required this.ten, required this.soTien, required this.mauSac});

  factory ReportCategorySlice.fromJson(Map<String, dynamic> j) => ReportCategorySlice(
        ten: j['ten'],
        soTien: (j['soTien'] as num).toDouble(),
        mauSac: j['mauSac'] ?? "#0d6efd",
      );
}

class ReportModel {
  final DateTime tuNgay;
  final DateTime denNgay;
  final double tongThu;
  final double tongChi;
  final List<ReportCategorySlice> danhMucChi;
  final List<ReportCategorySlice> danhMucThu;

  ReportModel({
    required this.tuNgay,
    required this.denNgay,
    required this.tongThu,
    required this.tongChi,
    required this.danhMucChi,
    required this.danhMucThu,
  });

  factory ReportModel.fromJson(Map<String, dynamic> j) => ReportModel(
        tuNgay: DateTime.parse(j['tuNgay']),
        denNgay: DateTime.parse(j['denNgay']),
        tongThu: (j['tongThu'] as num).toDouble(),
        tongChi: (j['tongChi'] as num).toDouble(),
        danhMucChi: (j['danhMucChi'] as List).map((e) => ReportCategorySlice.fromJson(e)).toList(),
        danhMucThu: (j['danhMucThu'] as List).map((e) => ReportCategorySlice.fromJson(e)).toList(),
      );
}

// ================== HOUSEHOLD ==================
class ThanhVienChiTieu {
  final String tenThanhVien;
  final double tongThu;
  final double tongChi;

  ThanhVienChiTieu({required this.tenThanhVien, required this.tongThu, required this.tongChi});

  factory ThanhVienChiTieu.fromJson(Map<String, dynamic> j) => ThanhVienChiTieu(
        tenThanhVien: j['tenThanhVien'],
        tongThu: (j['tongThu'] as num).toDouble(),
        tongChi: (j['tongChi'] as num).toDouble(),
      );
}

class HouseholdModel {
  final int id;
  final String tenHoGiaDinh;
  final String maMoi;
  final String? tenChuHo;
  final bool laChuHo;
  final double tongThuGiaDinh;
  final double tongChiGiaDinh;
  final List<TransactionModel> giaoDichGanDay;
  final List<ThanhVienChiTieu> thanhVienChiTieu;

  HouseholdModel({
    required this.id,
    required this.tenHoGiaDinh,
    required this.maMoi,
    this.tenChuHo,
    this.laChuHo = false,
    required this.tongThuGiaDinh,
    required this.tongChiGiaDinh,
    required this.giaoDichGanDay,
    required this.thanhVienChiTieu,
  });

  factory HouseholdModel.fromJson(Map<String, dynamic> j) => HouseholdModel(
        id: j['id'],
        tenHoGiaDinh: j['tenHoGiaDinh'],
        maMoi: j['maMoi'],
        tenChuHo: j['tenChuHo'],
        laChuHo: j['laChuHo'] ?? false,
        tongThuGiaDinh: (j['tongThuGiaDinh'] as num).toDouble(),
        tongChiGiaDinh: (j['tongChiGiaDinh'] as num).toDouble(),
        giaoDichGanDay: (j['giaoDichGanDay'] as List).map((e) => TransactionModel.fromJson(e)).toList(),
        thanhVienChiTieu: (j['thanhVienChiTieu'] as List).map((e) => ThanhVienChiTieu.fromJson(e)).toList(),
      );
}

// ================== PREMIUM ==================
class PremiumPricing {
  final double giaThang;
  final String soTaiKhoan;
  final String binNganHang;
  final String tenNganHangHienThi;

  PremiumPricing({required this.giaThang, required this.soTaiKhoan, required this.binNganHang, required this.tenNganHangHienThi});

  factory PremiumPricing.fromJson(Map<String, dynamic> j) => PremiumPricing(
        giaThang: (j['giaThang'] as num).toDouble(),
        soTaiKhoan: j['soTaiKhoan'] ?? "",
        binNganHang: j['binNganHang'] ?? "",
        tenNganHangHienThi: j['tenNganHangHienThi'] ?? "",
      );
}

class PremiumOrderModel {
  final int id;
  final String maGiaoDich;
  final double soTien;
  final int soThangDangKy;
  final bool daXacNhan;
  final DateTime ngayTao;
  final String soTaiKhoan;
  final String binNganHang;
  final String tenNganHangHienThi;

  PremiumOrderModel({
    required this.id,
    required this.maGiaoDich,
    required this.soTien,
    required this.soThangDangKy,
    required this.daXacNhan,
    required this.ngayTao,
    required this.soTaiKhoan,
    required this.binNganHang,
    required this.tenNganHangHienThi,
  });

  factory PremiumOrderModel.fromJson(Map<String, dynamic> j) => PremiumOrderModel(
        id: j['id'],
        maGiaoDich: j['maGiaoDich'],
        soTien: (j['soTien'] as num).toDouble(),
        soThangDangKy: j['soThangDangKy'],
        daXacNhan: j['daXacNhan'] ?? false,
        ngayTao: DateTime.parse(j['ngayTao']),
        soTaiKhoan: j['soTaiKhoan'] ?? "",
        binNganHang: j['binNganHang'] ?? "",
        tenNganHangHienThi: j['tenNganHangHienThi'] ?? "",
      );
}

// ================== FINANCIAL LINK (Integrations) ==================
class FinancialLinkModel {
  final int id;
  final NhaCungCap nhaCungCap;
  final String tenGoiNho;
  final String soTaiKhoanAn;
  final bool daKetNoi;
  final DateTime? ngayKetNoi;
  final DateTime? dongBoLanCuoi;
  final String? ghiChu;

  FinancialLinkModel({
    required this.id,
    required this.nhaCungCap,
    required this.tenGoiNho,
    required this.soTaiKhoanAn,
    required this.daKetNoi,
    this.ngayKetNoi,
    this.dongBoLanCuoi,
    this.ghiChu,
  });

  factory FinancialLinkModel.fromJson(Map<String, dynamic> j) => FinancialLinkModel(
        id: j['id'],
        nhaCungCap: NhaCungCapX.fromValue(j['nhaCungCap']),
        tenGoiNho: j['tenGoiNho'],
        soTaiKhoanAn: j['soTaiKhoanAn'],
        daKetNoi: j['daKetNoi'] ?? false,
        ngayKetNoi: j['ngayKetNoi'] != null ? DateTime.parse(j['ngayKetNoi']) : null,
        dongBoLanCuoi: j['dongBoLanCuoi'] != null ? DateTime.parse(j['dongBoLanCuoi']) : null,
        ghiChu: j['ghiChu'],
      );
}

// ================== ZALOPAY (UAT) ==================
class ZaloPayDonHangModel {
  final String appTransId;
  final double soTien;
  final String moTa;
  final String urlAnhQr; // đường dẫn tương đối (dùng với ApiClient.getBytes / urlAnhCoThamSo)
  final String? orderUrl;
  final int? premiumOrderId;
  final int? soThangDangKy;

  ZaloPayDonHangModel({
    required this.appTransId,
    required this.soTien,
    required this.moTa,
    required this.urlAnhQr,
    this.orderUrl,
    this.premiumOrderId,
    this.soThangDangKy,
  });

  factory ZaloPayDonHangModel.fromJson(Map<String, dynamic> j) => ZaloPayDonHangModel(
        appTransId: j['appTransId'],
        soTien: (j['soTien'] as num).toDouble(),
        moTa: j['moTa'] ?? '',
        urlAnhQr: j['urlAnhQr'],
        orderUrl: j['orderUrl'],
        premiumOrderId: j['premiumOrderId'],
        soThangDangKy: j['soThangDangKy'],
      );
}

enum ZaloPayTrangThai { choThanhToan, thanhCong, thatBai }

class ZaloPayTrangThaiModel {
  final String appTransId;
  final ZaloPayTrangThai trangThai;
  final double soTien;
  final bool isPremium;
  final DateTime? premiumHetHan;

  ZaloPayTrangThaiModel({
    required this.appTransId,
    required this.trangThai,
    required this.soTien,
    required this.isPremium,
    this.premiumHetHan,
  });

  factory ZaloPayTrangThaiModel.fromJson(Map<String, dynamic> j) => ZaloPayTrangThaiModel(
        appTransId: j['appTransId'],
        trangThai: switch (j['trangThai']) {
          'ThanhCong' => ZaloPayTrangThai.thanhCong,
          'ThatBai' => ZaloPayTrangThai.thatBai,
          _ => ZaloPayTrangThai.choThanhToan,
        },
        soTien: (j['soTien'] as num).toDouble(),
        isPremium: j['isPremium'] ?? false,
        premiumHetHan: j['premiumHetHan'] != null ? DateTime.parse(j['premiumHetHan']) : null,
      );
}

// ================== CHATBOT ==================
class CauHoiMau {
  final String cauHoi;
  final String cauTraLoi;

  CauHoiMau({required this.cauHoi, required this.cauTraLoi});

  factory CauHoiMau.fromJson(Map<String, dynamic> j) => CauHoiMau(
        cauHoi: j['cauHoi'],
        cauTraLoi: j['cauTraLoi'],
      );
}

class ChatMessage {
  final String noiDung;
  final bool laNguoiDung; // true = tin nhắn của người dùng, false = của bot

  ChatMessage({required this.noiDung, required this.laNguoiDung});
}

// ================== INSIGHTS ==================
class InsightItem {
  final String icon;
  final String mucDo;
  final String tieuDe;
  final String noiDung;

  InsightItem({required this.icon, required this.mucDo, required this.tieuDe, required this.noiDung});

  factory InsightItem.fromJson(Map<String, dynamic> j) => InsightItem(
        icon: j['icon'] ?? 'bi-lightbulb',
        mucDo: j['mucDo'] ?? 'info',
        tieuDe: j['tieuDe'] ?? '',
        noiDung: j['noiDung'] ?? '',
      );
}

class DuDoanSoDu {
  final double soDuHienTai;
  final double chiTieuTrungBinhNgay;
  final double thuNhapTrungBinhNgay;
  final int soNgayConLaiTrongThang;
  final double soDuDuDoanCuoiThang;
  final String xuHuong;

  DuDoanSoDu({
    required this.soDuHienTai,
    required this.chiTieuTrungBinhNgay,
    required this.thuNhapTrungBinhNgay,
    required this.soNgayConLaiTrongThang,
    required this.soDuDuDoanCuoiThang,
    required this.xuHuong,
  });

  factory DuDoanSoDu.fromJson(Map<String, dynamic> j) => DuDoanSoDu(
        soDuHienTai: (j['soDuHienTai'] as num?)?.toDouble() ?? 0,
        chiTieuTrungBinhNgay: (j['chiTieuTrungBinhNgay'] as num?)?.toDouble() ?? 0,
        thuNhapTrungBinhNgay: (j['thuNhapTrungBinhNgay'] as num?)?.toDouble() ?? 0,
        soNgayConLaiTrongThang: j['soNgayConLaiTrongThang'] ?? 0,
        soDuDuDoanCuoiThang: (j['soDuDuDoanCuoiThang'] as num?)?.toDouble() ?? 0,
        xuHuong: j['xuHuong'] ?? 'on_dinh',
      );
}

class InsightsModel {
  final List<InsightItem> thoiQuenChiTieu;
  final List<InsightItem> goiYTietKiem;
  final DuDoanSoDu duDoan;

  InsightsModel({required this.thoiQuenChiTieu, required this.goiYTietKiem, required this.duDoan});

  factory InsightsModel.fromJson(Map<String, dynamic> j) => InsightsModel(
        thoiQuenChiTieu: (j['thoiQuenChiTieu'] as List? ?? []).map((e) => InsightItem.fromJson(e)).toList(),
        goiYTietKiem: (j['goiYTietKiem'] as List? ?? []).map((e) => InsightItem.fromJson(e)).toList(),
        duDoan: DuDoanSoDu.fromJson(j['duDoan'] ?? {}),
      );
}

// ================== THÔNG BÁO ==================
enum LoaiThongBao { hoaDon, phanTichAI, baoCao, xacMinh, hoSo, hoGiaDinh, premium, heThong }

extension LoaiThongBaoX on LoaiThongBao {
  static LoaiThongBao fromValue(int v) => LoaiThongBao.values[v];
}

class ThongBaoModel {
  final int id;
  final String tieuDe;
  final String noiDung;
  final LoaiThongBao loai;
  final String? duongDanDieuHuong;
  final bool daXem;
  final DateTime ngayTao;

  ThongBaoModel({
    required this.id,
    required this.tieuDe,
    required this.noiDung,
    required this.loai,
    this.duongDanDieuHuong,
    required this.daXem,
    required this.ngayTao,
  });

  factory ThongBaoModel.fromJson(Map<String, dynamic> j) => ThongBaoModel(
        id: j['id'],
        tieuDe: j['tieuDe'],
        noiDung: j['noiDung'],
        loai: LoaiThongBaoX.fromValue(j['loai'] ?? 7),
        duongDanDieuHuong: j['duongDanDieuHuong'],
        daXem: j['daXem'] ?? false,
        ngayTao: DateTime.parse(j['ngayTao']),
      );
}
class AdminDashboardModel {
  final int tongNguoiDung;
  final int tongNguoiDungPremium;
  final int nguoiDungMoiThangNay;
  final int tongGiaoDich;
  final int tongHoGiaDinh;
  final double tongDoanhThu;
  final int donChoXacNhan;
  final List<String> nhanThang;
  final List<double> doanhThuTheoThang;
  final List<int> nguoiDungMoiTheoThang;

  AdminDashboardModel({
    required this.tongNguoiDung,
    required this.tongNguoiDungPremium,
    required this.nguoiDungMoiThangNay,
    required this.tongGiaoDich,
    required this.tongHoGiaDinh,
    required this.tongDoanhThu,
    required this.donChoXacNhan,
    required this.nhanThang,
    required this.doanhThuTheoThang,
    required this.nguoiDungMoiTheoThang,
  });

  factory AdminDashboardModel.fromJson(Map<String, dynamic> j) => AdminDashboardModel(
        tongNguoiDung: j['tongNguoiDung'] ?? 0,
        tongNguoiDungPremium: j['tongNguoiDungPremium'] ?? 0,
        nguoiDungMoiThangNay: j['nguoiDungMoiThangNay'] ?? 0,
        tongGiaoDich: j['tongGiaoDich'] ?? 0,
        tongHoGiaDinh: j['tongHoGiaDinh'] ?? 0,
        tongDoanhThu: (j['tongDoanhThu'] as num?)?.toDouble() ?? 0,
        donChoXacNhan: j['donChoXacNhan'] ?? 0,
        nhanThang: (j['nhanThang'] as List? ?? []).map((e) => e.toString()).toList(),
        doanhThuTheoThang: (j['doanhThuTheoThang'] as List? ?? []).map((e) => (e as num).toDouble()).toList(),
        nguoiDungMoiTheoThang: (j['nguoiDungMoiTheoThang'] as List? ?? []).map((e) => e as int).toList(),
      );
}

class AdminUserModel {
  final String id;
  final String? hoTen;
  final String? email;
  final String? userName;
  final DateTime ngayTao;
  final bool isPremium;
  final bool premiumConHieuLuc;
  final bool laAdmin;
  final bool biKhoa;
  final bool daXacMinhDanhTinh;

  AdminUserModel({
    required this.id,
    this.hoTen,
    this.email,
    this.userName,
    required this.ngayTao,
    required this.isPremium,
    required this.premiumConHieuLuc,
    required this.laAdmin,
    required this.biKhoa,
    this.daXacMinhDanhTinh = false,
  });

  factory AdminUserModel.fromJson(Map<String, dynamic> j) => AdminUserModel(
        id: j['id'],
        hoTen: j['hoTen'],
        email: j['email'],
        userName: j['userName'],
        ngayTao: DateTime.parse(j['ngayTao']),
        isPremium: j['isPremium'] ?? false,
        premiumConHieuLuc: j['premiumConHieuLuc'] ?? false,
        daXacMinhDanhTinh: j['daXacMinhDanhTinh'] ?? false,
        laAdmin: j['laAdmin'] ?? false,
        biKhoa: j['biKhoa'] ?? false,
      );
}

class AdminUserDetailModel extends AdminUserModel {
  final double tongThu;
  final double tongChi;
  final int soGiaoDich;
  final int soDonPremium;
  final DateTime? premiumHetHan;

  AdminUserDetailModel({
    required super.id,
    super.hoTen,
    super.email,
    super.userName,
    required super.ngayTao,
    required super.isPremium,
    required super.premiumConHieuLuc,
    required super.laAdmin,
    required super.biKhoa,
    super.daXacMinhDanhTinh,
    required this.tongThu,
    required this.tongChi,
    required this.soGiaoDich,
    required this.soDonPremium,
    this.premiumHetHan,
  });

  factory AdminUserDetailModel.fromJson(Map<String, dynamic> j) => AdminUserDetailModel(
        id: j['id'],
        hoTen: j['hoTen'],
        email: j['email'],
        userName: j['userName'],
        ngayTao: DateTime.parse(j['ngayTao']),
        isPremium: j['isPremium'] ?? false,
        premiumConHieuLuc: j['premiumConHieuLuc'] ?? false,
        laAdmin: j['laAdmin'] ?? false,
        biKhoa: j['biKhoa'] ?? false,
        daXacMinhDanhTinh: j['daXacMinhDanhTinh'] ?? false,
        tongThu: (j['tongThu'] as num?)?.toDouble() ?? 0,
        tongChi: (j['tongChi'] as num?)?.toDouble() ?? 0,
        soGiaoDich: j['soGiaoDich'] ?? 0,
        soDonPremium: j['soDonPremium'] ?? 0,
        premiumHetHan: j['premiumHetHan'] != null ? DateTime.parse(j['premiumHetHan']) : null,
      );
}

class AdminHouseholdModel {
  final int id;
  final String tenHoGiaDinh;
  final String maMoi;
  final String? tenChuHo;
  final int soThanhVien;
  final DateTime ngayTao;

  AdminHouseholdModel({
    required this.id,
    required this.tenHoGiaDinh,
    required this.maMoi,
    this.tenChuHo,
    required this.soThanhVien,
    required this.ngayTao,
  });

  factory AdminHouseholdModel.fromJson(Map<String, dynamic> j) => AdminHouseholdModel(
        id: j['id'],
        tenHoGiaDinh: j['tenHoGiaDinh'],
        maMoi: j['maMoi'],
        tenChuHo: j['tenChuHo'],
        soThanhVien: j['soThanhVien'] ?? 0,
        ngayTao: DateTime.parse(j['ngayTao']),
      );
}

class AdminPremiumOrderModel {
  final int id;
  final String maGiaoDich;
  final double soTien;
  final int soThangDangKy;
  final bool daXacNhan;
  final DateTime ngayTao;
  final String? emailNguoiDung;

  AdminPremiumOrderModel({
    required this.id,
    required this.maGiaoDich,
    required this.soTien,
    required this.soThangDangKy,
    required this.daXacNhan,
    required this.ngayTao,
    this.emailNguoiDung,
  });

  factory AdminPremiumOrderModel.fromJson(Map<String, dynamic> j) => AdminPremiumOrderModel(
        id: j['id'],
        maGiaoDich: j['maGiaoDich'],
        soTien: (j['soTien'] as num).toDouble(),
        soThangDangKy: j['soThangDangKy'],
        daXacNhan: j['daXacNhan'] ?? false,
        ngayTao: DateTime.parse(j['ngayTao']),
        emailNguoiDung: j['emailNguoiDung'],
      );
}

class AdminContactMessageModel {
  final int id;
  final String hoTen;
  final String email;
  final String noiDung;
  final DateTime ngayGui;
  final bool daXem;

  AdminContactMessageModel({
    required this.id,
    required this.hoTen,
    required this.email,
    required this.noiDung,
    required this.ngayGui,
    required this.daXem,
  });

  factory AdminContactMessageModel.fromJson(Map<String, dynamic> j) => AdminContactMessageModel(
        id: j['id'],
        hoTen: j['hoTen'],
        email: j['email'],
        noiDung: j['noiDung'],
        ngayGui: DateTime.parse(j['ngayGui']),
        daXem: j['daXem'] ?? false,
      );
}
