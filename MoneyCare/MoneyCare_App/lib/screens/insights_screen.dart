import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class InsightsScreen extends StatefulWidget {
  const InsightsScreen({super.key});
  @override
  State<InsightsScreen> createState() => _InsightsScreenState();
}

class _InsightsScreenState extends State<InsightsScreen> {
  InsightsModel? _duLieu;
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
      final r = await InsightsService.phanTich();
      setState(() => _duLieu = r);
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

  Color _mauMucDo(String mucDo) => switch (mucDo) {
        "danger" => mauChi,
        "warning" => Colors.amber.shade800,
        "success" => mauThu,
        _ => mauChinh,
      };

  IconData _iconMucDo(String mucDo) => switch (mucDo) {
        "danger" => Icons.warning_amber_rounded,
        "warning" => Icons.info_outline,
        "success" => Icons.check_circle_outline,
        _ => Icons.lightbulb_outline,
      };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Phân tích & Gợi ý AI")),
      body: _dangTai
          ? const DangTai()
          : _khongPhaiPremium
              ? ChanPremium(tenTinhNang: "Phân tích & Gợi ý AI", onThuLai: _taiDuLieu)
              : _loi != null
                  ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
                  : _noiDung(),
    );
  }

  Widget _noiDung() {
    final d = _duLieu!;
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
                const Text("Dự đoán số dư cuối tháng", style: TextStyle(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Text(tienVnd(d.duDoan.soDuDuDoanCuoiThang), style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: mauChinh)),
                const SizedBox(height: 4),
                Text("Còn ${d.duDoan.soNgayConLaiTrongThang} ngày trong tháng • Chi tiêu TB/ngày: ${tienVnd(d.duDoan.chiTieuTrungBinhNgay)}",
                    style: const TextStyle(color: Colors.grey)),
              ],
            ),
          ),
          const SizedBox(height: 20),
          const Text("Thói quen chi tiêu", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 8),
          ...d.thoiQuenChiTieu.map((i) => _theInsight(i)),
          const SizedBox(height: 20),
          const Text("Gợi ý tiết kiệm", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 8),
          ...d.goiYTietKiem.map((i) => _theInsight(i)),
        ],
      ),
    );
  }

  Widget _theInsight(InsightItem i) {
    final mau = _mauMucDo(i.mucDo);
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(_iconMucDo(i.mucDo), color: mau),
        title: Text(i.tieuDe, style: const TextStyle(fontWeight: FontWeight.bold)),
        subtitle: Text(i.noiDung),
      ),
    );
  }
}
