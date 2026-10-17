import 'package:flutter/material.dart';
import '../models/models.dart';
import '../services/services.dart';
import '../widgets/common_widgets.dart';

// Trợ lý AI trả lời có định dạng HTML đơn giản (chỉ dùng <b>...</b> để in đậm,
// vì trước đây soạn cho trang web hiển thị). Hàm này chuyển thành các đoạn
// TextSpan có/không in đậm để hiển thị đẹp trên app, thay vì hiện thẳng thẻ HTML.
List<InlineSpan> _dungHtmlDon(String text, {required Color mauChu}) {
  final spans = <InlineSpan>[];
  final regex = RegExp(r'<b>(.*?)</b>', dotAll: true);
  int last = 0;
  for (final m in regex.allMatches(text)) {
    if (m.start > last) spans.add(TextSpan(text: text.substring(last, m.start)));
    spans.add(TextSpan(text: m.group(1), style: const TextStyle(fontWeight: FontWeight.bold)));
    last = m.end;
  }
  if (last < text.length) spans.add(TextSpan(text: text.substring(last)));
  // Dọn các thẻ HTML còn sót lại (nếu có) để không bao giờ lộ thẻ thô ra màn hình
  for (final s in spans) {
    if (s is TextSpan && s.text != null) {
      final i = spans.indexOf(s);
      spans[i] = TextSpan(text: s.text!.replaceAll(RegExp(r'<[^>]+>'), ''), style: s.style);
    }
  }
  return spans;
}

class ChatbotScreen extends StatefulWidget {
  const ChatbotScreen({super.key});
  @override
  State<ChatbotScreen> createState() => _ChatbotScreenState();
}

class _ChatbotScreenState extends State<ChatbotScreen> {
  final List<ChatMessage> _tinNhans = [];
  final _inputCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();
  List<CauHoiMau> _goiY = [];
  bool _dangTraLoi = false;

  @override
  void initState() {
    super.initState();
    _tinNhans.add(ChatMessage(noiDung: "Xin chào! Mình là trợ lý tài chính MoneyCare. Bạn muốn hỏi gì nào? 😊", laNguoiDung: false));
    _taiGoiY();
  }

  Future<void> _taiGoiY() async {
    try {
      final ds = await ChatbotService.goiY();
      setState(() => _goiY = ds);
    } catch (_) {}
  }

  Future<void> _gui(String noiDung) async {
    if (noiDung.trim().isEmpty) return;
    setState(() {
      _tinNhans.add(ChatMessage(noiDung: noiDung, laNguoiDung: true));
      _dangTraLoi = true;
      _inputCtrl.clear();
    });
    _cuonXuong();
    try {
      final traLoi = await ChatbotService.hoi(noiDung);
      setState(() => _tinNhans.add(ChatMessage(noiDung: traLoi, laNguoiDung: false)));
    } catch (e) {
      setState(() => _tinNhans.add(ChatMessage(noiDung: "Xin lỗi, mình chưa trả lời được câu này. Vui lòng thử lại.", laNguoiDung: false)));
    } finally {
      setState(() => _dangTraLoi = false);
      _cuonXuong();
    }
  }

  void _cuonXuong() {
    Future.delayed(const Duration(milliseconds: 100), () {
      if (_scrollCtrl.hasClients) {
        _scrollCtrl.animateTo(_scrollCtrl.position.maxScrollExtent, duration: const Duration(milliseconds: 300), curve: Curves.easeOut);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Trợ lý tài chính AI")),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollCtrl,
              padding: const EdgeInsets.all(16),
              itemCount: _tinNhans.length + (_dangTraLoi ? 1 : 0),
              itemBuilder: (_, i) {
                if (i == _tinNhans.length) {
                  return const Align(alignment: Alignment.centerLeft, child: Padding(padding: EdgeInsets.all(8), child: DangTai()));
                }
                final m = _tinNhans[i];
                return Align(
                  alignment: m.laNguoiDung ? Alignment.centerRight : Alignment.centerLeft,
                  child: Container(
                    margin: const EdgeInsets.symmetric(vertical: 4),
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.75),
                    decoration: BoxDecoration(
                      color: m.laNguoiDung ? mauChinh : Colors.grey.shade200,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: Text.rich(
                      TextSpan(children: _dungHtmlDon(m.noiDung, mauChu: m.laNguoiDung ? Colors.white : Colors.black87)),
                      style: TextStyle(color: m.laNguoiDung ? Colors.white : Colors.black87),
                    ),
                  ),
                );
              },
            ),
          ),
          if (_goiY.isNotEmpty)
            SizedBox(
              height: 40,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                children: _goiY
                    .map((c) => Padding(
                          padding: const EdgeInsets.only(right: 8),
                          child: ActionChip(label: Text(c.cauHoi), onPressed: () => _gui(c.cauHoi)),
                        ))
                    .toList(),
              ),
            ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _inputCtrl,
                    decoration: const InputDecoration(hintText: "Nhập câu hỏi...", border: OutlineInputBorder(borderRadius: BorderRadius.all(Radius.circular(24)))),
                    onSubmitted: _gui,
                  ),
                ),
                const SizedBox(width: 8),
                CircleAvatar(
                  backgroundColor: mauChinh,
                  child: IconButton(icon: const Icon(Icons.send, color: Colors.white), onPressed: () => _gui(_inputCtrl.text)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
