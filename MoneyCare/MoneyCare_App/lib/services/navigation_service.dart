// Cho phép điều hướng từ những nơi KHÔNG có BuildContext (VD: AuthProvider khi phát hiện phiên
// đăng nhập hết hạn giữa lúc đang dùng app) - dùng để bật hết ngăn xếp màn hình đang mở về lại
// màn gốc, sau khi trạng thái đăng nhập đã được reset về "chưa đăng nhập".
import 'package:flutter/material.dart';

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();
