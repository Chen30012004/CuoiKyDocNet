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
                    if (!path.StartsWith("/account/verifyemail") &&
                        !path.StartsWith("/account/logout") &&
                        !path.StartsWith("/account/signup"))
                    {
                        _logger.LogInformation("Redirecting user {Email} to email verification.", user.Email);
                        context.Response.Redirect("/Account/VerifyEmail?email=" + user.Email);
                        return;
                    }
                }
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