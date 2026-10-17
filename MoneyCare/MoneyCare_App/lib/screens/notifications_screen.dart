import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../widgets/common_widgets.dart';

IconData iconThongBao(LoaiThongBao loai) => switch (loai) {
      LoaiThongBao.hoaDon => Icons.event_note,
      LoaiThongBao.phanTichAI => Icons.auto_awesome,
      LoaiThongBao.baoCao => Icons.bar_chart,
      LoaiThongBao.xacMinh => Icons.verified,
      LoaiThongBao.hoSo => Icons.person,
      LoaiThongBao.hoGiaDinh => Icons.groups,
      LoaiThongBao.premium => Icons.workspace_premium,
      LoaiThongBao.heThong => Icons.notifications,
    };

Color mauThongBao(LoaiThongBao loai) => switch (loai) {
      LoaiThongBao.hoaDon => Colors.amber.shade800,
      LoaiThongBao.phanTichAI => mauChinh,
      LoaiThongBao.baoCao => Colors.blue,
      LoaiThongBao.xacMinh => mauThu,
      LoaiThongBao.hoSo => Colors.purple,
      LoaiThongBao.hoGiaDinh => Colors.teal,
      LoaiThongBao.premium => Colors.orange,
      LoaiThongBao.heThong => Colors.grey,
    };

String thoiGianTruoc(DateTime t) {
  final chenhLech = DateTime.now().difference(t);
  if (chenhLech.inMinutes < 1) return "Vừa xong";
  if (chenhLech.inMinutes < 60) return "${chenhLech.inMinutes} phút trước";
  if (chenhLech.inHours < 24) return "${chenhLech.inHours} giờ trước";
  if (chenhLech.inDays < 7) return "${chenhLech.inDays} ngày trước";
  return ngayVn(t);
}

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<ThongBaoModel> _ds = [];
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
      final res = await ThongBaoService.danhSach();
      setState(() => _ds = res['danhSach']);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _danhDauTatCa() async {
    await ThongBaoService.danhDauTatCaDaXem();
    _taiDuLieu();
  }

  Future<void> _bamVaoThongBao(ThongBaoModel tb) async {
    if (!tb.daXem) {
      await ThongBaoService.danhDauDaXem(tb.id);
    }
    if (tb.duongDanDieuHuong != null && mounted) {
      Navigator.of(context).pushNamed(tb.duongDanDieuHuong!);
    }
    _taiDuLieu();
  }

  @override
  Widget build(BuildContext context) {
    final coChuaXem = _ds.any((t) => !t.daXem);
    return Scaffold(
      appBar: AppBar(
        title: const Text("Thông báo"),
        actions: [
          if (coChuaXem)
            TextButton(
              onPressed: _danhDauTatCa,
              child: const Text("Đọc tất cả", style: TextStyle(color: Colors.white)),
            ),
        ],
      ),
      body: _dangTai
          ? const DangTai()
          : _loi != null
              ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
              : _ds.isEmpty
                  ? const Center(child: Text("Chưa có thông báo nào"))
                  : RefreshIndicator(
                      onRefresh: _taiDuLieu,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(12),
                        itemCount: _ds.length,
                        itemBuilder: (_, i) {
                          final tb = _ds[i];
                          final mau = mauThongBao(tb.loai);
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            elevation: tb.daXem ? 0 : 1,
                            // Material 3 tự pha thêm "surfaceTint" xám lên màu nền của Card, khiến màu
                            // nhạt (opacity thấp) bị ngả xám tối thay vì ra đúng tông màu thương hiệu.
                            // Tắt surfaceTintColor + tăng độ đậm màu nền để thông báo CHƯA ĐỌC nổi bật
                            // rõ ràng bằng màu xanh ngọc thương hiệu (mauChinh) thay vì xám.
                            surfaceTintColor: Colors.transparent,
                            color: tb.daXem ? Colors.white : mauChinh.withValues(alpha: 0.10),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                              side: tb.daXem
                                  ? BorderSide.none
                                  : BorderSide(color: mauChinh.withValues(alpha: 0.35), width: 1),
                            ),
                            child: ListTile(
                              leading: Stack(
                                children: [
                                  CircleAvatar(backgroundColor: mau.withValues(alpha: 0.15), child: Icon(iconThongBao(tb.loai), color: mau, size: 18)),
                                  if (!tb.daXem)
                                    Positioned(
                                      right: 0,
                                      top: 0,
                                      child: Container(width: 10, height: 10, decoration: const BoxDecoration(color: mauChi, shape: BoxShape.circle)),
                                    ),
                                ],
                              ),
                              title: Text(tb.tieuDe, style: TextStyle(fontWeight: tb.daXem ? FontWeight.normal : FontWeight.bold)),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(tb.noiDung, maxLines: 2, overflow: TextOverflow.ellipsis),
                                  const SizedBox(height: 2),
                                  Text(thoiGianTruoc(tb.ngayTao), style: const TextStyle(fontSize: 11, color: Colors.grey)),
                                ],
                              ),
                              isThreeLine: true,
                              trailing: PopupMenuButton<String>(
                                onSelected: (v) async {
                                  if (v == 'chua_xem') {
                                    await ThongBaoService.danhDauChuaXem(tb.id);
                                  } else if (v == 'xoa') {
                                    await ThongBaoService.xoa(tb.id);
                                  }
                                  _taiDuLieu();
                                },
                                itemBuilder: (_) => [
                                  if (tb.daXem) const PopupMenuItem(value: 'chua_xem', child: Text("Đánh dấu chưa đọc")),
                                  const PopupMenuItem(value: 'xoa', child: Text("Xóa thông báo")),
                                ],
                              ),
                              onTap: () => _bamVaoThongBao(tb),
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
