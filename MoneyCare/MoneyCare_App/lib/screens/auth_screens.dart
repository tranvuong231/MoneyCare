import 'package:flutter/material.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/api_client.dart';
import '../services/services.dart';
import '../widgets/common_widgets.dart';

// Web Client ID lấy từ Google Cloud Console (Credentials -> OAuth 2.0 Client IDs -> Web application) -
// PHẢI trùng với Authentication:Google:ClientId trong cấu hình của backend.
// Không hard-code vào mã nguồn; truyền khi chạy:
//   flutter run --dart-define=GOOGLE_WEB_CLIENT_ID=xxxx.apps.googleusercontent.com
// Xem docs/SETUP.md. Để trống thì nút Google Sign-In sẽ không hoạt động.
const String _googleWebClientId = String.fromEnvironment('GOOGLE_WEB_CLIENT_ID');

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});
  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailCtrl = TextEditingController();
  final _matKhauCtrl = TextEditingController();
  bool _dangXuLy = false;
  bool _anMatKhau = true;

  Future<void> _dangNhap() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangXuLy = true);
    try {
      await context.read<AuthProvider>().dangNhap(_emailCtrl.text.trim(), _matKhauCtrl.text);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } catch (e) {
      if (mounted) hienThongBao(context, "Không kết nối được tới máy chủ. Kiểm tra lại địa chỉ API trong lib/config/api_config.dart", loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  Future<void> _dangNhapGoogle() async {
    setState(() => _dangXuLy = true);
    try {
      final googleSignIn = GoogleSignIn(serverClientId: _googleWebClientId.isEmpty ? null : _googleWebClientId, scopes: ['email']);
      final taiKhoan = await googleSignIn.signIn();
      if (taiKhoan == null) {
        setState(() => _dangXuLy = false);
        return; // Người dùng bấm hủy
      }
      final xacThuc = await taiKhoan.authentication;
      final idToken = xacThuc.idToken;
      if (idToken == null) {
        throw ApiException("Không lấy được mã xác thực từ Google");
      }
      if (!mounted) return;
      await context.read<AuthProvider>().dangNhapGoogle(idToken);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } catch (e) {
      debugPrint("Lỗi đăng nhập Google: $e");
      if (mounted) hienThongBao(context, "Đăng nhập Google thất bại, vui lòng thử lại", loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SizedBox(height: 24),
                  const Icon(Icons.savings_rounded, size: 72, color: mauChinh),
                  const SizedBox(height: 12),
                  const Text("MoneyCare", textAlign: TextAlign.center, style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold, color: mauChinh)),
                  const Text("Quản lý tài chính cá nhân", textAlign: TextAlign.center, style: TextStyle(color: Colors.grey)),
                  const SizedBox(height: 32),
                  TextFormField(
                    controller: _emailCtrl,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: "Email", prefixIcon: Icon(Icons.email_outlined), border: OutlineInputBorder()),
                    validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập email" : null,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _matKhauCtrl,
                    obscureText: _anMatKhau,
                    decoration: InputDecoration(
                      labelText: "Mật khẩu",
                      prefixIcon: const Icon(Icons.lock_outline),
                      border: const OutlineInputBorder(),
                      suffixIcon: IconButton(
                        icon: Icon(_anMatKhau ? Icons.visibility_off : Icons.visibility),
                        onPressed: () => setState(() => _anMatKhau = !_anMatKhau),
                      ),
                    ),
                    validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập mật khẩu" : null,
                  ),
                  const SizedBox(height: 24),
                  ElevatedButton(
                    onPressed: _dangXuLy ? null : _dangNhap,
                    style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                    child: _dangXuLy ? const CircularProgressIndicator(color: Colors.white) : const Text("Đăng nhập", style: TextStyle(fontSize: 16)),
                  ),
                  const SizedBox(height: 16),
                  Row(children: [
                    Expanded(child: Divider(color: Colors.grey.shade300)),
                    const Padding(padding: EdgeInsets.symmetric(horizontal: 8), child: Text("hoặc", style: TextStyle(color: Colors.grey))),
                    Expanded(child: Divider(color: Colors.grey.shade300)),
                  ]),
                  const SizedBox(height: 16),
                  OutlinedButton.icon(
                    onPressed: _dangXuLy ? null : _dangNhapGoogle,
                    icon: const Icon(Icons.g_mobiledata, size: 28, color: Colors.red),
                    label: const Text("Đăng nhập với Google"),
                    style: OutlinedButton.styleFrom(padding: const EdgeInsets.all(14), side: BorderSide(color: Colors.grey.shade300)),
                  ),
                  const SizedBox(height: 16),
                  TextButton(
                    onPressed: () => Navigator.of(context).pushNamed('/dang-ky'),
                    child: const Text("Chưa có tài khoản? Đăng ký ngay"),
                  ),
                  TextButton(
                    onPressed: () => Navigator.of(context).pushNamed('/quen-mat-khau'),
                    child: const Text("Quên mật khẩu?"),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});
  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _hoTenCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _matKhauCtrl = TextEditingController();
  bool _dangXuLy = false;

  Future<void> _dangKy() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangXuLy = true);
    try {
      await context.read<AuthProvider>().dangKy(_hoTenCtrl.text.trim(), _emailCtrl.text.trim(), _matKhauCtrl.text);
      // QUAN TRỌNG: màn Đăng ký được "push" chồng lên trên màn Đăng nhập, nên dù trạng thái đăng
      // nhập đã đổi, Flutter KHÔNG tự đóng màn này lại - phải tự đóng (pop) để lộ ra Trang chủ
      // đã đăng nhập bên dưới. Thiếu dòng này là nguyên nhân "đăng ký xong không thấy gì xảy ra".
      if (mounted) Navigator.of(context).popUntil((route) => route.isFirst);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } catch (e) {
      if (mounted) hienThongBao(context, "Không kết nối được tới máy chủ", loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Đăng ký tài khoản")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _hoTenCtrl,
                decoration: const InputDecoration(labelText: "Họ và tên", prefixIcon: Icon(Icons.person_outline), border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập họ tên" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _emailCtrl,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(labelText: "Email", prefixIcon: Icon(Icons.email_outlined), border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập email" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _matKhauCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: "Mật khẩu (tối thiểu 6 ký tự)", prefixIcon: Icon(Icons.lock_outline), border: OutlineInputBorder()),
                validator: (v) => (v == null || v.length < 6) ? "Mật khẩu tối thiểu 6 ký tự" : null,
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _dangXuLy ? null : _dangKy,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangXuLy ? const CircularProgressIndicator(color: Colors.white) : const Text("Đăng ký", style: TextStyle(fontSize: 16)),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});
  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _emailCtrl = TextEditingController();
  final _maCtrl = TextEditingController();
  final _matKhauMoiCtrl = TextEditingController();
  bool _daGuiMa = false;
  bool _dangXuLy = false;
  String? _maDemo;

  Future<void> _guiMa() async {
    if (_emailCtrl.text.trim().isEmpty) {
      hienThongBao(context, "Vui lòng nhập email", loi: true);
      return;
    }
    setState(() => _dangXuLy = true);
    try {
      final res = await AuthService.quenMatKhau(_emailCtrl.text.trim());
      setState(() {
        _daGuiMa = true;
        _maDemo = res['maDemo'];
      });
      if (mounted) hienThongBao(context, res['thongBao'] ?? "Đã gửi mã đặt lại mật khẩu");
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangXuLy = false);
    }
  }

  Future<void> _datLaiMatKhau() async {
    if (_maCtrl.text.trim().isEmpty || _matKhauMoiCtrl.text.length < 6) {
      hienThongBao(context, "Vui lòng nhập đầy đủ mã và mật khẩu mới (tối thiểu 6 ký tự)", loi: true);
      return;
    }
    setState(() => _dangXuLy = true);
    try {
      await AuthService.datLaiMatKhau(_emailCtrl.text.trim(), _maCtrl.text.trim(), _matKhauMoiCtrl.text);
      if (mounted) {
        hienThongBao(context, "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.");
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
    return Scaffold(
      appBar: AppBar(title: const Text("Quên mật khẩu")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              "Nhập email đã đăng ký, hệ thống sẽ gửi mã đặt lại mật khẩu tới hộp thư của bạn.",
              style: TextStyle(color: Colors.grey),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _emailCtrl,
              enabled: !_daGuiMa,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(labelText: "Email", prefixIcon: Icon(Icons.email_outlined), border: OutlineInputBorder()),
            ),
            const SizedBox(height: 16),
            if (!_daGuiMa)
              ElevatedButton(
                onPressed: _dangXuLy ? null : _guiMa,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangXuLy ? const CircularProgressIndicator(color: Colors.white) : const Text("Gửi mã đặt lại mật khẩu"),
              )
            else ...[
              if (_maDemo != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 12),
                  decoration: BoxDecoration(color: Colors.orange.withValues(alpha:0.1), borderRadius: BorderRadius.circular(8)),
                  child: Text("Chế độ demo (chưa cấu hình SMTP thật) - Mã của bạn:\n$_maDemo", style: const TextStyle(color: Colors.orange)),
                ),
              TextField(
                controller: _maCtrl,
                decoration: const InputDecoration(labelText: "Mã đặt lại mật khẩu (dán từ email)", border: OutlineInputBorder()),
                maxLines: 2,
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _matKhauMoiCtrl,
                obscureText: true,
                decoration: const InputDecoration(labelText: "Mật khẩu mới (tối thiểu 6 ký tự)", border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _dangXuLy ? null : _datLaiMatKhau,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangXuLy ? const CircularProgressIndicator(color: Colors.white) : const Text("Đặt lại mật khẩu"),
              ),
              TextButton(
                onPressed: () => setState(() => _daGuiMa = false),
                child: const Text("Nhập sai email? Quay lại"),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
