import 'package:flutter/material.dart';
import 'dart:typed_data';
import '../services/services.dart';
import '../services/api_client.dart';
import '../config/ngan_hang.dart';
import '../widgets/common_widgets.dart';

class PaymentScreen extends StatefulWidget {
  const PaymentScreen({super.key});
  @override
  State<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _soTaiKhoanCtrl = TextEditingController();
  final _soTienCtrl = TextEditingController();
  final _noiDungCtrl = TextEditingController();
  NganHang _nganHang = danhSachNganHang.first;

  Uint8List? _anhQrBytes;
  bool _dangTao = false;
  String? _loi;

  Future<void> _taoQr() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _dangTao = true;
      _loi = null;
      _anhQrBytes = null;
    });
    try {
      final duongDan = PaymentService.duongDanAnhQr(
        _soTaiKhoanCtrl.text.trim(),
        _nganHang.bin,
        double.tryParse(_soTienCtrl.text.replaceAll('.', '').replaceAll(',', '')) ?? 0,
        _noiDungCtrl.text.trim(),
      );
      final bytes = await ApiClient.getBytes(duongDan);
      setState(() => _anhQrBytes = Uint8List.fromList(bytes));
    } on ApiException catch (e) {
      setState(() => _loi = e.message);
    } catch (e) {
      setState(() => _loi = "Không tạo được mã QR. Kiểm tra lại kết nối tới máy chủ.");
    } finally {
      setState(() => _dangTao = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Tạo mã QR chuyển khoản")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              DropdownButtonFormField<NganHang>(
                initialValue: _nganHang,
                isExpanded: true,
                decoration: const InputDecoration(labelText: "Ngân hàng thụ hưởng", border: OutlineInputBorder()),
                items: danhSachNganHang.map((n) => DropdownMenuItem(value: n, child: Text(n.ten))).toList(),
                onChanged: (v) => setState(() => _nganHang = v!),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _soTaiKhoanCtrl,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: "Số tài khoản ngân hàng", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập số tài khoản" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _soTienCtrl,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: "Số tiền (VNĐ) - để trống nếu muốn người chuyển tự nhập", border: OutlineInputBorder()),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _noiDungCtrl,
                decoration: const InputDecoration(labelText: "Nội dung chuyển khoản", border: OutlineInputBorder()),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: _dangTao ? null : _taoQr,
                icon: _dangTao ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : const Icon(Icons.qr_code),
                label: Text(_dangTao ? "Đang tạo..." : "Tạo mã QR"),
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
              ),
              if (_loi != null) ...[
                const SizedBox(height: 16),
                ThongBaoLoi(noiDung: _loi!, onThuLai: _taoQr),
              ],
              if (_anhQrBytes != null) ...[
                const SizedBox(height: 24),
                Center(
                  child: Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(border: Border.all(color: Colors.grey.shade300), borderRadius: BorderRadius.circular(12)),
                    child: Image.memory(_anhQrBytes!, width: 240, height: 240),
                  ),
                ),
                const SizedBox(height: 8),
                const Center(child: Text("Quét mã bằng app ngân hàng bất kỳ để chuyển khoản", style: TextStyle(color: Colors.grey))),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
