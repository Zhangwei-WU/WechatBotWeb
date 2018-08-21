namespace WechatBotWeb.Middlewares
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;

    public class JwtAuthenticationMiddleware
    {

        private readonly RequestDelegate next;
        private IAuthenticationService authService;
        private IApplicationInsights insight;

        public JwtAuthenticationMiddleware(RequestDelegate next, IAuthenticationService service, IApplicationInsights insight)
        {
            this.next = next;
            this.authService = service;
            this.insight = insight;
        }

        public async Task Invoke(HttpContext context)
        {
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization))
            {
                var schemeIndex = authorization.IndexOf(' ');
                if (schemeIndex == -1)
                {
                    insight.Error("JwtAuthenticationMiddleware", "Invalid({0})", authorization);
                }
                else
                {
                    var scheme = authorization.Substring(0, schemeIndex);
                    var token = authorization.Substring(schemeIndex + 1);

                    var identity = await insight.WatchAsync(
                        async () => await authService.ValidateTokenAsync(CallContext.ClientContext, scheme, token), 
                        (r, e) => e.EventStatus = (r != null && r.IsAuthenticated).ToString(), 
                        "JwtAuthenticationMiddleware");

                    if (identity == null || !identity.IsAuthenticated)
                    {
                        insight.Error("JwtAuthenticationMiddleware", "Unauthorized({0})", authorization);
                    }
                    else
                    {
                        var principal = new ClaimsPrincipal(identity);
                        context.User = principal;
                    }
                }
            }

            await next(context);
        }
    }

    public static class JwtAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}
