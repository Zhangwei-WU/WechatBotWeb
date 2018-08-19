namespace WechatBotWeb.Middlewares
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;

    public class JwtAuthenticationMiddleware
    {

        private readonly RequestDelegate next;
        private IAuthenticationService authService;
        private IApplicationInsights insight;

        public JwtAuthenticationMiddleware(RequestDelegate next, IAuthenticationService service, IApplicationInsights insights)
        {
            this.next = next;
            this.authService = service;
        }

        public async Task Invoke(HttpContext context)
        {
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization))
            {
                var schemeIndex = authorization.IndexOf(' ');
                if (schemeIndex == -1)
                {
                    insight.Error("JwtAuthenticationMiddleware", "BadFormat Authorization: {0}", authorization);
                }
                else
                {
                    var scheme = authorization.Substring(0, schemeIndex);
                    var token = authorization.Substring(schemeIndex + 1);
                    var identity = await authService.ValidateTokenAsync(CallContext.ClientContext, scheme, token);
                    if (identity == null)
                    {
                        insight.Error("JwtAuthenticationMiddleware", "Error Authorization: {0}", authorization);
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
