import 'package:flutter/material.dart';

import '../models/models.dart';
import '../services/services.dart';
import '../widgets/common_widgets.dart';

class AdminHomeScreen extends StatelessWidget {
  const AdminHomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 5,
      child: Scaffold(
        appBar: AppBar(
          title: const Text("Quản trị hệ thống"),
          bottom: const TabBar(
            isScrollable: true,
            tabs: [
              Tab(text: "Tổng quan"),
              Tab(text: "Người dùng"),
              Tab(text: "Hộ gia đình"),
              Tab(text: "Đơn Premium"),
              Tab(text: "Liên hệ"),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            _AdminDashboardTab(),
            _AdminUsersTab(),
            _AdminHouseholdsTab(),
            _AdminOrdersTab(),
            _AdminMessagesTab(),
          ],
        ),
      ),
    );
  }
}

class _AdminDashboardTab extends StatefulWidget {
  const _AdminDashboardTab();
  @override
  State<_AdminDashboardTab> createState() => _AdminDashboardTabState();
}

class _AdminDashboardTabState extends State<_AdminDashboardTab> {
  AdminDashboardModel? _d;
  bool _dangTai = true;
  String? _loi;

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() {
      _dangTai = true;
      _loi = null;
    });
    try {
      final d = await AdminService.dashboard();
      setState(() => _d = d);
    } catch (e) {
      setState(() => _loi = e.toString());
    } finally {
      setState(() => _dangTai = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_dangTai) return const DangTai();
    if (_loi != null) return ThongBaoLoi(noiDung: _loi!, onThuLai: _tai);
    final d = _d!;
    return RefreshIndicator(
      onRefresh: _tai,
      child: GridView.count(
        padding: const EdgeInsets.all(16),
        crossAxisCount: 2,
        mainAxisSpacing: 12,
        crossAxisSpacing: 12,
        childAspectRatio: 1.4,
        children: [
          TheThongKe(tieuDe: "Tổng người dùng", giaTri: "${d.tongNguoiDung}", icon: Icons.people, mau: mauChinh),
          TheThongKe(tieuDe: "Premium hiện tại", giaTri: "${d.tongNguoiDungPremium}", icon: Icons.workspace_premium, mau: Colors.amber.shade800),
          TheThongKe(tieuDe: "Người dùng mới tháng này", giaTri: "${d.nguoiDungMoiThangNay}", icon: Icons.person_add, mau: mauThu),
          TheThongKe(tieuDe: "Tổng giao dịch", giaTri: "${d.tongGiaoDich}", icon: Icons.receipt_long, mau: mauChinh),
          TheThongKe(tieuDe: "Tổng hộ gia đình", giaTri: "${d.tongHoGiaDinh}", icon: Icons.groups, mau: mauChinh),
          TheThongKe(tieuDe: "Doanh thu Premium", giaTri: tienVnd(d.tongDoanhThu), icon: Icons.attach_money, mau: mauThu),
          TheThongKe(tieuDe: "Đơn chờ xác nhận", giaTri: "${d.donChoXacNhan}", icon: Icons.hourglass_empty, mau: mauChi),
        ],
      ),
    );
  }
}

class _AdminUsersTab extends StatefulWidget {
  const _AdminUsersTab();
  @override
  State<_AdminUsersTab> createState() => _AdminUsersTabState();
}

class _AdminUsersTabState extends State<_AdminUsersTab> {
  List<AdminUserModel> _ds = [];
  bool _dangTai = true;
  final _tuKhoaCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() => _dangTai = true);
    try {
      final ds = await AdminService.danhSachUsers(tuKhoa: _tuKhoaCtrl.text.trim());
      setState(() => _ds = ds);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(AdminUserModel u) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa tài khoản"),
        content: Text("Xóa vĩnh viễn tài khoản \"${u.hoTen ?? u.email ?? u.userName}\"? Toàn bộ giao dịch, danh mục, hóa đơn... của người dùng này cũng sẽ bị xóa và không thể khôi phục."),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan != true) return;
    try {
      await AdminService.xoaNguoiDung(u.id);
      _tai();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text("Không thể xóa: $e")));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            controller: _tuKhoaCtrl,
            decoration: InputDecoration(
              hintText: "Tìm theo tên / email...",
              border: const OutlineInputBorder(),
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _tai),
            ),
            onSubmitted: (_) => _tai(),
          ),
        ),
        Expanded(
          child: _dangTai
              ? const DangTai()
              : ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                  itemCount: _ds.length,
                  itemBuilder: (_, i) {
                    final u = _ds[i];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        leading: CircleAvatar(backgroundColor: mauChinh.withValues(alpha:0.15), child: const Icon(Icons.person, color: mauChinh)),
                        title: Text(u.hoTen ?? u.userName ?? ''),
                        subtitle: Text(u.email ?? ''),
                        trailing: Wrap(
                          spacing: 4,
                          crossAxisAlignment: WrapCrossAlignment.center,
                          children: [
                            if (u.laAdmin) const Icon(Icons.admin_panel_settings, color: mauChinh, size: 18),
                            if (u.daXacMinhDanhTinh) const Icon(Icons.verified, color: Colors.blue, size: 18),
                            if (u.premiumConHieuLuc) const Icon(Icons.workspace_premium, color: Colors.amber, size: 18),
                            if (u.biKhoa) const Icon(Icons.lock, color: Colors.red, size: 18),
                            IconButton(
                              icon: const Icon(Icons.delete_outline, color: mauChi, size: 20),
                              onPressed: () => _xoa(u),
                            ),
                          ],
                        ),
                        onTap: () async {
                          await Navigator.of(context).push(MaterialPageRoute(builder: (_) => AdminUserDetailScreen(userId: u.id)));
                          _tai();
                        },
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}

class AdminUserDetailScreen extends StatefulWidget {
  final String userId;
  const AdminUserDetailScreen({super.key, required this.userId});

  @override
  State<AdminUserDetailScreen> createState() => _AdminUserDetailScreenState();
}

class _AdminUserDetailScreenState extends State<AdminUserDetailScreen> {
  AdminUserDetailModel? _u;
  bool _dangTai = true;

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() => _dangTai = true);
    try {
      final u = await AdminService.chiTietUser(widget.userId);
      setState(() => _u = u);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _capNhatPremium() async {
    final ctrl = TextEditingController(text: "1");
    final soThang = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Gia hạn Premium"),
        content: TextField(controller: ctrl, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: "Số tháng")),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text("Hủy")),
          ElevatedButton(onPressed: () => Navigator.pop(context, ctrl.text), child: const Text("Xác nhận")),
        ],
      ),
    );
    if (soThang != null) {
      await AdminService.capNhatPremium(widget.userId, int.tryParse(soThang) ?? 1);
      _tai();
    }
  }

  Future<void> _xoaTaiKhoan() async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa tài khoản"),
        content: Text("Xóa vĩnh viễn tài khoản \"${_u!.hoTen ?? _u!.email ?? ''}\"? Toàn bộ giao dịch, danh mục, hóa đơn... của người dùng này cũng sẽ bị xóa và không thể khôi phục."),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan != true) return;
    try {
      await AdminService.xoaNguoiDung(widget.userId);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text("Không thể xóa: $e")));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Chi tiết người dùng")),
      body: _dangTai
          ? const DangTai()
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(_u!.hoTen ?? _u!.userName ?? '', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                Text(_u!.email ?? '', style: const TextStyle(color: Colors.grey)),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(child: TheThongKe(tieuDe: "Tổng thu", giaTri: tienVnd(_u!.tongThu), icon: Icons.arrow_downward, mau: mauThu)),
                    const SizedBox(width: 8),
                    Expanded(child: TheThongKe(tieuDe: "Tổng chi", giaTri: tienVnd(_u!.tongChi), icon: Icons.arrow_upward, mau: mauChi)),
                  ],
                ),
                const SizedBox(height: 8),
                TheThongKe(tieuDe: "Số giao dịch", giaTri: "${_u!.soGiaoDich}", icon: Icons.receipt_long, mau: mauChinh),
                const SizedBox(height: 20),
                ListTile(
                  title: const Text("Premium"),
                  subtitle: Text(_u!.premiumConHieuLuc ? "Còn hiệu lực đến ${_u!.premiumHetHan != null ? ngayVn(_u!.premiumHetHan!) : ''}" : "Chưa đăng ký / đã hết hạn"),
                  trailing: Wrap(spacing: 4, children: [
                    TextButton(onPressed: _capNhatPremium, child: const Text("Gia hạn")),
                    if (_u!.isPremium)
                      TextButton(
                        onPressed: () async {
                          await AdminService.huyPremium(widget.userId);
                          _tai();
                        },
                        child: const Text("Hủy", style: TextStyle(color: Colors.red)),
                      ),
                  ]),
                ),
                ListTile(
                  title: const Text("Trạng thái tài khoản"),
                  subtitle: Text(_u!.biKhoa ? "Đã khóa" : "Đang hoạt động"),
                  trailing: TextButton(
                    onPressed: () async {
                      if (_u!.biKhoa) {
                        await AdminService.moKhoaTaiKhoan(widget.userId);
                      } else {
                        await AdminService.khoaTaiKhoan(widget.userId);
                      }
                      _tai();
                    },
                    child: Text(_u!.biKhoa ? "Mở khóa" : "Khóa", style: TextStyle(color: _u!.biKhoa ? mauThu : mauChi)),
                  ),
                ),
                ListTile(
                  title: const Text("Xác minh danh tính (tích xanh)"),
                  subtitle: Text(_u!.daXacMinhDanhTinh ? "Đã xác minh" : "Chưa xác minh"),
                  trailing: TextButton(
                    onPressed: () async {
                      if (_u!.daXacMinhDanhTinh) {
                        await AdminService.huyXacMinh(widget.userId);
                      } else {
                        await AdminService.capXacMinh(widget.userId);
                      }
                      _tai();
                    },
                    child: Text(_u!.daXacMinhDanhTinh ? "Hủy xác minh" : "Cấp xác minh", style: TextStyle(color: _u!.daXacMinhDanhTinh ? mauChi : mauThu)),
                  ),
                ),
                ListTile(
                  title: const Text("Quyền Admin"),
                  subtitle: Text(_u!.laAdmin ? "Là Admin" : "Người dùng thường"),
                  trailing: TextButton(
                    onPressed: () async {
                      if (_u!.laAdmin) {
                        await AdminService.thuHoiQuyenAdmin(widget.userId);
                      } else {
                        await AdminService.capQuyenAdmin(widget.userId);
                      }
                      _tai();
                    },
                    child: Text(_u!.laAdmin ? "Thu hồi" : "Cấp quyền"),
                  ),
                ),
                const SizedBox(height: 20),
                Card(
                  color: Colors.red.shade50,
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text("Khu vực nguy hiểm", style: TextStyle(fontWeight: FontWeight.bold, color: Colors.red)),
                        const SizedBox(height: 6),
                        const Text(
                          "Xóa vĩnh viễn tài khoản này cùng toàn bộ giao dịch, danh mục, hóa đơn, liên kết ngân hàng và đơn Premium liên quan.",
                          style: TextStyle(fontSize: 12, color: Colors.black54),
                        ),
                        const SizedBox(height: 8),
                        OutlinedButton.icon(
                          onPressed: _xoaTaiKhoan,
                          style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                          icon: const Icon(Icons.delete_outline),
                          label: const Text("Xóa tài khoản này"),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

class _AdminHouseholdsTab extends StatefulWidget {
  const _AdminHouseholdsTab();
  @override
  State<_AdminHouseholdsTab> createState() => _AdminHouseholdsTabState();
}

class _AdminHouseholdsTabState extends State<_AdminHouseholdsTab> {
  List<AdminHouseholdModel> _ds = [];
  bool _dangTai = true;
  final _tuKhoaCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() => _dangTai = true);
    try {
      final ds = await AdminService.danhSachHouseholds(tuKhoa: _tuKhoaCtrl.text.trim());
      setState(() => _ds = ds);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(AdminHouseholdModel h) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa hộ gia đình"),
        content: Text("Xóa hộ gia đình \"${h.tenHoGiaDinh}\"? Toàn bộ thành viên sẽ về trạng thái không có hộ."),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan == true) {
      await AdminService.xoaHouseholdAdmin(h.id);
      _tai();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            controller: _tuKhoaCtrl,
            decoration: InputDecoration(
              hintText: "Tìm theo tên hộ / mã mời...",
              border: const OutlineInputBorder(),
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _tai),
            ),
            onSubmitted: (_) => _tai(),
          ),
        ),
        Expanded(
          child: _dangTai
              ? const DangTai()
              : RefreshIndicator(
                  onRefresh: _tai,
                  child: ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    itemCount: _ds.length,
                    itemBuilder: (_, i) {
                      final h = _ds[i];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 8),
                        child: ListTile(
                          leading: const Icon(Icons.groups, color: mauChinh),
                          title: Text(h.tenHoGiaDinh),
                          subtitle: Text("Chủ hộ: ${h.tenChuHo ?? 'N/A'} • ${h.soThanhVien} thành viên • Mã: ${h.maMoi}"),
                          trailing: IconButton(icon: const Icon(Icons.delete_outline, color: mauChi), onPressed: () => _xoa(h)),
                        ),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }
}

class _AdminOrdersTab extends StatefulWidget {
  const _AdminOrdersTab();
  @override
  State<_AdminOrdersTab> createState() => _AdminOrdersTabState();
}

class _AdminOrdersTabState extends State<_AdminOrdersTab> {
  List<AdminPremiumOrderModel> _donHang = [];
  bool _dangTai = true;
  final _tuKhoaCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() => _dangTai = true);
    try {
      final dh = await AdminService.danhSachPremiumOrders(tuKhoa: _tuKhoaCtrl.text.trim());
      setState(() => _donHang = dh);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  Future<void> _xoa(AdminPremiumOrderModel d) async {
    final xacNhan = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text("Xóa đơn Premium"),
        content: Text("Xóa đơn #${d.id} (${d.maGiaoDich})?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text("Hủy")),
          TextButton(onPressed: () => Navigator.pop(context, true), child: const Text("Xóa", style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (xacNhan == true) {
      await AdminService.xoaDon(d.id);
      _tai();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            controller: _tuKhoaCtrl,
            decoration: InputDecoration(
              hintText: "Tìm theo email / mã giao dịch...",
              border: const OutlineInputBorder(),
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _tai),
            ),
            onSubmitted: (_) => _tai(),
          ),
        ),
        Expanded(
          child: _dangTai
              ? const DangTai()
              : RefreshIndicator(
                  onRefresh: _tai,
                  child: _donHang.isEmpty
                      ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Center(child: Text("Chưa có đơn đăng ký Premium nào")))])
                      : ListView(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          children: _donHang
                              .map((d) => Card(
                                    child: ListTile(
                                      title: Text("${d.emailNguoiDung ?? ''} - ${tienVnd(d.soTien)}"),
                                      subtitle: Text("Mã GD: ${d.maGiaoDich} • ${d.soThangDangKy} tháng • ${d.daXacNhan ? 'Đã xác nhận' : 'Chờ xác nhận'}"),
                                      trailing: Wrap(spacing: 4, children: [
                                        if (!d.daXacNhan)
                                          TextButton(
                                            onPressed: () async {
                                              await AdminService.xacNhanDon(d.id);
                                              _tai();
                                            },
                                            child: const Text("Xác nhận"),
                                          )
                                        else
                                          const Icon(Icons.check_circle, color: mauThu),
                                        IconButton(icon: const Icon(Icons.delete_outline, color: mauChi), onPressed: () => _xoa(d)),
                                      ]),
                                    ),
                                  ))
                              .toList(),
                        ),
                ),
        ),
      ],
    );
  }
}

class _AdminMessagesTab extends StatefulWidget {
  const _AdminMessagesTab();
  @override
  State<_AdminMessagesTab> createState() => _AdminMessagesTabState();
}

class _AdminMessagesTabState extends State<_AdminMessagesTab> {
  List<AdminContactMessageModel> _tinNhan = [];
  bool _dangTai = true;
  final _tuKhoaCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _tai();
  }

  Future<void> _tai() async {
    setState(() => _dangTai = true);
    try {
      final tn = await AdminService.danhSachContactMessages(tuKhoa: _tuKhoaCtrl.text.trim());
      setState(() => _tinNhan = tn);
    } finally {
      setState(() => _dangTai = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: TextField(
            controller: _tuKhoaCtrl,
            decoration: InputDecoration(
              hintText: "Tìm theo tên / email...",
              border: const OutlineInputBorder(),
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _tai),
            ),
            onSubmitted: (_) => _tai(),
          ),
        ),
        Expanded(
          child: _dangTai
              ? const DangTai()
              : RefreshIndicator(
                  onRefresh: _tai,
                  child: _tinNhan.isEmpty
                      ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Center(child: Text("Chưa có tin nhắn liên hệ nào")))])
                      : ListView(
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          children: _tinNhan
                              .map((t) => Card(
                                    child: ListTile(
                                      title: Text(t.hoTen),
                                      subtitle: Text("${t.email}\n${t.noiDung}", maxLines: 3, overflow: TextOverflow.ellipsis),
                                      isThreeLine: true,
                                      trailing: Wrap(spacing: 4, children: [
                                        IconButton(
                                          icon: Icon(t.daXem ? Icons.mark_email_read : Icons.mark_email_unread, color: t.daXem ? mauThu : Colors.orange),
                                          onPressed: () async {
                                            await AdminService.danhDauDaXem(t.id);
                                            _tai();
                                          },
                                        ),
                                        IconButton(
                                          icon: const Icon(Icons.delete_outline, color: mauChi),
                                          onPressed: () async {
                                            await AdminService.xoaTinNhan(t.id);
                                            _tai();
                                          },
                                        ),
                                      ]),
                                    ),
                                  ))
                              .toList(),
                        ),
                ),
        ),
      ],
    );
  }
}
