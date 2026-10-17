// Tuần 7: màn hình Thanh toán QR (screens/payment_screen.dart - hiển thị ảnh PNG trả về trực tiếp
// từ ApiClient.getBytes, không có model Dart riêng) và Chatbot hỗ trợ (screens/chatbot_screen.dart).
// Test CauHoiMau.fromJson (câu hỏi gợi ý tải từ API) và ChatMessage (tin nhắn hiển thị trong khung chat).
import 'package:flutter_test/flutter_test.dart';
import 'package:moneycare_app/models/models.dart';

void main() {
  group('CauHoiMau', () {
    test('fromJson đọc đúng 1 câu hỏi gợi ý từ API chatbot', () {
      final cauHoi = CauHoiMau.fromJson({
        "cauHoi": "Làm sao để thêm giao dịch mới?",
        "cauTraLoi": "Vào menu Giao dịch → bấm \"Thêm giao dịch\"...",
      });

      expect(cauHoi.cauHoi, "Làm sao để thêm giao dịch mới?");
      expect(cauHoi.cauTraLoi, contains("Thêm giao dịch"));
    });
  });

  group('ChatMessage', () {
    test('Phân biệt đúng tin nhắn của người dùng và của bot', () {
      final tinNhanNguoiDung = ChatMessage(noiDung: "Làm sao để đổi mật khẩu?", laNguoiDung: true);
      final tinNhanBot = ChatMessage(noiDung: "Vào Trang cá nhân → Đổi mật khẩu.", laNguoiDung: false);

      expect(tinNhanNguoiDung.laNguoiDung, true);
      expect(tinNhanBot.laNguoiDung, false);
      expect(tinNhanNguoiDung.noiDung, "Làm sao để đổi mật khẩu?");
    });
  });
}
