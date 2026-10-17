import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class IntegrationsScreen extends StatefulWidget {
  const IntegrationsScreen({super.key});
  @override
  State<IntegrationsScreen> createState() => _IntegrationsScreenState();
}

class _IntegrationsScreenState extends State<IntegrationsScreen> {
  List<FinancialLinkModel> _ds = [];
  bool _dangTai = true;
  bool _khongPhaiPremium = false;

  @override
  void initState() {
    super.initState();
    _taiDuLieu();
  }

  Future<void> _taiDuLieu() async {
    setState(() {
      _dangTai = true;
      _khongPhaiPremium = false;
    });
    try {
      final ds = await IntegrationService.danhSach();
      setState(() => _ds = ds);
    } on ApiException catch (e) {
      if (e.statusCode == 402) setState(() => _khongPhaiPremium = true);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _themLienKet() async {
    NhaCungCap nhaCungCap = NhaCungCap.nganHang;
    final tenCtrl = TextEditingController();
    final soTkCtrl = TextEditingController();

    final ketQua = await showDialog<String>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text("Liên kết ngân hàng / ví"),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<NhaCungCap>(
                initialValue: nhaCungCap,
                items: NhaCungCap.values.map((n) => DropdownMenuItem(value: n, child: Text(n.nhan))).toList(),
                onChanged: (v) => setDialogState(() => nhaCungCap = v!),
                decoration: const InputDecoration(labelText: "Nhà cung cấp"),
              ),
              if (nhaCungCap == NhaCungCap.zaloPay) ...[
                const SizedBox(height: 8),
                const Text(
                  "ZaloPay được liên kết THẬT qua môi trường UAT: bạn sẽ quét mã QR và thanh toán "
                  "1.000đ bằng app ZaloPay (đăng nhập tài khoản sandbox) để xác thực quyền sở hữu.",
                  style: TextStyle(fontSize: 12, color: Colors.grey),
                ),
              ] else ...[
                TextField(controller: tenCtrl, decoration: const InputDecoration(labelText: "Tên gợi nhớ")),
                TextField(controller: soTkCtrl, decoration: const InputDecoration(labelText: "Số tài khoản / SĐT")),
              ],
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text("Hủy")),
            ElevatedButton(
              onPressed: () => Navigator.pop(ctx, nhaCungCap == NhaCungCap.zaloPay ? 'zalopay' : 'thu-cong'),
              child: Text(nhaCungCap == NhaCungCap.zaloPay ? "Tạo mã QR" : "Lưu"),
            ),
          ],
        ),
      ),
    );

    if (ketQua == 'thu-cong') {
      try {
        await IntegrationService.tao(nhaCungCap, tenCtrl.text.trim(), soTkCtrl.text.trim(), null);
        _taiDuLieu();
      } on ApiException catch (e) {
        if (mounted) hienThongBao(context, e.message, loi: true);
      }
    } else if (ketQua == 'zalopay') {
      await _lienKetZaloPay();
    }
  }

  // Tạo đơn xác thực ZaloPay UAT thật (1.000đ), hiện QR + tự động polling trạng thái thanh toán
  Future<void> _lienKetZaloPay() async {
    ZaloPayDonHangModel don;
    try {
      don = await ZaloPayService.taoDonLienKet();
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
      return;
    }

    List<int>? anhBytesRaw;
    try {
      anhBytesRaw = await ApiClient.getBytes(don.urlAnhQr);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
      return;
    }
    final anhBytes = Uint8List.fromList(anhBytesRaw);

    if (!mounted) return;
    bool dangDong = false;
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          // Bắt đầu polling ngay khi dialog mở (chỉ 1 lần)
          Future<void> polling() async {
            while (!dangDong) {
              await Future.delayed(const Duration(seconds: 3));
              if (dangDong) return;
              try {
                final tt = await ZaloPayService.trangThai(don.appTransId);
                if (tt.trangThai == ZaloPayTrangThai.thanhCong) {
                  dangDong = true;
                  if (ctx.mounted) Navigator.pop(ctx);
                  if (mounted) {
                    hienThongBao(context, "Đã xác thực và liên kết ZaloPay (UAT) thành công!");
                    _taiDuLieu();
                  }
                  return;
                } else if (tt.trangThai == ZaloPayTrangThai.thatBai) {
                  dangDong = true;
                  if (ctx.mounted) Navigator.pop(ctx);
                  if (mounted) hienThongBao(context, "Xác thực ZaloPay thất bại hoặc đã hết hạn.", loi: true);
                  return;
                }
              } catch (_) {
                // Bỏ qua lỗi tạm thời của 1 lần polling, thử lại ở vòng lặp tiếp theo
              }
            }
          }

          // ignore: discarded_futures
          polling();

          return AlertDialog(
            title: const Text("Quét mã QR ZaloPay (UAT)"),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Image.memory(anhBytes, width: 240, height: 240),
                const SizedBox(height: 12),
                Text("${don.soTien.toStringAsFixed(0)} đ", style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.red)),
                const SizedBox(height: 12),
                const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)),
                    SizedBox(width: 8),
                    Text("Đang chờ thanh toán...", style: TextStyle(fontSize: 12)),
                  ],
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () {
                  dangDong = true;
                  Navigator.pop(ctx);
                },
                child: const Text("Đóng"),
              ),
            ],
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Liên kết Ngân hàng / Ví")),
      body: _dangTai
          ? const DangTai()
          : _khongPhaiPremium
              ? ChanPremium(tenTinhNang: "Liên kết Ngân hàng / Ví điện tử", onThuLai: _taiDuLieu)
              : Column(
                  children: [
                    Container(
                      margin: const EdgeInsets.all(12),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(color: Colors.blue.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(12)),
                      child: const Text(
                        "Lưu ý: Ngân hàng và MoMo hiện chỉ hỗ trợ nhập tay (demo) - cần hợp đồng đối tác chính thức để kết nối API thật. "
                        "Riêng ZaloPay đã liên kết THẬT qua môi trường UAT/sandbox.",
                        style: TextStyle(fontSize: 12),
                      ),
                    ),
                    Expanded(
                      child: _ds.isEmpty
                          ? const Center(child: Text("Chưa có liên kết nào"))
                          : ListView.builder(
                              padding: const EdgeInsets.all(12),
                              itemCount: _ds.length,
                              itemBuilder: (_, i) {
                                final l = _ds[i];
                                return Card(
                                  margin: const EdgeInsets.only(bottom: 8),
                                  child: ListTile(
                                    leading: const Icon(Icons.account_balance, color: mauChinh),
                                    title: Text(l.tenGoiNho),
                                    subtitle: Text("${l.nhaCungCap.nhan} • ${l.soTaiKhoanAn}"),
                                    trailing: PopupMenuButton<String>(
                                      onSelected: (v) async {
                                        if (v == 'dong-bo') {
                                          try {
                                            final tb = await IntegrationService.dongBo(l.id);
                                            if (mounted) hienThongBao(context, tb);
                                          } on ApiException catch (e) {
                                            if (mounted) hienThongBao(context, e.message, loi: true);
                                          }
                                        } else if (v == 'huy') {
                                          await IntegrationService.huy(l.id);
                                          _taiDuLieu();
                                        }
                                      },
                                      itemBuilder: (_) => [
                                        const PopupMenuItem(value: 'dong-bo', child: Text("Đồng bộ ngay")),
                                        const PopupMenuItem(value: 'huy', child: Text("Hủy liên kết")),
                                      ],
                                    ),
                                  ),
                                );
                              },
                            ),
                    ),
                  ],
                ),
      floatingActionButton: _khongPhaiPremium
          ? null
          : FloatingActionButton(backgroundColor: mauChinh, onPressed: _themLienKet, child: const Icon(Icons.add, color: Colors.white)),
    );
  }
}
