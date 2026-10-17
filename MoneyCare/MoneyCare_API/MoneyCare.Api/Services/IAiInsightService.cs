// Giao diện định nghĩa các chức năng phân tích tài chính bằng AI.
using MoneyCare.ViewModels;

namespace MoneyCare.Services;

public interface IAiInsightService
{
    Task<InsightsViewModel> PhanTichAsync(string userId);
}
