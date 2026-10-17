import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/auth_provider.dart';
import 'providers/theme_provider.dart';
import 'widgets/common_widgets.dart';
import 'services/notification_service.dart';
import 'services/navigation_service.dart';

import 'screens/auth_screens.dart';
import 'screens/dashboard_screen.dart';
import 'screens/transactions_screens.dart';
import 'screens/categories_screens.dart';
import 'screens/bills_screens.dart';
import 'screens/reports_screen.dart';
import 'screens/insights_screen.dart';
import 'screens/chatbot_screen.dart';
import 'screens/household_screen.dart';
import 'screens/integrations_screen.dart';
import 'screens/premium_screen.dart';
import 'screens/payment_screen.dart';
import 'screens/ocr_screen.dart';
import 'screens/profile_screens.dart';
import 'screens/settings_contact_screens.dart';
import 'screens/admin_screens.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  NotificationService.khoiTao();
  runApp(const MoneyCareApp());
}

class MoneyCareApp extends StatelessWidget {
  const MoneyCareApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
      ],
      child: Consumer<ThemeProvider>(
        builder: (context, themeProvider, _) => MaterialApp(
          navigatorKey: navigatorKey,
          title: 'MoneyCare',
          debugShowCheckedModeBanner: false,
          themeMode: themeProvider.themeMode,
          theme: ThemeData(
            useMaterial3: true,
            colorSchemeSeed: mauChinh,
            brightness: Brightness.light,
            scaffoldBackgroundColor: const Color(0xFFF8FAFC),
            appBarTheme: const AppBarTheme(backgroundColor: mauChinh, foregroundColor: Colors.white, elevation: 0),
          ),
          darkTheme: ThemeData(
            useMaterial3: true,
            colorSchemeSeed: mauChinh,
            brightness: Brightness.dark,
            scaffoldBackgroundColor: const Color(0xFF121212),
            appBarTheme: const AppBarTheme(backgroundColor: Color(0xFF0F766E), foregroundColor: Colors.white, elevation: 0),
          ),
          // Trang khởi động dựa vào trạng thái đăng nhập (xem _CongXacThuc bên dưới)
          home: const _CongXacThuc(),
          routes: {
            '/dang-nhap': (_) => const LoginScreen(),
            '/dang-ky': (_) => const RegisterScreen(),
            '/quen-mat-khau': (_) => const ForgotPasswordScreen(),
            '/transactions': (_) => const TransactionListScreen(),
            '/categories': (_) => const CategoryListScreen(),
            '/bills': (_) => const BillListScreen(),
            '/reports': (_) => const ReportScreen(),
            '/insights': (_) => const InsightsScreen(),
            '/chatbot': (_) => const ChatbotScreen(),
            '/household': (_) => const HouseholdScreen(),
            '/integrations': (_) => const IntegrationsScreen(),
            '/premium': (_) => const PremiumScreen(),
            '/payment': (_) => const PaymentScreen(),
            '/ocr': (_) => const OcrScreen(),
            '/settings': (_) => const SettingsScreen(),
            '/contact': (_) => const ContactScreen(),
            '/admin': (_) => const AdminHomeScreen(),
            '/profile': (_) => const ProfileScreen(),
          },
        ),
      ),
    );
  }
}

// "Cổng" quyết định hiển thị màn hình đăng nhập hay vào thẳng ứng dụng,
// dựa vào việc đã có JWT token hợp lệ được lưu trên máy hay chưa.
class _CongXacThuc extends StatelessWidget {
  const _CongXacThuc();

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    // Đồng bộ Dark mode đã lưu của người dùng vào giao diện thật của app
    if (auth.nguoiDung != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        context.read<ThemeProvider>().dat(auth.nguoiDung!.giaoDienToi);
      });
    }

    switch (auth.trangThai) {
      case TrangThaiXacThuc.dangTai:
        return const Scaffold(body: DangTai());
      case TrangThaiXacThuc.chuaDangNhap:
        return const LoginScreen();
      case TrangThaiXacThuc.daDangNhap:
        return const MainShell();
    }
  }
}

// Khung chính của app: thanh điều hướng dưới cùng (bottom navigation) với 5 tab.
class MainShell extends StatefulWidget {
  const MainShell({super.key});
  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _tabHienTai = 0;

  // Mỗi tab có 1 Key riêng; mỗi khi CHUYỂN SANG một tab, key của tab đó được tạo mới,
  // buộc widget bị hủy và dựng lại từ đầu (initState chạy lại) -> luôn lấy dữ liệu/mới nhất
  // (VD: vừa nâng cấp Premium xong, bấm qua lại tab AI sẽ vào được ngay, không cần đăng xuất).
  final List<Key> _keys = List.generate(5, (_) => UniqueKey());

  List<Widget> _xayManHinh() => [
        DashboardScreen(key: _keys[0]),
        TransactionListScreen(key: _keys[1]),
        ReportScreen(key: _keys[2]),
        InsightsScreen(key: _keys[3]),
        ProfileScreen(key: _keys[4]),
      ];

  void _chuyenTab(int i) {
    if (i != _tabHienTai) {
      setState(() {
        _keys[i] = UniqueKey();
        _tabHienTai = i;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(index: _tabHienTai, children: _xayManHinh()),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tabHienTai,
        onDestinationSelected: _chuyenTab,
        destinations: const [
          NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home), label: "Trang chủ"),
          NavigationDestination(icon: Icon(Icons.receipt_long_outlined), selectedIcon: Icon(Icons.receipt_long), label: "Giao dịch"),
          NavigationDestination(icon: Icon(Icons.bar_chart_outlined), selectedIcon: Icon(Icons.bar_chart), label: "Báo cáo"),
          NavigationDestination(icon: Icon(Icons.auto_awesome_outlined), selectedIcon: Icon(Icons.auto_awesome), label: "AI"),
          NavigationDestination(icon: Icon(Icons.person_outline), selectedIcon: Icon(Icons.person), label: "Cá nhân"),
        ],
      ),
    );
  }
}
