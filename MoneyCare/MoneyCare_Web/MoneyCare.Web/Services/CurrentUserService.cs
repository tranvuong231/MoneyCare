using MoneyCare.Models;
using MoneyCare.Web.Dtos;

namespace MoneyCare.Web.Services;

public interface ICurrentUserService
{
    Task<ApplicationUser?> GetAppUserAsync();
    Task<bool> LaAdminAsync();
    Task<int> SoChuaXemThongBaoAsync();
}

/// <summary>
/// Thay thế UserManager/DbContext trong _Layout.cshtml và mọi nơi khác cần thông tin nhanh về người
/// dùng đang đăng nhập - gọi API 1 lần rồi cache lại trong phạm vi 1 request (Scoped) để tránh gọi
/// API nhiều lần lặp lại khi nhiều phần của trang cùng cần dữ liệu này.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly MoneyCareApiClient _api;
    private ApplicationUser? _cached;
    private bool _daGoi;

    public CurrentUserService(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<ApplicationUser?> GetAppUserAsync()
    {
        if (_daGoi) return _cached;
        _daGoi = true;
        try
        {
            var dto = await _api.GetAsync<UserDto>("/api/auth/toi");
            _cached = dto == null ? null : UserMapper.TuDto(dto, _api.BaseUrl);
            _cachedDto = dto;
        }
        catch (ApiException)
        {
            _cached = null;
        }
        return _cached;
    }

    private UserDto? _cachedDto;

    public async Task<bool> LaAdminAsync()
    {
        await GetAppUserAsync();
        return _cachedDto?.LaAdmin == true;
    }

    public async Task<int> SoChuaXemThongBaoAsync()
    {
        try
        {
            var res = await _api.GetAsync<Dictionary<string, int>>("/api/thong-bao/so-chua-xem");
            return res != null && res.TryGetValue("soChuaXem", out var n) ? n : 0;
        }
        catch (ApiException)
        {
            return 0;
        }
    }
}
