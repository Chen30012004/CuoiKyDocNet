using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CuoiKyDocNet.Models;
using System.Threading.Tasks;

namespace CuoiKyDocNet.Middleware
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
                _logger.LogInformation("User is authenticated, checking email verification.");
                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    _logger.LogInformation("User found, Email: {Email}, EmailConfirmed: {EmailConfirmed}", user.Email, user.EmailConfirmed);
                    if (!user.EmailConfirmed)
                    {
                        if (!context.Request.Path.StartsWithSegments("/Account/VerifyCode") &&
                            !context.Request.Path.StartsWithSegments("/Account/Logout") &&
                            !context.Request.Path.StartsWithSegments("/Account/SignUp"))
                        {
                            _logger.LogInformation("Redirecting to VerifyCode for user: {Email}", user.Email);
                            context.Response.Redirect("/Account/VerifyCode");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("User {Email} is verified, proceeding.", user.Email);
                    }
                }
                else
                {
                    _logger.LogWarning("User not found, redirecting to Login.");
                    context.Response.Redirect("/Account/Login");
                    return;
                }
            }
            await _next(context);
        }
    }

    // Extension method để dễ dàng đăng ký middleware trong Program.cs
    public static class EmailVerificationMiddlewareExtensions
    {
        public static IApplicationBuilder UseEmailVerification(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EmailVerificationMiddleware>();
        }
    }
}