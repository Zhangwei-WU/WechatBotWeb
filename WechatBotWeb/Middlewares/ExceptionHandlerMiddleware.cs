using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WechatBotWeb.Common;

namespace WechatBotWeb.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate next;
        private IApplicationInsights insight;

        public ExceptionHandlerMiddleware(RequestDelegate next, IApplicationInsights insight)
        {
            this.next = next;
            this.insight = insight;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                var shouldLogException = true;
                var ex = e;
                while (ex is ApplicationInsightsAlreadyInspectedException)
                {
                    shouldLogException = false;
                    ex = e.InnerException;
                }

                if (shouldLogException)
                {
                    insight.Exception(ex, "ExceptionHandler");
                }

                if (context.Response.HasStarted)
                {
                    throw;
                }

                if (ex is HttpStatusException)
                {
                }
                else
                {

                }
            }
        }
    }

    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
