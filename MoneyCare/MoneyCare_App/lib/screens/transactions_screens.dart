import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class TransactionListScreen extends StatefulWidget {
  const TransactionListScreen({super.key});
  @override
  State<TransactionListScreen> createState() => _TransactionListScreenState();
}

class _TransactionListScreenState extends State<TransactionListScreen> {
  List<TransactionModel> _ds = [];
  double _tongThu = 0, _tongChi = 0;
  bool _dangTai = true;
  String? _loi;
  LoaiGiaoDich? _locLoai;

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
      final res = await TransactionService.danhSach(loai: _locLoai);
      setState(() {
        _ds = res['danhSach'];
        _tongThu = res['tongThu'];
        _tongChi = res['tongChi'];
      });
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(TransactionModel t) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa giao dịch"),
        content: const Text("Bạn có chắc muốn xóa giao dịch này?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan != true) return;
    try {
      await TransactionService.xoa(t.id);
      _taiDuLieu();
      if (mounted) hienThongBao(context, "Đã xóa giao dịch");
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Giao dịch thu / chi")),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Container(
              padding: const EdgeInsets.all(4),
              decoration: BoxDecoration(color: const Color(0xFFF1F5F9), borderRadius: BorderRadius.circular(14)),
              child: Row(
                children: [
                  _oLoc(nhan: "Tất cả", chon: _locLoai == null, mau: mauChinh, onTap: () {
                    setState(() => _locLoai = null);
                    _taiDuLieu();
                  }),
                  _oLoc(nhan: "Thu", chon: _locLoai == LoaiGiaoDich.thu, mau: mauThu, icon: Icons.arrow_downward, onTap: () {
                    setState(() => _locLoai = LoaiGiaoDich.thu);
                    _taiDuLieu();
                  }),
                  _oLoc(nhan: "Chi", chon: _locLoai == LoaiGiaoDich.chi, mau: mauChi, icon: Icons.arrow_upward, onTap: () {
                    setState(() => _locLoai = LoaiGiaoDich.chi);
                    _taiDuLieu();
                  }),
                ],
              ),
            ),
          ),
          if (!_dangTai && _loi == null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Row(
                children: [
                  Expanded(child: TheThongKe(tieuDe: "Thu", giaTri: tienVnd(_tongThu), icon: Icons.arrow_downward, mau: mauThu)),
                  const SizedBox(width: 8),
                  Expanded(child: TheThongKe(tieuDe: "Chi", giaTri: tienVnd(_tongChi), icon: Icons.arrow_upward, mau: mauChi)),
                ],
              ),
            ),
          Expanded(
            child: _dangTai
                ? const DangTai()
                : _loi != null
                    ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
                    : _ds.isEmpty
                        ? const Center(child: Text("Chưa có giao dịch nào"))
                        : RefreshIndicator(
                            onRefresh: _taiDuLieu,
                            child: ListView.builder(
                              padding: const EdgeInsets.all(12),
                              itemCount: _ds.length,
                              itemBuilder: (_, i) {
                                final t = _ds[i];
                                return Card(
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
                                      style: const TextStyle(fontSize: 12),
                                    ),
                                    trailing: Text(
                                      "${t.loai == LoaiGiaoDich.thu ? '+' : '-'}${tienVnd(t.soTien)}",
                                      style: TextStyle(fontWeight: FontWeight.bold, color: t.loai == LoaiGiaoDich.thu ? mauThu : mauChi),
                                    ),
                                    onTap: () async {
                                      final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => TransactionFormScreen(giaoDich: t)));
                                      if (ketQua == true) _taiDuLieu();
                                    },
                                    onLongPress: () => _xoa(t),
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
          final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => const TransactionFormScreen()));
          if (ketQua == true) _taiDuLieu();
        },
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }

  Widget _oLoc({required String nhan, required bool chon, required Color mau, IconData? icon, required VoidCallback onTap}) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: BoxDecoration(
            color: chon ? mau : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
            boxShadow: chon ? [BoxShadow(color: mau.withValues(alpha: 0.3), blurRadius: 6, offset: const Offset(0, 2))] : null,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (icon != null) ...[
                Icon(icon, size: 14, color: chon ? Colors.white : Colors.grey.shade600),
                const SizedBox(width: 4),
              ],
              Text(
                nhan,
                style: TextStyle(
                  color: chon ? Colors.white : Colors.grey.shade700,
                  fontWeight: chon ? FontWeight.bold : FontWeight.normal,
                  fontSize: 13,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class TransactionFormScreen extends StatefulWidget {
  final TransactionModel? giaoDich;
  const TransactionFormScreen({super.key, this.giaoDich});

  @override
  State<TransactionFormScreen> createState() => _TransactionFormScreenState();
}

class _TransactionFormScreenState extends State<TransactionFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _soTienCtrl = TextEditingController();
  final _ghiChuCtrl = TextEditingController();
  LoaiGiaoDich _loai = LoaiGiaoDich.chi;
  DateTime _ngay = DateTime.now();
  int? _categoryId;
  bool _chiaSeGiaDinh = false;
  List<CategoryModel> _danhMucs = [];
  bool _dangTaiDanhMuc = true;
  bool _dangLuu = false;

  bool get _laSua => widget.giaoDich != null;

  @override
  void initState() {
    super.initState();
    if (_laSua) {
      final t = widget.giaoDich!;
      _soTienCtrl.text = t.soTien.toStringAsFixed(0);
      _ghiChuCtrl.text = t.ghiChu ?? '';
      _loai = t.loai;
      _ngay = t.ngayGiaoDich;
      _categoryId = t.categoryId;
      _chiaSeGiaDinh = t.chiaSeGiaDinh;
    }
    _taiDanhMuc();
  }

  Future<void> _taiDanhMuc() async {
    setState(() => _dangTaiDanhMuc = true);
    try {
      final ds = await CategoryService.danhSach(loai: _loai);
      setState(() {
        _danhMucs = ds;
        if (_categoryId == null || !ds.any((c) => c.id == _categoryId)) {
          _categoryId = ds.isNotEmpty ? ds.first.id : null;
        }
      });
    } catch (_) {
    } finally {
      setState(() => _dangTaiDanhMuc = false);
    }
  }

  Future<void> _luu() async {
    if (!_formKey.currentState!.validate()) return;
    if (_categoryId == null) {
      hienThongBao(context, "Vui lòng tạo danh mục trước khi thêm giao dịch", loi: true);
      return;
    }
    setState(() => _dangLuu = true);
    final model = TransactionModel(
      id: widget.giaoDich?.id ?? 0,
      soTien: double.parse(_soTienCtrl.text.replaceAll('.', '').replaceAll(',', '')),
      loai: _loai,
      ngayGiaoDich: _ngay,
      ghiChu: _ghiChuCtrl.text.trim().isEmpty ? null : _ghiChuCtrl.text.trim(),
      categoryId: _categoryId!,
      chiaSeGiaDinh: _chiaSeGiaDinh,
    );
    try {
      if (_laSua) {
        await TransactionService.sua(widget.giaoDich!.id, model);
      } else {
        await TransactionService.tao(model);
      }
      if (mounted) Navigator.of(context).pop(true);
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_laSua ? "Sửa giao dịch" : "Thêm giao dịch")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SegmentedButton<LoaiGiaoDich>(
                segments: const [
                  ButtonSegment(value: LoaiGiaoDich.thu, label: Text("Thu"), icon: Icon(Icons.arrow_downward)),
                  ButtonSegment(value: LoaiGiaoDich.chi, label: Text("Chi"), icon: Icon(Icons.arrow_upward)),
                ],
                selected: {_loai},
                onSelectionChanged: (v) {
                  setState(() => _loai = v.first);
                  _taiDanhMuc();
                },
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
              _dangTaiDanhMuc
                  ? const Padding(padding: EdgeInsets.all(8), child: DangTai())
                  : DropdownButtonFormField<int>(
                      initialValue: _categoryId,
                      decoration: const InputDecoration(labelText: "Danh mục", border: OutlineInputBorder()),
                      items: _danhMucs.map((c) => DropdownMenuItem(value: c.id, child: Text(c.tenDanhMuc))).toList(),
                      onChanged: (v) => setState(() => _categoryId = v),
                      hint: _danhMucs.isEmpty ? const Text("Chưa có danh mục - hãy tạo trước") : null,
                    ),
              const SizedBox(height: 16),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: Text("Ngày giao dịch: ${ngayVn(_ngay)}"),
                trailing: const Icon(Icons.calendar_today),
                onTap: () async {
                  final chon = await showDatePicker(context: context, initialDate: _ngay, firstDate: DateTime(2000), lastDate: DateTime(2100));
                  if (chon != null) setState(() => _ngay = chon);
                },
              ),
              TextFormField(
                controller: _ghiChuCtrl,
                decoration: const InputDecoration(labelText: "Ghi chú (không bắt buộc)", border: OutlineInputBorder()),
                maxLength: 255,
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text("Chia sẻ với gia đình"),
                subtitle: const Text("Hiện trong báo cáo chung của hộ gia đình (Premium)"),
                value: _chiaSeGiaDinh,
                onChanged: (v) => setState(() => _chiaSeGiaDinh = v),
              ),
              const SizedBox(height: 8),
              ElevatedButton(
                onPressed: _dangLuu ? null : _luu,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Lưu giao dịch"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
