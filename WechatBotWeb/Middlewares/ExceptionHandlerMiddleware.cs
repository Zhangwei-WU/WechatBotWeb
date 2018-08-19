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
            catch(HttpStatusException ex)
            {
                insight.Exception(ex, "ExceptionHandler");
                // output client friendly body
            }
            catch (Exception ex)
            {
                insight.Exception(ex, "ExceptionHandler");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                // output general error 500
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
