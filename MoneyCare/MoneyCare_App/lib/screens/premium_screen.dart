import 'dart:async';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../providers/auth_provider.dart';
import '../widgets/common_widgets.dart';

class PremiumScreen extends StatefulWidget {
  const PremiumScreen({super.key});
  @override
  State<PremiumScreen> createState() => _PremiumScreenState();
}

class _PremiumScreenState extends State<PremiumScreen> {
  PremiumPricing? _bangGia;
  PremiumOrderModel? _donHang;
  Uint8List? _anhQrBytes;
  bool _dangTai = true;
  bool _dangTaoAnhQr = false;
  int _soThang = 1;

  // ==== ZaloPay UAT - thanh toán tự động, không cần bấm xác nhận ====
  ZaloPayDonHangModel? _donZaloPay;
  Uint8List? _anhQrZaloPayBytes;
  bool _dangTaoZaloPay = false;
  Timer? _pollingZaloPay;

  @override
  void initState() {
    super.initState();
    _taiBangGia();
  }

  @override
  void dispose() {
    _pollingZaloPay?.cancel();
    super.dispose();
  }

  Future<void> _taiBangGia() async {
    setState(() => _dangTai = true);
    try {
      final bg = await PremiumService.bangGia();
      setState(() => _bangGia = bg);
    } catch (e) {
      if (mounted) hienThongBao(context, "Không tải được bảng giá", loi: true);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _dangKy() async {
    try {
      final don = await PremiumService.dangKyNangCap(_soThang);
      setState(() {
        _donHang = don;
        _anhQrBytes = null;
      });
      _taiAnhQr(don);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  Future<void> _taiAnhQr(PremiumOrderModel don) async {
    setState(() => _dangTaoAnhQr = true);
    try {
      final duongDan = PaymentService.duongDanAnhQr(don.soTaiKhoan, don.binNganHang, don.soTien, don.maGiaoDich);
      final bytes = await ApiClient.getBytes(duongDan);
      setState(() => _anhQrBytes = Uint8List.fromList(bytes));
    } catch (_) {
      // Không sao nếu QR lỗi - người dùng vẫn có thể chuyển khoản thủ công bằng
      // số tài khoản + nội dung hiển thị ở trên
    } finally {
      setState(() => _dangTaoAnhQr = false);
    }
  }

  Future<void> _xacNhanThanhToan() async {
    if (_donHang == null) return;
    try {
      final tb = await PremiumService.xacNhanThanhToan(_donHang!.id);
      if (!mounted) return;
      hienThongBao(context, tb);
      final auth = context.read<AuthProvider>();
      await auth.lamMoiThongTinNguoiDung();
      if (!mounted) return;
      setState(() {
        _donHang = null;
        _anhQrBytes = null;
      });
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  // ==== ZaloPay UAT: tạo đơn -> hiện QR -> tự động polling -> tự mở Premium khi thanh toán xong ====
  Future<void> _dangKyZaloPay() async {
    setState(() => _dangTaoZaloPay = true);
    try {
      final don = await ZaloPayService.taoDonPremium(_soThang);
      final bytes = await ApiClient.getBytes(don.urlAnhQr);
      setState(() {
        _donZaloPay = don;
        _anhQrZaloPayBytes = Uint8List.fromList(bytes);
      });
      _batDauPollingZaloPay(don.appTransId);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangTaoZaloPay = false);
    }
  }

  void _batDauPollingZaloPay(String appTransId) {
    _pollingZaloPay?.cancel();
    _pollingZaloPay = Timer.periodic(const Duration(seconds: 3), (timer) async {
      try {
        final tt = await ZaloPayService.trangThai(appTransId);
        if (tt.trangThai == ZaloPayTrangThai.thanhCong) {
          timer.cancel();
          if (!mounted) return;
          hienThongBao(context, "🎉 Thanh toán ZaloPay thành công! Premium đã được kích hoạt tự động.");
          final auth = context.read<AuthProvider>();
          await auth.lamMoiThongTinNguoiDung();
          if (!mounted) return;
          setState(() {
            _donZaloPay = null;
            _anhQrZaloPayBytes = null;
          });
        } else if (tt.trangThai == ZaloPayTrangThai.thatBai) {
          timer.cancel();
          if (!mounted) return;
          hienThongBao(context, "Thanh toán ZaloPay thất bại hoặc đơn đã hết hạn.", loi: true);
          setState(() {
            _donZaloPay = null;
            _anhQrZaloPayBytes = null;
          });
        }
      } catch (_) {
        // Bỏ qua lỗi tạm thời của 1 lần polling (VD mất mạng thoáng qua), thử lại ở lần sau
      }
    });
  }

  void _huyZaloPay() {
    _pollingZaloPay?.cancel();
    setState(() {
      _donZaloPay = null;
      _anhQrZaloPayBytes = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final nguoiDung = context.watch<AuthProvider>().nguoiDung;

    return Scaffold(
      appBar: AppBar(title: const Text("Nâng cấp Premium")),
      body: _dangTai
          ? const DangTai()
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (nguoiDung?.premiumConHieuLuc == true)
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(color: Colors.amber.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(16)),
                    child: Row(
                      children: [
                        const Icon(Icons.workspace_premium, color: Colors.amber, size: 32),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text("Tài khoản của bạn đang là Premium, hiệu lực đến ${nguoiDung!.premiumHetHan != null ? ngayVn(nguoiDung.premiumHetHan!) : ''}"),
                        ),
                      ],
                    ),
                  )
                else ...[
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(gradient: const LinearGradient(colors: [Color(0xFFFACC15), Color(0xFFF59E0B)]), borderRadius: BorderRadius.circular(16)),
                    child: Column(
                      children: [
                        const Icon(Icons.workspace_premium, color: Colors.white, size: 40),
                        const SizedBox(height: 8),
                        Text("${(_bangGia?.giaThang ?? 0).toStringAsFixed(0)} ₫ / tháng", style: const TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold)),
                        const SizedBox(height: 4),
                        const Text("Mở khóa OCR, QR, Insights AI, Hộ gia đình, Xuất báo cáo...", style: TextStyle(color: Colors.white70), textAlign: TextAlign.center),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Text("Chọn số tháng đăng ký", style: TextStyle(fontWeight: FontWeight.bold)),
                  Wrap(
                    spacing: 8,
                    children: [1, 3, 6, 12].map((t) {
                      return ChoiceChip(
                        label: Text("$t tháng"),
                        selected: _soThang == t,
                        onSelected: (_) => setState(() => _soThang = t),
                      );
                    }).toList(),
                  ),
                  const SizedBox(height: 16),
                  if (_donHang == null && _donZaloPay == null) ...[
                    ElevatedButton.icon(
                      onPressed: _dangTaoZaloPay ? null : _dangKyZaloPay,
                      icon: _dangTaoZaloPay
                          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                          : const Icon(Icons.qr_code),
                      label: const Text("Thanh toán qua ZaloPay (UAT) - Tự động kích hoạt"),
                      style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton(
                      onPressed: _dangKy,
                      style: OutlinedButton.styleFrom(padding: const EdgeInsets.all(16)),
                      child: const Text("Chuyển khoản ngân hàng (xác nhận thủ công)"),
                    ),
                  ],
                  if (_donZaloPay != null) ...[
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(border: Border.all(color: mauChinh), borderRadius: BorderRadius.circular(12)),
                      child: Column(
                        children: [
                          const Text("Quét mã bằng app ZaloPay (đăng nhập tài khoản UAT/sandbox)", textAlign: TextAlign.center),
                          const SizedBox(height: 8),
                          Text("${_donZaloPay!.soTien.toStringAsFixed(0)} ₫", style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: mauChi)),
                          const SizedBox(height: 12),
                          if (_anhQrZaloPayBytes != null) Image.memory(_anhQrZaloPayBytes!, width: 220, height: 220),
                          const SizedBox(height: 12),
                          const Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
                              SizedBox(width: 8),
                              Text("Đang chờ thanh toán - Premium sẽ tự động kích hoạt...", style: TextStyle(fontSize: 12)),
                            ],
                          ),
                          const SizedBox(height: 12),
                          TextButton(onPressed: _huyZaloPay, child: const Text("Hủy")),
                        ],
                      ),
                    ),
                  ],
                  if (_donHang != null) ...[
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(border: Border.all(color: Colors.grey.shade300), borderRadius: BorderRadius.circular(12)),
                      child: Column(
                        children: [
                          Text("Chuyển khoản ${_donHang!.soTien.toStringAsFixed(0)} ₫ tới:", textAlign: TextAlign.center),
                          const SizedBox(height: 8),
                          Text("${_donHang!.tenNganHangHienThi} - STK: ${_donHang!.soTaiKhoan}", style: const TextStyle(fontWeight: FontWeight.bold)),
                          const SizedBox(height: 4),
                          Text("Nội dung CK: ${_donHang!.maGiaoDich}", style: const TextStyle(fontWeight: FontWeight.bold, color: mauChi)),
                          const SizedBox(height: 12),
                          if (_dangTaoAnhQr)
                            const SizedBox(width: 200, height: 200, child: DangTai())
                          else if (_anhQrBytes != null)
                            Image.memory(_anhQrBytes!, width: 200, height: 200)
                          else
                            const Padding(
                              padding: EdgeInsets.all(12),
                              child: Text("Không tạo được ảnh QR - vui lòng chuyển khoản thủ công theo thông tin ở trên", textAlign: TextAlign.center, style: TextStyle(color: Colors.grey)),
                            ),
                          const SizedBox(height: 12),
                          ElevatedButton(
                            onPressed: _xacNhanThanhToan,
                            style: ElevatedButton.styleFrom(backgroundColor: mauThu, foregroundColor: Colors.white),
                            child: const Text("Tôi đã chuyển khoản - Xác nhận"),
                          ),
                        ],
                      ),
                    ),
                  ],
                ],
              ],
            ),
    );
  }
}
