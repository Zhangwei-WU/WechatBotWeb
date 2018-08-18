namespace WechatBotWeb.Middlewares
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;
    using WechatBotWeb.Common;

    public class ClientInfoMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IApplicationInsights insight;
        public ClientInfoMiddleware(RequestDelegate next, IApplicationInsights insight)
        {
            this.next = next;
            this.insight = insight;
        }

        public async Task Invoke(HttpContext context)
        {
            var newDeviceId = !context.Request.Cookies.TryGetValue(GlobalVariables.DeviceIdName, out string deviceId) || string.IsNullOrEmpty(deviceId);
            if (newDeviceId) deviceId = Guid.NewGuid().ToString("N").ToLowerInvariant();

            var newSessionId = !context.Request.Cookies.TryGetValue(GlobalVariables.SessionIdName, out string sessionId) || string.IsNullOrEmpty(sessionId);
            if (newSessionId) sessionId = DateTime.UtcNow.ToBinary().ToString("X16");

            CallContext.ClientContext = new ClientContext
            {
                ClientDeviceId = deviceId,
                ClientSessionId = sessionId,
                CorrelationId = Guid.NewGuid().ToString(),
                IP = null
            };

            if (newDeviceId) insight.Event(ApplicationInsightEventNames.EventClientNewDeviceIdSource);
            if (newSessionId) insight.Event(ApplicationInsightEventNames.EventClientNewSessionIdSource);

            context.Response.Cookies.Append(
                GlobalVariables.DeviceIdName,
                deviceId,
                new CookieOptions
                {
                    Domain = context.Request.Host.Host,
                    IsEssential = true,
                    Path = GlobalVariables.WebApiPath,
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddYears(1)
                });

            context.Response.Cookies.Append(
                GlobalVariables.SessionIdName,
                sessionId,
                new CookieOptions
                {
                    Domain = context.Request.Host.Host,
                    IsEssential = true,
                    Path = GlobalVariables.WebApiPath,
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true
                });


            using (var watch = insight.Watch("RequestProcessTime"))
            {
                await next.Invoke(context);
                watch.Properties.Add("StatusCode", context.Response.StatusCode.ToString());
            }
        }
    }

    public static class ClientInfoMiddlewareExtensions
    {
        public static IApplicationBuilder UseClientInfoMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientInfoMiddleware>();
        }
    }
}
