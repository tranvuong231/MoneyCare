import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../services/notification_service.dart';
import '../widgets/common_widgets.dart';

class BillListScreen extends StatefulWidget {
  const BillListScreen({super.key});
  @override
  State<BillListScreen> createState() => _BillListScreenState();
}

class _BillListScreenState extends State<BillListScreen> {
  List<BillModel> _ds = [];
  bool _dangTai = true;
  String? _loi;
  String? _locTrangThai; // null = Tất cả, "ChoThanhToan", "QuaHan", "DaThanhToan"

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
      final ds = await BillService.danhSach(trangThai: _locTrangThai);
      setState(() => _ds = ds);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(BillModel b) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa hóa đơn"),
        content: Text('Xóa hóa đơn "${b.tenHoaDon}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan != true) return;
    await BillService.xoa(b.id);
    await NotificationService.huyNhacHoaDon(b.id);
    setState(() => _ds.removeWhere((x) => x.id == b.id));
    if (mounted) hienThongBao(context, 'Đã xóa hóa đơn "${b.tenHoaDon}"');
  }

  Widget _oLoc({required String nhan, required bool chon, required VoidCallback onTap}) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: BoxDecoration(
            color: chon ? mauChinh : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
            boxShadow: chon ? [BoxShadow(color: mauChinh.withValues(alpha: 0.3), blurRadius: 6, offset: const Offset(0, 2))] : null,
          ),
          child: Text(
            nhan,
            textAlign: TextAlign.center,
            style: TextStyle(
              color: chon ? Colors.white : Colors.grey.shade700,
              fontWeight: chon ? FontWeight.bold : FontWeight.normal,
              fontSize: 12,
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Hóa đơn & Nhắc nhở")),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Container(
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(color: const Color(0xFFF1F5F9), borderRadius: BorderRadius.circular(14)),
              child: Row(
                children: [
                  _oLoc(nhan: "Tất cả", chon: _locTrangThai == null, onTap: () {
                    setState(() => _locTrangThai = null);
                    _taiDuLieu();
                  }),
                  _oLoc(nhan: "Chờ TT", chon: _locTrangThai == "ChoThanhToan", onTap: () {
                    setState(() => _locTrangThai = "ChoThanhToan");
                    _taiDuLieu();
                  }),
                  _oLoc(nhan: "Quá hạn", chon: _locTrangThai == "QuaHan", onTap: () {
                    setState(() => _locTrangThai = "QuaHan");
                    _taiDuLieu();
                  }),
                  _oLoc(nhan: "Đã TT", chon: _locTrangThai == "DaThanhToan", onTap: () {
                    setState(() => _locTrangThai = "DaThanhToan");
                    _taiDuLieu();
                  }),
                ],
              ),
            ),
          ),
          Expanded(
            child: _dangTai
                ? const DangTai()
                : _loi != null
                    ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
                    : _ds.isEmpty
                        ? const Center(child: Text("Không có hóa đơn nào phù hợp"))
                        : RefreshIndicator(
                            onRefresh: _taiDuLieu,
                            child: ListView.builder(
                              padding: const EdgeInsets.all(12),
                              itemCount: _ds.length,
                              itemBuilder: (_, i) {
                                final b = _ds[i];
                                final quaHan = !b.daThanhToan && b.soNgayConLai < 0;
                                return Dismissible(
                                  key: ValueKey(b.id),
                                  direction: DismissDirection.endToStart,
                                  background: Container(
                                    alignment: Alignment.centerRight,
                                    padding: const EdgeInsets.symmetric(horizontal: 20),
                                    margin: const EdgeInsets.only(bottom: 8),
                                    decoration: BoxDecoration(color: mauChi, borderRadius: BorderRadius.circular(12)),
                                    child: const Icon(Icons.delete, color: Colors.white),
                                  ),
                                  confirmDismiss: (_) async {
                                    return await showDialog<bool>(
                                      context: context,
                                      builder: (_) => AlertDialog(
                                        title: const Text("Xóa hóa đơn"),
                                        content: Text('Xóa hóa đơn "${b.tenHoaDon}"?'),
                                        actions: [
                                          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
                                          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
                                        ],
                                      ),
                                    );
                                  },
                                  onDismissed: (_) async {
                                    await BillService.xoa(b.id);
                                    await NotificationService.huyNhacHoaDon(b.id);
                                    setState(() => _ds.removeWhere((x) => x.id == b.id));
                                    if (mounted) hienThongBao(context, 'Đã xóa hóa đơn "${b.tenHoaDon}"');
                                  },
                                  child: Card(
                                    margin: const EdgeInsets.only(bottom: 8),
                                    child: ListTile(
                                      contentPadding: const EdgeInsets.only(left: 16, right: 8, top: 4, bottom: 4),
                                      leading: CircleAvatar(
                                        backgroundColor: b.daThanhToan ? mauThu.withValues(alpha: 0.15) : (quaHan ? mauChi.withValues(alpha: 0.15) : Colors.amber.withValues(alpha: 0.15)),
                                        child: Icon(
                                          b.daThanhToan ? Icons.check_circle : Icons.event_note,
                                          color: b.daThanhToan ? mauThu : (quaHan ? mauChi : Colors.amber.shade800),
                                          size: 18,
                                        ),
                                      ),
                                      title: Text(b.tenHoaDon, style: TextStyle(decoration: b.daThanhToan ? TextDecoration.lineThrough : null)),
                                      subtitle: Text(
                                        b.daThanhToan
                                            ? "Đã thanh toán"
                                            : quaHan
                                                ? "Quá hạn ${-b.soNgayConLai} ngày - Hạn ${ngayVn(b.ngayDenHan)}"
                                                : "Còn ${b.soNgayConLai} ngày - Hạn ${ngayVn(b.ngayDenHan)}",
                                        style: const TextStyle(fontSize: 12),
                                      ),
                                      trailing: Column(
                                        mainAxisSize: MainAxisSize.min,
                                        mainAxisAlignment: MainAxisAlignment.center,
                                        crossAxisAlignment: CrossAxisAlignment.end,
                                        children: [
                                          Text(tienVnd(b.soTien), style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                                          const SizedBox(height: 4),
                                          GestureDetector(
                                            onTap: () async {
                                              final capNhat = await BillService.danhDauThanhToan(b.id);
                                              if (capNhat.daThanhToan) {
                                                await NotificationService.huyNhacHoaDon(capNhat.id);
                                              } else {
                                                await NotificationService.datLichNhacHoaDon(
                                                  billId: capNhat.id,
                                                  tenHoaDon: capNhat.tenHoaDon,
                                                  soTien: capNhat.soTien,
                                                  ngayDenHan: capNhat.ngayDenHan,
                                                );
                                              }
                                              _taiDuLieu();
                                            },
                                            child: Container(
                                              width: 22,
                                              height: 22,
                                              decoration: BoxDecoration(
                                                shape: BoxShape.circle,
                                                color: b.daThanhToan ? mauThu : Colors.transparent,
                                                border: Border.all(color: b.daThanhToan ? mauThu : Colors.grey.shade400, width: 1.5),
                                              ),
                                              child: b.daThanhToan ? const Icon(Icons.check, size: 14, color: Colors.white) : null,
                                            ),
                                          ),
                                        ],
                                      ),
                                      onTap: () async {
                                        final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => BillFormScreen(hoaDon: b)));
                                        if (ketQua == true) _taiDuLieu();
                                      },
                                      onLongPress: () => _xoa(b),
                                    ),
                                  ),
                                );
                              },
                            ),
                          ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: mauChinh,
        onPressed: () async {
          final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => const BillFormScreen()));
          if (ketQua == true) _taiDuLieu();
        },
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }
}

class BillFormScreen extends StatefulWidget {
  final BillModel? hoaDon;
  const BillFormScreen({super.key, this.hoaDon});

  @override
  State<BillFormScreen> createState() => _BillFormScreenState();
}

class _BillFormScreenState extends State<BillFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _tenCtrl = TextEditingController();
  final _soTienCtrl = TextEditingController();
  final _ghiChuCtrl = TextEditingController();
  DateTime _ngayDenHan = DateTime.now().add(const Duration(days: 7));
  bool _daThanhToan = false;
  bool _dangLuu = false;

  bool get _laSua => widget.hoaDon != null;

  @override
  void initState() {
    super.initState();
    if (_laSua) {
      final b = widget.hoaDon!;
      _tenCtrl.text = b.tenHoaDon;
      _soTienCtrl.text = b.soTien.toStringAsFixed(0);
      _ghiChuCtrl.text = b.ghiChu ?? '';
      _ngayDenHan = b.ngayDenHan;
      _daThanhToan = b.daThanhToan;
    }
  }

  Future<void> _luu() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangLuu = true);
    final model = BillModel(
      id: widget.hoaDon?.id ?? 0,
      tenHoaDon: _tenCtrl.text.trim(),
      soTien: double.parse(_soTienCtrl.text.replaceAll('.', '').replaceAll(',', '')),
      ngayDenHan: _ngayDenHan,
      daThanhToan: _daThanhToan,
      ghiChu: _ghiChuCtrl.text.trim().isEmpty ? null : _ghiChuCtrl.text.trim(),
      soNgayConLai: 0,
    );
    try {
      BillModel ketQua;
      if (_laSua) {
        ketQua = await BillService.sua(widget.hoaDon!.id, model);
      } else {
        ketQua = await BillService.tao(model);
      }

      // Đặt/hủy lịch nhắc cục bộ dựa trên trạng thái thanh toán
      if (ketQua.daThanhToan) {
        await NotificationService.huyNhacHoaDon(ketQua.id);
      } else {
        await NotificationService.datLichNhacHoaDon(
          billId: ketQua.id,
          tenHoaDon: ketQua.tenHoaDon,
          soTien: ketQua.soTien,
          ngayDenHan: ketQua.ngayDenHan,
        );
      }

      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } catch (e) {
      // Bắt mọi lỗi không lường trước (VD lỗi plugin thông báo) để KHÔNG BAO GIỜ
      // để màn hình đứng im không phản hồi khi bấm Lưu.
      if (mounted) hienThongBao(context, "Đã có lỗi xảy ra, vui lòng thử lại: $e", loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_laSua ? "Sửa hóa đơn" : "Thêm hóa đơn")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _tenCtrl,
                decoration: const InputDecoration(labelText: "Tên hóa đơn", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập tên hóa đơn" : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _soTienCtrl,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: "Số tiền (VNĐ)", border: OutlineInputBorder()),
                validator: (v) {
                  if (v == null || v.isEmpty) return "Vui lòng nhập số tiền";
                  final n = double.tryParse(v.replaceAll('.', '').replaceAll(',', ''));
                  if (n == null || n <= 0) return "Số tiền không hợp lệ";
                  return null;
                },
              ),
              const SizedBox(height: 16),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text("Ngày đến hạn: ${ngayVn(_ngayDenHan)}"),
                trailing: const Icon(Icons.calendar_today),
                onTap: () async {
                  final chon = await showDatePicker(context: context, initialDate: _ngayDenHan, firstDate: DateTime(2000), lastDate: DateTime(2100));
                  if (chon != null) setState(() => _ngayDenHan = chon);
                },
              ),
              TextFormField(
                controller: _ghiChuCtrl,
                decoration: const InputDecoration(labelText: "Ghi chú (không bắt buộc)", border: OutlineInputBorder()),
                maxLength: 255,
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text("Đã thanh toán"),
                value: _daThanhToan,
                onChanged: (v) => setState(() => _daThanhToan = v),
              ),
              const SizedBox(height: 8),
              ElevatedButton(
                onPressed: _dangLuu ? null : _luu,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Lưu hóa đơn"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
