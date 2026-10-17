// Bản API của RequirePremiumAttribute gốc (bên Web MVC cũ chuyển hướng sang trang giá) -
// ở đây không có "trang" để chuyển hướng, nên trả về mã lỗi HTTP 402 Payment Required kèm
// { "loi": "..." } - ApiClient bên Flutter/Web tự nhận diện mã 402 để hiển thị đúng thông báo
// "Tính năng này chỉ dành cho tài khoản Premium" (xem services/api_client.dart).
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MoneyCare.Models;

namespace MoneyCare.Api.Filters;

public class RequirePremiumApiAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.HttpContext.User);

        if (user == null || !user.PremiumConHieuLuc)
        {
            context.Result = new ObjectResult(new { loi = "Tính năng này chỉ dành cho tài khoản Premium. Hãy nâng cấp để mở khóa!" })
            {
                StatusCode = 402
            };
            return;
        }

        await next();
    }
}
