import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class HouseholdScreen extends StatefulWidget {
  const HouseholdScreen({super.key});
  @override
  State<HouseholdScreen> createState() => _HouseholdScreenState();
}

class _HouseholdScreenState extends State<HouseholdScreen> {
  HouseholdModel? _hoGiaDinh;
  bool _dangTai = true;
  String? _loi;
  bool _khongPhaiPremium = false;

  @override
  void initState() {
    super.initState();
    _taiDuLieu();
  }

  Future<void> _taiDuLieu() async {
    setState(() {
      _dangTai = true;
      _loi = null;
      _khongPhaiPremium = false;
    });
    try {
      final h = await HouseholdService.thongTin();
      setState(() => _hoGiaDinh = h);
    } on ApiException catch (e) {
      if (e.statusCode == 402) {
        setState(() => _khongPhaiPremium = true);
      } else {
        setState(() => _loi = e.message);
      }
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _hienDialogTao() async {
    final ctrl = TextEditingController();
    final ten = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Tạo hộ gia đình"),
        content: TextField(controller: ctrl, decoration: const InputDecoration(labelText: "Tên hộ gia đình")),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text("Hủy")),
          ElevatedButton(onPressed: () => Navigator.pop(context, ctrl.text), child: const Text("Tạo")),
        ],
      ),
    );
    if (ten == null || ten.trim().isEmpty) return;
    try {
      final tb = await HouseholdService.tao(ten.trim());
      if (mounted) hienThongBao(context, tb);
      _taiDuLieu();
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  Future<void> _hienDialogThamGia() async {
    final ctrl = TextEditingController();
    final maMoi = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Tham gia hộ gia đình"),
        content: TextField(controller: ctrl, decoration: const InputDecoration(labelText: "Nhập mã mời (6 ký tự)")),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text("Hủy")),
          ElevatedButton(onPressed: () => Navigator.pop(context, ctrl.text), child: const Text("Tham gia")),
        ],
      ),
    );
    if (maMoi == null || maMoi.trim().isEmpty) return;
    try {
      final tb = await HouseholdService.thamGia(maMoi.trim());
      if (mounted) hienThongBao(context, tb);
      _taiDuLieu();
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  Future<void> _roiHo() async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Rời hộ gia đình"),
        content: const Text("Bạn có chắc muốn rời hộ gia đình này?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Rời", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan == true) {
      try {
        await HouseholdService.roiHo();
        _taiDuLieu();
      } on ApiException catch (e) {
        if (mounted) hienThongBao(context, e.message, loi: true);
      }
    }
  }

  Future<void> _xoaHoGiaDinh() async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa hộ gia đình"),
        content: const Text("Xóa hộ gia đình sẽ đưa TẤT CẢ thành viên (kể cả bạn) về trạng thái không có hộ. Bạn có chắc chắn?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan == true) {
      try {
        await HouseholdService.xoaHoGiaDinh();
        if (mounted) hienThongBao(context, "Đã xóa hộ gia đình");
        _taiDuLieu();
      } on ApiException catch (e) {
        if (mounted) hienThongBao(context, e.message, loi: true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Hộ gia đình"),
        actions: [
          if (_hoGiaDinh != null)
            PopupMenuButton<String>(
              onSelected: (v) {
                if (v == 'xoa') {
                  _xoaHoGiaDinh();
                } else if (v == 'roi') {
                  _roiHo();
                }
              },
              itemBuilder: (_) => [
                if (_hoGiaDinh!.laChuHo)
                  const PopupMenuItem(value: 'xoa', child: Text("Xóa hộ gia đình", style: TextStyle(color: mauChi)))
                else
                  const PopupMenuItem(value: 'roi', child: Text("Rời hộ gia đình", style: TextStyle(color: mauChi))),
              ],
            ),
        ],
      ),
      body: _dangTai
          ? const DangTai()
          : _khongPhaiPremium
              ? ChanPremium(tenTinhNang: "Quản lý hộ gia đình", onThuLai: _taiDuLieu)
              : _loi != null
                  ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
                  : _hoGiaDinh == null
                      ? _chuaCoHo()
                      : _daCoHo(_hoGiaDinh!),
    );
  }

  Widget _chuaCoHo() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.groups, size: 64, color: mauChinh),
            const SizedBox(height: 16),
            const Text("Bạn chưa thuộc hộ gia đình nào", textAlign: TextAlign.center, style: TextStyle(fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            const Text("Tạo hộ gia đình mới hoặc tham gia bằng mã mời để chia sẻ tài chính với người thân.", textAlign: TextAlign.center),
            const SizedBox(height: 24),
            ElevatedButton(onPressed: _hienDialogTao, style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white), child: const Text("Tạo hộ gia đình")),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _hienDialogThamGia, child: const Text("Tham gia bằng mã mời")),
          ],
        ),
      ),
    );
  }

  Widget _daCoHo(HouseholdModel h) {
    return RefreshIndicator(
      onRefresh: _taiDuLieu,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(color: mauChinh.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(16)),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(h.tenHoGiaDinh, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
                const SizedBox(height: 4),
                Text("Chủ hộ: ${h.tenChuHo ?? 'N/A'}", style: const TextStyle(color: Colors.grey)),
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Text("Mã mời: "),
                    Text(h.maMoi, style: const TextStyle(fontWeight: FontWeight.bold, letterSpacing: 2)),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(child: TheThongKe(tieuDe: "Tổng thu gia đình", giaTri: tienVnd(h.tongThuGiaDinh), icon: Icons.arrow_downward, mau: mauThu)),
              const SizedBox(width: 12),
              Expanded(child: TheThongKe(tieuDe: "Tổng chi gia đình", giaTri: tienVnd(h.tongChiGiaDinh), icon: Icons.arrow_upward, mau: mauChi)),
            ],
          ),
          const SizedBox(height: 20),
          const Text("Chi tiêu theo thành viên", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 8),
          ...h.thanhVienChiTieu.map((tv) => Card(
                child: ListTile(
                  title: Text(tv.tenThanhVien),
                  subtitle: Text("Thu: ${tienVnd(tv.tongThu)} • Chi: ${tienVnd(tv.tongChi)}"),
                ),
              )),
          const SizedBox(height: 20),
          const Text("Giao dịch chia sẻ gần đây", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 8),
          if (h.giaoDichGanDay.isEmpty) const Padding(padding: EdgeInsets.all(8), child: Text("Chưa có giao dịch nào được chia sẻ")),
          ...h.giaoDichGanDay.map((t) => Card(
                child: ListTile(
                  title: Text(t.tenDanhMuc ?? ""),
                  subtitle: Text("${t.tenNguoiTao ?? ''} • ${ngayVn(t.ngayGiaoDich)}"),
                  trailing: Text(
                    "${t.loai == LoaiGiaoDich.thu ? '+' : '-'}${tienVnd(t.soTien)}",
                    style: TextStyle(fontWeight: FontWeight.bold, color: t.loai == LoaiGiaoDich.thu ? mauThu : mauChi),
                  ),
                ),
              )),
          const SizedBox(height: 20),
        ],
      ),
    );
  }
}
