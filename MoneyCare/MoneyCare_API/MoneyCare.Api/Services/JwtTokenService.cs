// Sinh JWT (JSON Web Token) sau khi đăng nhập/đăng ký thành công. Token này được Web và
// Flutter App cùng dùng để gọi các API cần đăng nhập (đính kèm header Authorization: Bearer <token>).
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MoneyCare.Models;

namespace MoneyCare.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string TaoToken(ApplicationUser user, IList<string> roles)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        // QUAN TRỌNG: dùng chuỗi ngắn "role" (không phải hằng số ClaimTypes.Role, vốn là 1 URI dài)
        // - JwtSecurityToken ghi claim vào token đúng y tên Type đã cho, không tự rút gọn. Nếu dùng
        // ClaimTypes.Role, khi Web tự đọc token để lấy quyền Admin (xem AccountController.DocRoleTuJwt)
        // sẽ tìm chữ "role" mà không thấy, khiến trang Admin bị chặn dù tài khoản đúng là Admin.
        // Khi MoneyCare_API validate token, JwtBearer TỰ ĐỘNG map "role" -> ClaimTypes.Role (mặc định
        // MapInboundClaims = true) nên [Authorize(Roles = "Admin")] phía API vẫn hoạt động bình thường.
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var soNgayHetHan = jwtConfig.GetValue<int?>("ExpireDays") ?? 30;

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(soNgayHetHan),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
