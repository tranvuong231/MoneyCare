import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../services/services.dart';
import '../models/models.dart';
import '../widgets/common_widgets.dart';
import 'notifications_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});
  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  Map<String, dynamic>? _duLieu;
  bool _dangTai = true;
  String? _loi;

  @override
  void initState() {
    super.initState();
    _taiDuLieu();
  }

  Future<void> _taiDuLieu() async {
    setState(() {
      _dangTai = true;
      _loi = null;
    });
    try {
      final now = DateTime.now();
      final dauThang = DateTime(now.year, now.month, 1);
      final res = await TransactionService.danhSach(tuNgay: dauThang, denNgay: now);
      setState(() => _duLieu = res);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final nguoiDung = context.watch<AuthProvider>().nguoiDung;

    return Scaffold(
      appBar: AppBar(
        title: Text("Xin chào, ${nguoiDung?.hoTen ?? nguoiDung?.email ?? ''} 👋"),
        actions: [
          if (nguoiDung?.premiumConHieuLuc == true)
            const Padding(padding: EdgeInsets.only(right: 4), child: Center(child: HuyHieuPremium())),
          _ChuongThongBao(),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _taiDuLieu,
        child: _dangTai
            ? const DangTai()
            : _loi != null
                ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
                : _noiDung(context),
      ),
    );
  }

  Widget _noiDung(BuildContext context) {
    final tongThu = _duLieu!['tongThu'] as double;
    final tongChi = _duLieu!['tongChi'] as double;
    final soDu = tongThu - tongChi;
    final ds = (_duLieu!['danhSach'] as List<TransactionModel>).take(6).toList();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [mauChinh, Color(0xFF0D9488)], begin: Alignment.topLeft, end: Alignment.bottomRight),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text("Số dư tháng này", style: TextStyle(color: Colors.white70)),
              const SizedBox(height: 4),
              Text(tienVnd(soDu), style: const TextStyle(color: Colors.white, fontSize: 30, fontWeight: FontWeight.bold)),
            ],
          ),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(child: TheThongKe(tieuDe: "Tổng thu", giaTri: tienVnd(tongThu), icon: Icons.arrow_downward, mau: mauThu)),
            const SizedBox(width: 12),
            Expanded(child: TheThongKe(tieuDe: "Tổng chi", giaTri: tienVnd(tongChi), icon: Icons.arrow_upward, mau: mauChi)),
          ],
        ),
        const SizedBox(height: 20),
        const Text("Truy cập nhanh", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 4,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 8,
          crossAxisSpacing: 8,
          children: [
            _oTruyCapNhanh(context, Icons.receipt_long, "Giao dịch", '/transactions'),
            _oTruyCapNhanh(context, Icons.category, "Danh mục", '/categories'),
            _oTruyCapNhanh(context, Icons.event_note, "Hóa đơn", '/bills'),
            _oTruyCapNhanh(context, Icons.bar_chart, "Báo cáo", '/reports'),
            _oTruyCapNhanh(context, Icons.camera_alt, "Quét hóa đơn", '/ocr'),
            _oTruyCapNhanh(context, Icons.qr_code, "QR chuyển khoản", '/payment'),
            _oTruyCapNhanh(context, Icons.groups, "Hộ gia đình", '/household'),
            _oTruyCapNhanh(context, Icons.smart_toy, "Trợ lý AI", '/chatbot'),
          ],
        ),
        const SizedBox(height: 20),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text("Giao dịch gần đây", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            TextButton(onPressed: () => Navigator.of(context).pushNamed('/transactions'), child: const Text("Xem tất cả")),
          ],
        ),
        if (ds.isEmpty) const Padding(padding: EdgeInsets.all(16), child: Text("Chưa có giao dịch nào trong tháng này")),
        ...ds.map((t) => Card(
              margin: const EdgeInsets.only(bottom: 8),
              child: ListTile(
                leading: CircleAvatar(
                  backgroundColor: mauTuHex(t.mauDanhMuc ?? '#0d6efd').withValues(alpha: 0.15),
                  child: Icon(t.loai == LoaiGiaoDich.thu ? Icons.arrow_downward : Icons.arrow_upward,
                      color: mauTuHex(t.mauDanhMuc ?? '#0d6efd'), size: 18),
                ),
                title: Text(t.tenDanhMuc ?? "Không có danh mục"),
                subtitle: Text(
                  "${t.thoiGianTao != null ? gioNgayVn(t.thoiGianTao!) : ngayVn(t.ngayGiaoDich)}"
                  "${t.ghiChu != null && t.ghiChu!.isNotEmpty ? ' • ${t.ghiChu}' : ''}",
                ),
                trailing: Text(
                  "${t.loai == LoaiGiaoDich.thu ? '+' : '-'}${tienVnd(t.soTien)}",
                  style: TextStyle(fontWeight: FontWeight.bold, color: t.loai == LoaiGiaoDich.thu ? mauThu : mauChi),
                ),
              ),
            )),
      ],
    );
  }

  Widget _oTruyCapNhanh(BuildContext context, IconData icon, String nhan, String route) {
    return InkWell(
      onTap: () async {
        await Navigator.of(context).pushNamed(route);
        // Tải lại dữ liệu Trang chủ khi quay về - phòng trường hợp màn hình vừa mở (quét hóa đơn,
        // thêm giao dịch, đánh dấu hóa đơn đã thanh toán...) làm thay đổi số liệu Tổng thu/chi hoặc
        // danh sách "Giao dịch gần đây", để không phải kéo-để-làm-mới thủ công mới thấy cập nhật.
        if (mounted) _taiDuLieu();
      },
      borderRadius: BorderRadius.circular(16),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          CircleAvatar(radius: 22, backgroundColor: mauChinh.withValues(alpha: 0.1), child: Icon(icon, color: mauChinh, size: 20)),
          const SizedBox(height: 4),
          Text(nhan, textAlign: TextAlign.center, style: const TextStyle(fontSize: 10)),
        ],
      ),
    );
  }
}

// Chuông thông báo hiển thị trên Trang chủ, kèm số nhỏ báo số lượng chưa xem.
// Tự tải lại số chưa xem mỗi khi quay lại màn hình này (VD sau khi mở màn Thông báo rồi bấm Back).
class _ChuongThongBao extends StatefulWidget {
  const _ChuongThongBao();
  @override
  State<_ChuongThongBao> createState() => _ChuongThongBaoState();
}

class _ChuongThongBaoState extends State<_ChuongThongBao> {
  int _soChuaXem = 0;

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    try {
      final so = await ThongBaoService.soChuaXem();
      if (mounted) setState(() => _soChuaXem = so);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      alignment: Alignment.center,
      children: [
        IconButton(
          icon: const Icon(Icons.notifications_outlined),
          onPressed: () async {
            await Navigator.of(context).push(MaterialPageRoute(builder: (_) => const NotificationsScreen()));
            _tai();
          },
        ),
        if (_soChuaXem > 0)
          Positioned(
            right: 6,
            top: 6,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
              constraints: const BoxConstraints(minWidth: 16),
              decoration: BoxDecoration(color: mauChi, borderRadius: BorderRadius.circular(8)),
              child: Text(
                _soChuaXem > 99 ? "99+" : "$_soChuaXem",
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold),
              ),
            ),
          ),
      ],
    );
  }
}
