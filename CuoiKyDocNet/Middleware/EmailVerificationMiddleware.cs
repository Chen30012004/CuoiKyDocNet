using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CuoiKyDocNet.Data
{
    public class EmailVerificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmailVerificationMiddleware> _logger;

        public EmailVerificationMiddleware(RequestDelegate next, ILogger<EmailVerificationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user != null && !user.EmailConfirmed)
                {
                    var path = context.Request.Path.Value.ToLower();
                    _logger.LogDebug("Checking path {Path} for user {Email} (EmailConfirmed: {EmailConfirmed}).", path, user.Email, user.EmailConfirmed);

                    // Bỏ qua các trang liên quan đến xác nhận email, đăng xuất, đăng ký, thay đổi mật khẩu và chỉnh sửa profile
                    if (!path.StartsWith("/account/verifyemail") &&
                        !path.StartsWith("/account/logout") &&
                        !path.StartsWith("/account/signup") &&
                        !path.StartsWith("/account/changepassword") &&
                        !path.StartsWith("/account/editprofile"))
                    {
                        _logger.LogInformation("Redirecting user {Email} to email verification from path {Path}.", user.Email, path);
                        context.Response.Redirect("/Account/VerifyEmail?email=" + user.Email);
                        return;
                    }
                    else
                    {
                        _logger.LogDebug("Skipping email verification redirect for path {Path} for user {Email}.", path, user.Email);
                    }
                }
                else if (user == null)
                {
                    _logger.LogWarning("User not found in EmailVerificationMiddleware for authenticated request.");
                }
            }
            else
            {
                _logger.LogDebug("User is not authenticated, skipping email verification middleware.");
            }

            await _next(context);
        }
    }

    public static class EmailVerificationMiddlewareExtensions
    {
        public static IApplicationBuilder UseEmailVerification(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EmailVerificationMiddleware>();
        }
    }
}