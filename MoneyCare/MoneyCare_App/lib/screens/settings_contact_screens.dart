import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../providers/auth_provider.dart';
import '../providers/theme_provider.dart';
import '../widgets/common_widgets.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});
  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _giaoDienToi = false;
  bool _batBaoCaoTaiChinh = false;
  bool _batNhacNhoNhapLieu = false;
  TanSuatBaoCao _tanSuat = TanSuatBaoCao.hangTuan;
  TimeOfDay _gioBaoCao = const TimeOfDay(hour: 8, minute: 0);
  bool _dangLuu = false;

  @override
  void initState() {
    super.initState();
    final u = context.read<AuthProvider>().nguoiDung;
    if (u != null) {
      _giaoDienToi = u.giaoDienToi;
      _batBaoCaoTaiChinh = u.batBaoCaoTaiChinh;
      _batNhacNhoNhapLieu = u.batNhacNhoNhapLieu;
      _tanSuat = u.tanSuatBaoCao;
      final parts = u.gioBaoCao.split(':');
      _gioBaoCao = TimeOfDay(hour: int.tryParse(parts[0]) ?? 8, minute: int.tryParse(parts.length > 1 ? parts[1] : '0') ?? 0);
    }
  }

  Future<void> _luuGiaoDien() async {
    setState(() => _dangLuu = true);
    try {
      await SettingsService.capNhatGiaoDien(giaoDienToi: _giaoDienToi);
      if (!mounted) return;
      await context.read<AuthProvider>().lamMoiThongTinNguoiDung();
      if (mounted) hienThongBao(context, "Đã lưu thay đổi giao diện!");
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  Future<void> _luuThongBao() async {
    setState(() => _dangLuu = true);
    try {
      await SettingsService.capNhatThongBao(
        batBaoCaoTaiChinh: _batBaoCaoTaiChinh,
        batNhacNhoNhapLieu: _batNhacNhoNhapLieu,
        tanSuatBaoCao: _tanSuat,
        gioBaoCao: "${_gioBaoCao.hour.toString().padLeft(2, '0')}:${_gioBaoCao.minute.toString().padLeft(2, '0')}",
      );
      // Quan trọng: làm mới lại thông tin người dùng đã lưu trong app, nếu không
      // lần sau mở lại màn Cài đặt sẽ hiển thị giá trị CŨ (trước khi lưu) cho tới khi đăng xuất/đăng nhập lại.
      if (mounted) await context.read<AuthProvider>().lamMoiThongTinNguoiDung();
      if (mounted) hienThongBao(context, "Đã lưu cài đặt thông báo!");
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Cài đặt")),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Text("Giao diện & hiển thị", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          SwitchListTile(
            title: const Text("Giao diện tối (Dark mode)"),
            value: _giaoDienToi,
            onChanged: (v) {
              setState(() => _giaoDienToi = v);
              context.read<ThemeProvider>().dat(v); // áp dụng ngay lập tức, không cần chờ mạng
              _luuGiaoDien();
            },
          ),
          const Divider(height: 32),
          const Text("Thông báo & nhắc nhở", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          SwitchListTile(
            title: const Text("Bật báo cáo tài chính định kỳ"),
            value: _batBaoCaoTaiChinh,
            onChanged: (v) => setState(() => _batBaoCaoTaiChinh = v),
          ),
          SwitchListTile(
            title: const Text("Bật nhắc nhở nhập liệu"),
            value: _batNhacNhoNhapLieu,
            onChanged: (v) => setState(() => _batNhacNhoNhapLieu = v),
          ),
          ListTile(
            title: const Text("Tần suất báo cáo"),
            trailing: DropdownButton<TanSuatBaoCao>(
              value: _tanSuat,
              items: TanSuatBaoCao.values.map((t) => DropdownMenuItem(value: t, child: Text(t.nhan))).toList(),
              onChanged: (v) => setState(() => _tanSuat = v!),
            ),
          ),
          ListTile(
            title: const Text("Giờ nhận báo cáo"),
            trailing: TextButton(
              onPressed: () async {
                final chon = await showTimePicker(context: context, initialTime: _gioBaoCao);
                if (chon != null) setState(() => _gioBaoCao = chon);
              },
              child: Text(_gioBaoCao.format(context)),
            ),
          ),
          const SizedBox(height: 16),
          ElevatedButton(
            onPressed: _dangLuu ? null : _luuThongBao,
            style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
            child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Lưu cài đặt thông báo"),
          ),
        ],
      ),
    );
  }
}

class ContactScreen extends StatefulWidget {
  const ContactScreen({super.key});
  @override
  State<ContactScreen> createState() => _ContactScreenState();
}

class _ContactScreenState extends State<ContactScreen> {
  final _formKey = GlobalKey<FormState>();
  final _hoTenCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _noiDungCtrl = TextEditingController();
  bool _dangGui = false;

  @override
  void initState() {
    super.initState();
    final u = context.read<AuthProvider>().nguoiDung;
    if (u != null) {
      _hoTenCtrl.text = u.hoTen ?? '';
      _emailCtrl.text = u.email ?? '';
    }
  }

  Future<void> _gui() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangGui = true);
    try {
      await ContactService.gui(_hoTenCtrl.text.trim(), _emailCtrl.text.trim(), _noiDungCtrl.text.trim());
      if (mounted) {
        hienThongBao(context, "Đã gửi liên hệ, cảm ơn bạn!");
        Navigator.of(context).pop();
      }
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangGui = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Liên hệ / Góp ý")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _hoTenCtrl,
                decoration: const InputDecoration(labelText: "Họ tên", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập họ tên" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: "Email", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập email" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _noiDungCtrl,
                maxLines: 5,
                decoration: const InputDecoration(labelText: "Nội dung liên hệ / góp ý", border: OutlineInputBorder(), alignLabelWithHint: true),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập nội dung" : null,
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _dangGui ? null : _gui,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangGui ? const CircularProgressIndicator(color: Colors.white) : const Text("Gửi liên hệ"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
