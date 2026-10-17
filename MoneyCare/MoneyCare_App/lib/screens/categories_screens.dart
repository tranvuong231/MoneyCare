import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class CategoryListScreen extends StatefulWidget {
  const CategoryListScreen({super.key});
  @override
  State<CategoryListScreen> createState() => _CategoryListScreenState();
}

class _CategoryListScreenState extends State<CategoryListScreen> {
  List<CategoryModel> _ds = [];
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
      final ds = await CategoryService.danhSach();
      setState(() => _ds = ds);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(CategoryModel c) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa danh mục"),
        content: Text("Xóa danh mục \"${c.tenDanhMuc}\"?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan != true) return;
    try {
      await CategoryService.xoa(c.id);
      _taiDuLieu();
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Danh mục thu / chi")),
      body: _dangTai
          ? const DangTai()
          : _loi != null
              ? ThongBaoLoi(noiDung: _loi!, onThuLai: _taiDuLieu)
              : _ds.isEmpty
                  ? const Center(child: Text("Chưa có danh mục nào. Bấm + để tạo mới"))
                  : RefreshIndicator(
                      onRefresh: _taiDuLieu,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(12),
                        itemCount: _ds.length,
                        itemBuilder: (_, i) {
                          final c = _ds[i];
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              leading: CircleAvatar(backgroundColor: mauTuHex(c.mauSac).withValues(alpha: 0.15), child: Icon(Icons.circle, color: mauTuHex(c.mauSac), size: 14)),
                              title: Text(c.tenDanhMuc),
                              subtitle: Text(c.loai == LoaiGiaoDich.thu ? "Danh mục Thu" : "Danh mục Chi"),
                              trailing: PopupMenuButton<String>(
                                onSelected: (v) async {
                                  if (v == 'sua') {
                                    final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => CategoryFormScreen(danhMuc: c)));
                                    if (ketQua == true) _taiDuLieu();
                                  } else if (v == 'xoa') {
                                    _xoa(c);
                                  }
                                },
                                itemBuilder: (_) => [
                                  const PopupMenuItem(value: 'sua', child: Text("Sửa")),
                                  const PopupMenuItem(value: 'xoa', child: Text("Xóa")),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: mauChinh,
        onPressed: () async {
          final ketQua = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => const CategoryFormScreen()));
          if (ketQua == true) _taiDuLieu();
        },
        child: const Icon(Icons.add, color: Colors.white),
      ),
    );
  }
}

class CategoryFormScreen extends StatefulWidget {
  final CategoryModel? danhMuc;
  const CategoryFormScreen({super.key, this.danhMuc});

  @override
  State<CategoryFormScreen> createState() => _CategoryFormScreenState();
}

class _CategoryFormScreenState extends State<CategoryFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _tenCtrl = TextEditingController();
  LoaiGiaoDich _loai = LoaiGiaoDich.chi;
  String _mauSac = "#0d6efd";
  bool _dangLuu = false;

  final _bangMau = ["#0d6efd", "#16A34A", "#DC2626", "#F59E0B", "#8B5CF6", "#EC4899", "#0EA5E9", "#14B8A6"];

  bool get _laSua => widget.danhMuc != null;

  @override
  void initState() {
    super.initState();
    if (_laSua) {
      _tenCtrl.text = widget.danhMuc!.tenDanhMuc;
      _loai = widget.danhMuc!.loai;
      _mauSac = widget.danhMuc!.mauSac;
    }
  }

  Future<void> _luu() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _dangLuu = true);
    final model = CategoryModel(id: widget.danhMuc?.id ?? 0, tenDanhMuc: _tenCtrl.text.trim(), loai: _loai, mauSac: _mauSac);
    try {
      if (_laSua) {
        await CategoryService.sua(widget.danhMuc!.id, model);
      } else {
        await CategoryService.tao(model);
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
      appBar: AppBar(title: Text(_laSua ? "Sửa danh mục" : "Thêm danh mục")),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SegmentedButton<LoaiGiaoDich>(
                segments: const [
                  ButtonSegment(value: LoaiGiaoDich.thu, label: Text("Thu")),
                  ButtonSegment(value: LoaiGiaoDich.chi, label: Text("Chi")),
                ],
                selected: {_loai},
                onSelectionChanged: (v) => setState(() => _loai = v.first),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _tenCtrl,
                decoration: const InputDecoration(labelText: "Tên danh mục", border: OutlineInputBorder()),
                validator: (v) => (v == null || v.isEmpty) ? "Vui lòng nhập tên danh mục" : null,
              ),
              const SizedBox(height: 16),
              const Text("Màu sắc"),
              const SizedBox(height: 8),
              Wrap(
                spacing: 10,
                children: _bangMau.map((m) {
                  final chon = m == _mauSac;
                  return GestureDetector(
                    onTap: () => setState(() => _mauSac = m),
                    child: CircleAvatar(
                      radius: chon ? 20 : 16,
                      backgroundColor: mauTuHex(m),
                      child: chon ? const Icon(Icons.check, color: Colors.white, size: 16) : null,
                    ),
                  );
                }).toList(),
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _dangLuu ? null : _luu,
                style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Lưu danh mục"),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
