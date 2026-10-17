using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Filters;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;
using MoneyCare.ViewModels;

namespace MoneyCare.Api.Controllers;

[Route("api/insights")]
[RequirePremiumApi]
public class InsightsController : BaseApiController
{
    private readonly IAiInsightService _aiInsightService;
    private readonly ILlmService _llmService;
    private readonly ApplicationDbContext _context;

    public InsightsController(IAiInsightService aiInsightService, ILlmService llmService, ApplicationDbContext context)
    {
        _aiInsightService = aiInsightService;
        _llmService = llmService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> PhanTich()
    {
        var vm = await _aiInsightService.PhanTichAsync(UserId);

        try
        {
            var heThong =
                "Bạn là chuyên gia tư vấn tài chính cá nhân. Dựa trên số liệu được cung cấp, hãy đưa ra " +
                "DUY NHẤT 1 gợi ý tiết kiệm/quản lý chi tiêu ngắn gọn (1-2 câu, tối đa 60 từ), bằng tiếng Việt, " +
                "không dùng thẻ HTML/Markdown, không lặp lại số liệu, đi thẳng vào lời khuyên thực tế.";

            var cauHoi =
                $"Số dư hiện tại: {vm.DuDoan.SoDuHienTai:N0}đ. " +
                $"Chi tiêu trung bình/ngày: {vm.DuDoan.ChiTieuTrungBinhNgay:N0}đ. " +
                $"Thu nhập trung bình/ngày: {vm.DuDoan.ThuNhapTrungBinhNgay:N0}đ. " +
                $"Dự đoán số dư cuối tháng: {vm.DuDoan.SoDuDuDoanCuoiThang:N0}đ. Xu hướng: {vm.DuDoan.XuHuong}.";

            var goiYAi = await _llmService.HoiAsync(heThong, cauHoi);
            if (!string.IsNullOrWhiteSpace(goiYAi))
            {
                vm.GoiYTietKiem.Insert(0, new InsightItem
                {
                    Icon = "bi-stars",
                    MucDo = "info",
                    TieuDe = "Gợi ý từ AI (LM Studio)",
                    NoiDung = goiYAi
                });
            }
        }
        catch { /* Không để lỗi gọi AI làm hỏng toàn bộ API */ }

        var canhBaoNguyHiem = vm.ThoiQuenChiTieu.Concat(vm.GoiYTietKiem).FirstOrDefault(i => i.MucDo == "danger");
        if (canhBaoNguyHiem != null)
        {
            const string tieuDe = "Cảnh báo chi tiêu từ AI";
            if (!await ThongBaoHelper.DaCoHomNayAsync(_context, UserId, tieuDe))
            {
                await ThongBaoHelper.TaoAsync(_context, UserId, tieuDe, canhBaoNguyHiem.NoiDung, LoaiThongBao.PhanTichAI, "/insights");
            }
        }

        return Ok(vm);
    }
}
