import 'dart:io';
import 'package:flutter/material.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});
  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  ReportModel? _baoCao;
  bool _dangTai = true;
  String? _loi;
  late DateTime _tuNgay;
  late DateTime _denNgay;
  bool _dangXuat = false;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _tuNgay = DateTime(now.year, now.month, 1);
    _denNgay = DateTime(now.year, now.month + 1, 0);
    _taiDuLieu();
  }

  Future<void> _xuatBaoCao(bool laExcel) async {
    setState(() => _dangXuat = true);
    try {
      final duongDan = laExcel ? ExportService.duongDanExcel(_tuNgay, _denNgay) : ExportService.duongDanPdf(_tuNgay, _denNgay);
      final bytes = await ApiClient.getBytes(duongDan);

      final thuMuc = await getTemporaryDirectory();
      final tenFile = "MoneyCare_BaoCao_${_tuNgay.toIso8601String().split('T')[0]}_${_denNgay.toIso8601String().split('T')[0]}.${laExcel ? 'xlsx' : 'pdf'}";
      final file = File("${thuMuc.path}/$tenFile");
      await file.writeAsBytes(bytes);

      if (mounted) {
        await Share.shareXFiles([XFile(file.path)], text: "Báo cáo tài chính MoneyCare");
      }
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } catch (e) {
      if (mounted) hienThongBao(context, "Không xuất được báo cáo", loi: true);
    } finally {
      if (mounted) setState(() => _dangXuat = false);
    }
  }

  Future<void> _taiDuLieu() async {
    setState(() {
      _dangTai = true;
      _loi = null;
    });
    try {
      final r = await ReportService.layBaoCao(tuNgay: _tuNgay, denNgay: _denNgay);
      setState(() => _baoCao = r);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _chonKhoangNgay() async {
    final ketQua = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
      initialDateRange: DateTimeRange(start: _tuNgay, end: _denNgay),
    );
    if (ketQua != null) {
      setState(() {
        _tuNgay = ketQua.start;
        _denNgay = ketQua.end;
      });
      _taiDuLieu();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Báo cáo thống kê"),
        actions: [
          IconButton(icon: const Icon(Icons.date_range), onPressed: _chonKhoangNgay),
          _dangXuat
              ? const Padding(padding: EdgeInsets.all(14), child: SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)))
              : PopupMenuButton<bool>(
                  icon: const Icon(Icons.ios_share),
                  tooltip: "Xuất báo cáo",
                  onSelected: _xuatBaoCao,
                  itemBuilder: (_) => const [
                    PopupMenuItem(value: true, child: Text("Xuất Excel (.xlsx)")),
                    PopupMenuItem(value: false, child: Text("Xuất PDF")),
                  ],
                ),
        ],
      ),
      body: _dangTai
          ? const DangTai()
          : _loi != null
              ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
              : _noiDung(),
    );
  }

  Widget _noiDung() {
    final bc = _baoCao!;
    return RefreshIndicator(
      onRefresh: _taiDuLieu,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text("Từ ${ngayVn(bc.tuNgay)} đến ${ngayVn(bc.denNgay)}", style: const TextStyle(color: Colors.grey)),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: TheThongKe(tieuDe: "Tổng thu", giaTri: tienVnd(bc.tongThu), icon: Icons.arrow_downward, mau: mauThu)),
              const SizedBox(width: 12),
              Expanded(child: TheThongKe(tieuDe: "Tổng chi", giaTri: tienVnd(bc.tongChi), icon: Icons.arrow_upward, mau: mauChi)),
            ],
          ),
          const SizedBox(height: 24),
          const Text("Chi tiêu theo danh mục", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 12),
          bc.danhMucChi.isEmpty ? const Padding(padding: EdgeInsets.all(16), child: Text("Không có dữ liệu chi tiêu")) : _bieuDoTron(bc.danhMucChi),
          const SizedBox(height: 12),
          ..._chuThich(bc.danhMucChi),
          const SizedBox(height: 24),
          const Text("Thu nhập theo danh mục", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
          const SizedBox(height: 12),
          bc.danhMucThu.isEmpty ? const Padding(padding: EdgeInsets.all(16), child: Text("Không có dữ liệu thu nhập")) : _bieuDoTron(bc.danhMucThu),
          const SizedBox(height: 12),
          ..._chuThich(bc.danhMucThu),
        ],
      ),
    );
  }

  Widget _bieuDoTron(List<ReportCategorySlice> ds) {
    final tong = ds.fold<double>(0, (s, e) => s + e.soTien);
    return SizedBox(
      height: 200,
      child: PieChart(
        PieChartData(
          sectionsSpace: 2,
          centerSpaceRadius: 40,
          sections: ds.map((s) {
            final phanTram = tong == 0 ? 0 : (s.soTien / tong * 100);
            return PieChartSectionData(
              color: mauTuHex(s.mauSac),
              value: s.soTien,
              title: "${phanTram.toStringAsFixed(0)}%",
              radius: 60,
              titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12),
            );
          }).toList(),
        ),
      ),
    );
  }

  List<Widget> _chuThich(List<ReportCategorySlice> ds) {
    return ds
        .map((s) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                children: [
                  CircleAvatar(radius: 6, backgroundColor: mauTuHex(s.mauSac)),
                  const SizedBox(width: 8),
                  Expanded(child: Text(s.ten)),
                  Text(tienVnd(s.soTien), style: const TextStyle(fontWeight: FontWeight.bold)),
                ],
              ),
            ))
        .toList();
  }
}
