// ============================================================================
// THÔNG BÁO NHẮC HÓA ĐƠN (LOCAL NOTIFICATIONS)
// ============================================================================
// Đây là thông báo CỤC BỘ - được lên lịch sẵn ngay trên máy điện thoại,
// không cần máy chủ đẩy (push server / Firebase Cloud Messaging).
// Với đồ án môn học, đây là cách đơn giản và đủ dùng để đáp ứng yêu cầu
// "tích hợp thông báo đẩy" nhắc hóa đơn mà không cần hạ tầng phức tạp.
// ============================================================================
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:timezone/timezone.dart' as tz;
import 'package:timezone/data/latest.dart' as tz_data;

class NotificationService {
  static final _plugin = FlutterLocalNotificationsPlugin();
  static bool _daKhoiTao = false;

  static Future<void> khoiTao() async {
    if (_daKhoiTao) return;
    tz_data.initializeTimeZones();

    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosInit = DarwinInitializationSettings();
    const settings = InitializationSettings(android: androidInit, iOS: iosInit);

    await _plugin.initialize(settings);

    // Xin quyền hiển thị thông báo (Android 13+ và iOS)
    await _plugin
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.requestNotificationsPermission();
    await _plugin
        .resolvePlatformSpecificImplementation<IOSFlutterLocalNotificationsPlugin>()
        ?.requestPermissions(alert: true, badge: true, sound: true);

    _daKhoiTao = true;
  }

  // Lên lịch nhắc thanh toán hóa đơn vào lúc 9h sáng của ngày đến hạn.
  // billId dùng làm notification id để có thể hủy/thay thế khi sửa hoặc xóa hóa đơn.
  //
  // LƯU Ý QUAN TRỌNG: dùng "inexactAllowWhileIdle" (không cần đúng giờ tuyệt đối) thay vì
  // "exactAllowWhileIdle", vì lịch CHÍNH XÁC trên Android 12+ đòi hỏi quyền đặc biệt
  // "Báo thức & lời nhắc" (SCHEDULE_EXACT_ALARM) mà người dùng phải tự bật tay trong Cài đặt
  // hệ thống - nếu chưa bật, gọi "exact" sẽ ném lỗi và làm dừng luôn cả việc lưu hóa đơn phía
  // trên (đây chính là lỗi "bấm Thêm không phản ứng gì" trước đây). Toàn bộ hàm cũng được bọc
  // try-catch để CHẮC CHẮN không bao giờ làm hỏng luồng lưu hóa đơn dù có lỗi gì xảy ra.
  static Future<void> datLichNhacHoaDon({
    required int billId,
    required String tenHoaDon,
    required double soTien,
    required DateTime ngayDenHan,
  }) async {
    try {
      await khoiTao();

      final gioNhac = tz.TZDateTime.from(
        DateTime(ngayDenHan.year, ngayDenHan.month, ngayDenHan.day, 9, 0),
        tz.local,
      );

      // Nếu giờ nhắc đã ở quá khứ (VD hạn hôm nay nhưng đã qua 9h sáng) thì không đặt lịch
      if (gioNhac.isBefore(tz.TZDateTime.now(tz.local))) return;

      const details = NotificationDetails(
        android: AndroidNotificationDetails(
          'moneycare_bills',
          'Nhắc hóa đơn MoneyCare',
          channelDescription: 'Thông báo nhắc đến hạn thanh toán hóa đơn',
          importance: Importance.high,
          priority: Priority.high,
        ),
        iOS: DarwinNotificationDetails(),
      );

      await _plugin.zonedSchedule(
        billId,
        'Hóa đơn sắp đến hạn: $tenHoaDon',
        'Số tiền ${soTien.toStringAsFixed(0)} ₫ - hạn hôm nay',
        gioNhac,
        details,
        androidScheduleMode: AndroidScheduleMode.inexactAllowWhileIdle,
        uiLocalNotificationDateInterpretation: UILocalNotificationDateInterpretation.absoluteTime,
      );
    } catch (e) {
      // Không để lỗi đặt lịch thông báo (thiếu quyền, lỗi plugin...) làm hỏng việc lưu hóa đơn
      // ignore: avoid_print
      print("NotificationService.datLichNhacHoaDon lỗi (bỏ qua, không ảnh hưởng lưu hóa đơn): $e");
    }
  }

  static Future<void> huyNhacHoaDon(int billId) async {
    try {
      await khoiTao();
      await _plugin.cancel(billId);
    } catch (e) {
      // ignore: avoid_print
      print("NotificationService.huyNhacHoaDon lỗi (bỏ qua): $e");
    }
  }
}
