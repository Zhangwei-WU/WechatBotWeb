namespace WechatBotWeb.Middlewares
{
    using System;
    using System.Threading.Tasks;
    using WechatBotWeb.Common;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.ApplicationInsights.DataContracts;

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
            var newDeviceId = !context.Request.Cookies.TryGetValue(GlobalVariables.DeviceIdName, out string deviceId) || string.IsNullOrEmpty(deviceId) || deviceId.Length != 32;
            if (newDeviceId) deviceId = Guid.NewGuid().ToString("N").ToLowerInvariant();

            var newSessionId = !context.Request.Cookies.TryGetValue(GlobalVariables.SessionIdName, out string sessionId) || string.IsNullOrEmpty(sessionId);
            if (newSessionId) sessionId = DateTime.UtcNow.ToBinary().ToString("X16").ToLowerInvariant() + deviceId.Substring(0, 16);

            var telemetry = context.Features.Get<RequestTelemetry>();

            CallContext.ClientContext = new ClientContext
            {
                ClientDeviceId = deviceId,
                ClientSessionId = sessionId,
                CorrelationId = telemetry?.Context.Operation.Id ?? Guid.NewGuid().ToString("N").ToLowerInvariant() + "|" + sessionId
            };

            telemetry.Context.Session.Id = sessionId;
            telemetry.Context.Properties["Device Id"] = deviceId;

            if (newDeviceId) insight.Event(ApplicationInsightConstants.EventClientNewDeviceIdSource);
            if (newSessionId) insight.Event(ApplicationInsightConstants.EventClientNewSessionIdSource);

            context.Response.Cookies.Append(
                GlobalVariables.DeviceIdName,
                deviceId,
                new CookieOptions
                {
                    //Domain = context.Request.Host.Host,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddYears(1)
                });

            context.Response.Cookies.Append(
                GlobalVariables.SessionIdName,
                sessionId,
                new CookieOptions
                {
                    //Domain = context.Request.Host.Host,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Strict,
                    HttpOnly = false
                });

            await next(context);
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
