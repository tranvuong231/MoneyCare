// Danh sách BIN ngân hàng theo chuẩn VietQR/NAPAS - đủ dùng cho đồ án.
// Người dùng chỉ cần chọn TÊN ngân hàng, không cần biết mã BIN là gì.
class NganHang {
  final String ten;
  final String bin;
  const NganHang(this.ten, this.bin);
}

const List<NganHang> danhSachNganHang = [
  NganHang("Vietcombank", "970436"),
  NganHang("VietinBank", "970415"),
  NganHang("BIDV", "970418"),
  NganHang("Agribank", "970405"),
  NganHang("Techcombank", "970407"),
  NganHang("MB Bank", "970422"),
  NganHang("ACB", "970416"),
  NganHang("VPBank", "970432"),
  NganHang("TPBank", "970423"),
  NganHang("Sacombank", "970403"),
  NganHang("HDBank", "970437"),
  NganHang("SHB", "970443"),
  NganHang("VIB", "970441"),
  NganHang("Eximbank", "970431"),
  NganHang("MSB", "970426"),
  NganHang("SeABank", "970440"),
  NganHang("OCB", "970448"),
  NganHang("SCB", "970429"),
  NganHang("Nam A Bank", "970428"),
  NganHang("BAOVIET Bank", "970438"),
  NganHang("Bac A Bank", "970409"),
  NganHang("PVcomBank", "970412"),
  NganHang("VietBank", "970433"),
  NganHang("Public Bank Vietnam (PBVN)", "970439"),
  NganHang("Woori Bank", "970457"),
  NganHang("CB (CBBank)", "970444"),
];
