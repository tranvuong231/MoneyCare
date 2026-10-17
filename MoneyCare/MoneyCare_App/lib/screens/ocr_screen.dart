import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../services/api_client.dart';
import '../widgets/common_widgets.dart';

class OcrScreen extends StatefulWidget {
  const OcrScreen({super.key});
  @override
  State<OcrScreen> createState() => _OcrScreenState();
}

class _OcrScreenState extends State<OcrScreen> {
  File? _anh;
  bool _dangQuet = false;
  bool _dangLuu = false;
  bool _khongPhaiPremium = false;
  OcrKetQuaDto? _ketQua;

  final _soTienCtrl = TextEditingController();
  final _ghiChuCtrl = TextEditingController();
  DateTime _ngay = DateTime.now();
  int? _categoryId;
  LoaiGiaoDich _loai = LoaiGiaoDich.chi;
  List<CategoryModel> _danhMuc = [];

  Future<void> _chonAnh(ImageSource nguon) async {
    final picker = ImagePicker();
    final file = await picker.pickImage(source: nguon, imageQuality: 85);
    if (file == null) return;
    setState(() {
      _anh = File(file.path);
      _ketQua = null;
      _khongPhaiPremium = false;
    });
    await _quet();
  }

  Future<void> _quet() async {
    if (_anh == null) return;
    setState(() => _dangQuet = true);
    try {
      final ketQua = await OcrService.quet(_anh!);
      final danhMucs = await CategoryService.danhSach(loai: ketQua.loaiGoiY);
      setState(() {
        _ketQua = ketQua;
        _loai = ketQua.loaiGoiY;
        _danhMuc = danhMucs;
        _soTienCtrl.text = ketQua.soTienNhanDien != null ? ketQua.soTienNhanDien!.toStringAsFixed(0) : '';
        _ngay = ketQua.ngayNhanDien ?? DateTime.now();
        // Ưu tiên danh mục AI gợi ý (nếu có và khớp với 1 danh mục thật của người dùng)
        _categoryId = ketQua.danhMucGoiYId ?? (danhMucs.isNotEmpty ? danhMucs.first.id : null);
        // AI đã cố gắng điền sẵn ghi chú chính xác nhất có thể - người dùng chỉ cần kiểm tra lại
        _ghiChuCtrl.text = ketQua.ghiChuGoiY ?? (ketQua.tenCuaHang ?? '');
      });
      if (ketQua.soTienNhanDien == null && mounted) {
        hienThongBao(context, "Không nhận diện được số tiền tự động - vui lòng nhập tay bên dưới", loi: true);
      }
    } on ApiException catch (e) {
      if (e.statusCode == 402) {
        setState(() => _khongPhaiPremium = true);
      } else {
        if (mounted) hienThongBao(context, e.message, loi: true);
      }
    } finally {
      setState(() => _dangQuet = false);
    }
  }

  // Người dùng đổi loại Thu/Chi trên form -> tải lại đúng danh mục theo loại đó, giữ lựa chọn
  // danh mục cũ nếu nó vẫn thuộc loại mới (hiếm khi trùng id giữa 2 loại), còn lại thì bỏ chọn.
  Future<void> _doiLoai(LoaiGiaoDich loaiMoi) async {
    if (loaiMoi == _loai) return;
    final danhMucCuTen = _danhMuc.firstWhere((c) => c.id == _categoryId, orElse: () => _danhMuc.first).tenDanhMuc;
    setState(() => _loai = loaiMoi);
    final danhMucs = await CategoryService.danhSach(loai: loaiMoi);
    final khopTen = danhMucs.where((c) => c.tenDanhMuc == danhMucCuTen);
    setState(() {
      _danhMuc = danhMucs;
      _categoryId = khopTen.isNotEmpty ? khopTen.first.id : (danhMucs.isNotEmpty ? danhMucs.first.id : null);
    });
  }

  Future<void> _xacNhanLuu() async {
    if (_categoryId == null) {
      hienThongBao(context, "Vui lòng chọn danh mục", loi: true);
      return;
    }
    final soTien = double.tryParse(_soTienCtrl.text.replaceAll('.', ''));
    if (soTien == null || soTien <= 0) {
      hienThongBao(context, "Vui lòng nhập số tiền hợp lệ", loi: true);
      return;
    }
    setState(() => _dangLuu = true);
    try {
      await OcrService.xacNhanLuu(
        soTien: soTien,
        ngayGiaoDich: _ngay,
        ghiChu: _ghiChuCtrl.text.trim().isEmpty ? null : _ghiChuCtrl.text.trim(),
        categoryId: _categoryId!,
        loai: _loai,
      );
      if (mounted) {
        hienThongBao(context, "Đã lưu giao dịch từ hóa đơn quét!");
        Navigator.of(context).pop(true);
      }
    } on ApiException catch (e) {
      if (mounted) hienThongBao(context, e.message, loi: true);
    } finally {
      if (mounted) setState(() => _dangLuu = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Quét hóa đơn (OCR)")),
      body: _khongPhaiPremium
          ? ChanPremium(tenTinhNang: "Quét hóa đơn tự động (OCR)", onThuLai: () => setState(() => _khongPhaiPremium = false))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_anh != null) ClipRRect(borderRadius: BorderRadius.circular(12), child: Image.file(_anh!, height: 200, fit: BoxFit.cover)),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: () => _chonAnh(ImageSource.camera),
                          icon: const Icon(Icons.camera_alt),
                          label: const Text("Chụp ảnh"),
                          style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () => _chonAnh(ImageSource.gallery),
                          icon: const Icon(Icons.photo_library),
                          label: const Text("Chọn từ thư viện"),
                        ),
                      ),
                    ],
                  ),
                  if (_dangQuet) const Padding(padding: EdgeInsets.all(24), child: DangTai()),
                  if (_ketQua != null && !_dangQuet) ...[
                    const SizedBox(height: 24),
                    Row(
                      children: [
                        const Expanded(
                          child: Text("Kết quả nhận diện - kiểm tra lại trước khi lưu", style: TextStyle(fontWeight: FontWeight.bold)),
                        ),
                        Chip(
                          avatar: Icon(_ketQua!.phanTichBoiAi ? Icons.auto_awesome : Icons.rule, size: 16, color: Colors.white),
                          label: Text(_ketQua!.phanTichBoiAi ? "AI (LM Studio)" : "Quy tắc"),
                          backgroundColor: _ketQua!.phanTichBoiAi ? Colors.green : Colors.blueGrey,
                          labelStyle: const TextStyle(color: Colors.white, fontSize: 12),
                          visualDensity: VisualDensity.compact,
                        ),
                      ],
                    ),
                    if (_ketQua!.tenCuaHang != null) ...[
                      const SizedBox(height: 4),
                      Text("Cửa hàng: ${_ketQua!.tenCuaHang}", style: const TextStyle(color: Colors.grey)),
                    ],
                    const SizedBox(height: 12),
                    SegmentedButton<LoaiGiaoDich>(
                      segments: const [
                        ButtonSegment(value: LoaiGiaoDich.chi, label: Text("Chi"), icon: Icon(Icons.arrow_upward)),
                        ButtonSegment(value: LoaiGiaoDich.thu, label: Text("Thu"), icon: Icon(Icons.arrow_downward)),
                      ],
                      selected: {_loai},
                      onSelectionChanged: (s) => _doiLoai(s.first),
                      style: SegmentedButton.styleFrom(
                        selectedBackgroundColor: _loai == LoaiGiaoDich.thu ? mauThu : mauChi,
                        selectedForegroundColor: Colors.white,
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _soTienCtrl,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(labelText: "Số tiền (VNĐ)", border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<int>(
                      initialValue: _danhMuc.any((c) => c.id == _categoryId) ? _categoryId : null,
                      decoration: InputDecoration(labelText: _loai == LoaiGiaoDich.thu ? "Danh mục thu" : "Danh mục chi", border: const OutlineInputBorder()),
                      items: _danhMuc.map((c) => DropdownMenuItem(value: c.id, child: Text(c.tenDanhMuc))).toList(),
                      onChanged: (v) => setState(() => _categoryId = v),
                    ),
                    const SizedBox(height: 12),
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text("Ngày giao dịch: ${ngayVn(_ngay)}"),
                      trailing: const Icon(Icons.calendar_today),
                      onTap: () async {
                        final chon = await showDatePicker(context: context, initialDate: _ngay, firstDate: DateTime(2000), lastDate: DateTime(2100));
                        if (chon != null) setState(() => _ngay = chon);
                      },
                    ),
                    TextField(
                      controller: _ghiChuCtrl,
                      decoration: const InputDecoration(labelText: "Ghi chú", border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 8),
                    ExpansionTile(
                      title: const Text("Xem văn bản gốc nhận diện được"),
                      children: [Padding(padding: const EdgeInsets.all(8), child: Text(_ketQua!.vanBanGoc))],
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: _dangLuu ? null : _xacNhanLuu,
                      style: ElevatedButton.styleFrom(backgroundColor: mauChinh, foregroundColor: Colors.white, padding: const EdgeInsets.all(16)),
                      child: _dangLuu ? const CircularProgressIndicator(color: Colors.white) : const Text("Xác nhận & Lưu giao dịch"),
                    ),
                  ],
                ],
              ),
            ),
    );
  }
}
