import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

// Định dạng số tiền theo kiểu Việt Nam: 1.000.000 ₫
String tienVnd(double soTien) {
  final f = NumberFormat.decimalPattern('vi_VN');
  return "${f.format(soTien)} ₫";
}

String ngayVn(DateTime d) => DateFormat('dd/MM/yyyy').format(d);
String gioVn(DateTime d) => DateFormat('HH:mm').format(d);
String gioNgayVn(DateTime d) => DateFormat('HH:mm-dd/MM/yyyy').format(d);

Color mauTuHex(String hex) {
  var h = hex.replaceAll('#', '');
  if (h.length == 6) h = 'FF$h';
  try {
    return Color(int.parse(h, radix: 16));
  } catch (_) {
    return Colors.blue;
  }
}

const mauChinh = Color(0xFF14B8A6); // Xanh ngọc - màu thương hiệu MoneyCare
const mauThu = Color(0xFF16A34A); // Xanh lá - Thu
const mauChi = Color(0xFFDC2626); // Đỏ - Chi

class DangTai extends StatelessWidget {
  const DangTai({super.key});
  @override
  Widget build(BuildContext context) => const Center(child: CircularProgressIndicator(color: mauChinh));
}

class ThongBaoLoi extends StatelessWidget {
  final String noiDung;
  final VoidCallback? onThuLai;
  const ThongBaoLoi({super.key, required this.noiDung, this.onThuLai});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, color: Colors.redAccent, size: 48),
            const SizedBox(height: 12),
            Text(noiDung, textAlign: TextAlign.center, style: const TextStyle(color: Colors.black87)),
            if (onThuLai != null) ...[
              const SizedBox(height: 16),
              ElevatedButton(onPressed: onThuLai, child: const Text("Thử lại")),
            ]
          ],
        ),
      ),
    );
  }
}

void hienThongBao(BuildContext context, String noiDung, {bool loi = false}) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(noiDung),
      backgroundColor: loi ? Colors.red.shade600 : mauChinh,
      behavior: SnackBarBehavior.floating,
    ),
  );
}

class TheThongKe extends StatelessWidget {
  final String tieuDe;
  final String giaTri;
  final IconData icon;
  final Color mau;
  const TheThongKe({super.key, required this.tieuDe, required this.giaTri, required this.icon, required this.mau});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: mau.withOpacity(0.08),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: mau.withOpacity(0.2)),
      ),
      child: Row(
        children: [
          CircleAvatar(backgroundColor: mau.withOpacity(0.15), child: Icon(icon, color: mau, size: 20)),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tieuDe, style: TextStyle(fontSize: 12, color: Colors.grey.shade700)),
                const SizedBox(height: 2),
                Text(giaTri, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: mau)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// Huy hiệu Premium nhỏ dùng nhiều nơi (Profile, AppBar, danh sách tính năng khóa)
class HuyHieuPremium extends StatelessWidget {
  const HuyHieuPremium({super.key});
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [Color(0xFFFACC15), Color(0xFFF59E0B)]),
        borderRadius: BorderRadius.circular(20),
      ),
      child: const Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.workspace_premium, size: 14, color: Colors.white),
          SizedBox(width: 4),
          Text("PREMIUM", style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}

// Màn hình chặn tính năng Premium khi tài khoản chưa nâng cấp (dùng khi API trả lỗi 402)
class ChanPremium extends StatelessWidget {
  final String tenTinhNang;
  final VoidCallback? onThuLai;
  const ChanPremium({super.key, required this.tenTinhNang, this.onThuLai});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.lock, size: 56, color: Colors.amber),
            const SizedBox(height: 16),
            Text("$tenTinhNang là tính năng Premium",
                textAlign: TextAlign.center, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            const Text("Nâng cấp tài khoản để mở khóa tính năng này.", textAlign: TextAlign.center),
            const SizedBox(height: 20),
            ElevatedButton.icon(
              onPressed: () => Navigator.of(context).pushNamed('/premium'),
              icon: const Icon(Icons.workspace_premium),
              label: const Text("Nâng cấp ngay"),
              style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white),
            ),
            if (onThuLai != null) ...[
              const SizedBox(height: 8),
              TextButton.icon(
                onPressed: onThuLai,
                icon: const Icon(Icons.refresh),
                label: const Text("Vừa nâng cấp xong? Bấm để kiểm tra lại"),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
