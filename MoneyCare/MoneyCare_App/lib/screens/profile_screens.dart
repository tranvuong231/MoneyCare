import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../providers/auth_provider.dart';
import '../config/api_config.dart';
import '../widgets/common_widgets.dart';

// Ảnh đại diện có thể là đường dẫn cục bộ do server lưu (VD "/uploads/avatars/...") hoặc
// URL đầy đủ từ Google (VD "https://lh3.googleusercontent.com/..."). Hàm này xử lý cả 2 trường hợp.
String _urlAnh(String duongDan) => duongDan.startsWith('http') ? duongDan : "${ApiConfig.baseUrl}$duongDan";

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final u = auth.nguoiDung;
    if (u == null) return const DangTai();

    return Scaffold(
      appBar: AppBar(title: const Text("Hồ sơ cá nhân")),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Center(
            child: Column(
              children: [
                Stack(
                  children: [
                    CircleAvatar(
                      radius: 48,
                      backgroundColor: mauChinh.withValues(alpha: 0.15),
                      backgroundImage: u.duongDanAnhDaiDien != null ? NetworkImage(_urlAnh(u.duongDanAnhDaiDien!)) : null,
                      child: u.duongDanAnhDaiDien == null ? const Icon(Icons.person, size: 48, color: mauChinh) : null,
                    ),
                    if (!u.dangNhapBangGoogle)
                      Positioned(
                        bottom: 0,
                        right: 0,
                        child: GestureDetector(
                          onTap: () async {
                            final picker = ImagePicker();
                            final file = await picker.pickImage(source: ImageSource.gallery, imageQuality: 85);
                            if (file == null) return;
                            try {
                              await ProfileService.doiAnhDaiDien(File(file.path));
                              await auth.lamMoiThongTinNguoiDung();
                            } on ApiException catch (e) {
                              if (context.mounted) hienThongBao(context, e.message, loi: true);
                            }
                          },
                          child: const CircleAvatar(radius: 16, backgroundColor: mauChinh, child: Icon(Icons.edit, size: 16, color: Colors.white)),
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 12),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(u.hoTen ?? u.email ?? '', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    if (u.daXacMinhDanhTinh) const Padding(padding: EdgeInsets.only(left: 4), child: Icon(Icons.verified, color: mauChinh, size: 18)),
                  ],
                ),
                Text(u.email ?? '', style: const TextStyle(color: Colors.grey)),
                if (u.premiumConHieuLuc) const Padding(padding: EdgeInsets.only(top: 8), child: HuyHieuPremium()),
              ],
            ),
          ),
          const SizedBox(height: 24),
          _dongMuc(context, Icons.edit, "Sửa hồ sơ", () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const EditProfileScreen()))),
          _dongMuc(context, Icons.lock_outline, "Đổi mật khẩu", () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ChangePasswordScreen()))),
          _dongMuc(context, Icons.phone_outlined, u.phoneNumber == null ? "Thêm số điện thoại" : "Số điện thoại: ${u.phoneNumber}",
              () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const AddPhoneScreen()))),
          if (!u.emailConfirmed)
            _dongMuc(context, Icons.mark_email_unread_outlined, "Xác minh Email", () => _xuLyXacMinhEmail(context, auth)),
          _dongMuc(context, Icons.workspace_premium_outlined, "Nâng cấp Premium", () => Navigator.of(context).pushNamed('/premium')),
          _dongMuc(context, Icons.settings_outlined, "Cài đặt", () => Navigator.of(context).pushNamed('/settings')),
          _dongMuc(context, Icons.account_balance_outlined, "Liên kết Ngân hàng / Ví", () => Navigator.of(context).pushNamed('/integrations')),
          _dongMuc(context, Icons.mail_outline, "Liên hệ / Góp ý", () => Navigator.of(context).pushNamed('/contact')),
          if (u.laAdmin) _dongMuc(context, Icons.admin_panel_settings_outlined, "Trang quản trị (Admin)", () => Navigator.of(context).pushNamed('/admin')),
          const SizedBox(height: 12),
          _dongMuc(context, Icons.logout, "Đăng xuất", () async {
            final xacNhan = await showDialog<bool>(
              context: context,
              builder: (_) => AlertDialog(
                title: const Text("Đăng xuất"),
                content: const Text("Bạn có chắc muốn đăng xuất?"),
                actions: [
                  TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
                  TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Đăng xuất", style: TextStyle(color: Colors.red))),
                ],
              ),
            );
            if (xacNhan == true) await auth.dangXuat();
          }, mau: Colors.red),
        ],
      ),
    );
  }

  Widget _dongMuc(BuildContext context, IconData icon, String nhan, VoidCallback? onTap, {Color? mau}) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(icon, color: mau ?? mauChinh),
        title: Text(nhan, style: TextStyle(color: mau)),
        trailing: onTap != null ? const Icon(Icons.chevron_right) : null,
        onTap: onTap,
      ),
    );
  }

  Future<void> _xuLyXacMinhEmail(BuildContext context, AuthProvider auth) async {
    try {
      final res = await ProfileService.guiXacMinhEmail();
      final otpDemo = res['otpDemo'] as String?;
      if (context.mounted) hienThongBao(context, res['thongBao'] ?? "Đã gửi mã xác minh");

      final maCtrl = TextEditingController(text: otpDemo ?? '');
      if (!context.mounted) return;
      final maNhap = await showDialog<String>(
        context: context,
        builder: (_) => AlertDialog(
          title: const Text("Nhập mã xác minh Email"),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text("Mã OTP 6 số vừa được gửi tới email của bạn${otpDemo != null ? ' (chế độ demo - đã tự điền sẵn mã)' : ''}."),
              const SizedBox(height: 12),
              TextField(
                controller: maCtrl,
                keyboardType: TextInputType.number,
                maxLength: 6,
                decoration: const InputDecoration(labelText: "Mã OTP", border: OutlineInputBorder()),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(context), child: const Text("Hủy")),
            ElevatedButton(onPressed: () => Navigator.pop(context, maCtrl.text.trim()), child: const Text("Xác nhận")),
          ],
        ),
      );

      if (maNhap != null && maNhap.isNotEmpty) {
        await ProfileService.xacNhanEmail(maNhap);
        await auth.lamMoiThongTinNguoiDung();
        if (context.mounted) hienThongBao(context, "🎉 Xác minh email thành công!");
      }
    } on ApiException catch (e) {
      if (context.mounted) hienThongBao(context, e.message, loi: true);
    }
  }
}

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});
  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _userNameCtrl;
  late TextEditingController _hoTenCtrl;
  late TextEditingController _cccdCtrl;
  DateTime? _ngaySinh;
  GioiTinh? _gioiTinh;
  bool _dangLuu = false;

  @override
  void initState() {
    super.initState();
    final u = context.read<AuthProvider>().nguoiDung!;
    _userNameCtrl = TextEditingController(text: u.email);
    _hoTenCtrl = TextEditingController(text: u.hoTen ?? '');
    _cccdCtrl = TextEditingController(text: u.canCuocCongDan ?? '');
    _ngaySinh = u.ngaySinh;
    _gioiTinh = u.gioiTinh;
  }

  Future<void> _luu() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangLuu = true);
    try {
      await ProfileService.suaHoSo(
        userName: _userNameCtrl.text.trim(),
        hoTen: _hoTenCtrl.text.trim(),
        ngaySinh: _ngaySinh,
        gioiTinh: _gioiTinh,
        canCuocCongDan: _cccdCtrl.text.trim(),
      );
      if (!mounted) return;
      await context.read<AuthProvider>().lamMoiThongTinNguoiDung();
      if (mounted) Navigator.of(context).pop();
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final u = context.watch<AuthProvider>().nguoiDung!;
    final khoaTenDangNhap = u.dangNhapBangGoogle || u.emailConfirmed;
    final khoaHoTen = u.dangNhapBangGoogle || u.daXacMinhDanhTinh;

    return Scaffold(
      appBar: AppBar(title: const Text("Sửa hồ sơ")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (khoaTenDangNhap || khoaHoTen)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(color: mauChinh.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(8)),
                  child: Text(
                    [
                      if (u.dangNhapBangGoogle) "Tài khoản đăng nhập bằng Google - tên đăng nhập & họ tên được đồng bộ tự động, không thể chỉnh sửa.",
                      if (!u.dangNhapBangGoogle && khoaTenDangNhap) "Email đã được xác minh - không thể đổi tên đăng nhập/email nữa.",
                      if (!u.dangNhapBangGoogle && khoaHoTen) "Tài khoản đã xác minh danh tính (tích xanh) - không thể đổi họ tên nữa.",
                    ].join(" "),
                    style: const TextStyle(fontSize: 13, color: mauChinh),
                  ),
                ),
              TextFormField(
                controller: _userNameCtrl,
                enabled: !khoaTenDangNhap,
                decoration: const InputDecoration(labelText: "Tên đăng nhập", border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _hoTenCtrl,
                enabled: !khoaHoTen,
                decoration: const InputDecoration(labelText: "Họ và tên", border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(_ngaySinh == null ? "Chọn ngày sinh" : "Ngày sinh: ${ngayVn(_ngaySinh!)}"),
                trailing: const Icon(Icons.calendar_today),
                onTap: () async {
                  final chon = await showDatePicker(context: context, initialDate: _ngaySinh ?? DateTime(2000), firstDate: DateTime(1900), lastDate: DateTime.now());
                  if (chon != null) setState(() => _ngaySinh = chon);
                },
              ),
              DropdownButtonFormField<GioiTinh>(
                initialValue: _gioiTinh,
                decoration: const InputDecoration(labelText: "Giới tính", border: OutlineInputBorder()),
                items: GioiTinh.values.map((g) => DropdownMenuItem(value: g, child: Text(g.nhan))).toList(),
                onChanged: (v) => setState(() => _gioiTinh = v),
              ),
              const SizedBox(height: 16),
              TextFormField(controller: _cccdCtrl, decoration: const InputDecoration(labelText: "Căn cước công dân", border: OutlineInputBorder())),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _dangLuu ? null : _luu,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Lưu thay đổi"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});
  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _hienTaiCtrl = TextEditingController();
  final _moiCtrl = TextEditingController();
  bool _dangLuu = false;

  Future<void> _doi() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangLuu = true);
    try {
      await ProfileService.doiMatKhau(_hienTaiCtrl.text, _moiCtrl.text);
      if (mounted) {
        hienThongBao(context, "Đổi mật khẩu thành công!");
        Navigator.of(context).pop();
      }
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Đổi mật khẩu")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _hienTaiCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: "Mật khẩu hiện tại", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập mật khẩu hiện tại" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _moiCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: "Mật khẩu mới (tối thiểu 6 ký tự)", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.length < 6) ? "Mật khẩu tối thiểu 6 ký tự" : null,
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _dangLuu ? null : _doi,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Đổi mật khẩu"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class AddPhoneScreen extends StatefulWidget {
  const AddPhoneScreen({super.key});
  @override
  State<AddPhoneScreen> createState() => _AddPhoneScreenState();
}

class _AddPhoneScreenState extends State<AddPhoneScreen> {
  final _sdtCtrl = TextEditingController();
  final _otpCtrl = TextEditingController();
  bool _daGuiOtp = false;
  String? _otpDemo;
  bool _dangXuLy = false;

  Future<void> _guiOtp() async {
    setState(() => _dangXuLy = true);
    try {
      final res = await ProfileService.themSoDienThoai(_sdtCtrl.text.trim());
      setState(() {
        _daGuiOtp = true;
        _otpDemo = res['otpDemo'];
      });
      if (mounted) hienThongBao(context, res['thongBao'] ?? "Đã gửi mã OTP");
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  Future<void> _xacMinh() async {
    setState(() => _dangXuLy = true);
    try {
      await ProfileService.xacMinhSoDienThoai(_sdtCtrl.text.trim(), _otpCtrl.text.trim());
      if (!mounted) return;
      await context.read<AuthProvider>().lamMoiThongTinNguoiDung();
      if (mounted) {
        hienThongBao(context, "Xác minh số điện thoại thành công!");
        Navigator.of(context).pop();
      }
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final u = context.watch<AuthProvider>().nguoiDung;
    final coSoCu = u?.phoneNumber != null && u!.phoneNumber!.isNotEmpty;

    return Scaffold(
      appBar: AppBar(title: Text(coSoCu ? "Đổi số điện thoại" : "Thêm số điện thoại")),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (coSoCu)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(color: Colors.orange.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(8)),
                child: Text(
                  "Số hiện tại: ${u.phoneNumber}. Đổi sang số mới sẽ cần xác minh lại - trong lúc chờ xác minh, tích xanh (nếu có) sẽ tạm mất và chỉ được cấp lại sau khi xác minh số mới thành công.",
                  style: const TextStyle(fontSize: 13, color: Colors.deepOrange),
                ),
              ),
            Container(
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.only(bottom: 16),
              decoration: BoxDecoration(color: mauChinh.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(8)),
              child: const Text(
                "Lưu ý: sau khi xác minh số điện thoại thành công và đủ điều kiện tích xanh (đã xác minh cả Email), bạn sẽ KHÔNG thể sửa họ và tên nữa.",
                style: TextStyle(fontSize: 13, color: mauChinh),
              ),
            ),
            TextField(
              controller: _sdtCtrl,
              enabled: !_daGuiOtp,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: "Số điện thoại", border: OutlineInputBorder()),
            ),
            const SizedBox(height: 16),
            if (!_daGuiOtp)
              ElevatedButton(
                onPressed: _dangXuLy ? null : _guiOtp,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white),
                child: const Text("Gửi mã OTP"),
              )
            else ...[
              if (_otpDemo != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text("Chế độ demo - Mã OTP của bạn là: $_otpDemo", style: const TextStyle(color: Colors.orange, fontWeight: FontWeight.bold)),
                ),
              TextField(controller: _otpCtrl, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: "Nhập mã OTP", border: OutlineInputBorder())),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _dangXuLy ? null : _xacMinh,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white),
                child: const Text("Xác minh"),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
