namespace WechatBotWeb.Middlewares
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using WechatBotWeb.Common;

    public class ErrorResponse
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
    }

    public class ExceptionHandlingOptions
    {
        public bool HideErrorMessage { get; set; }
    }

    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ExceptionHandlingOptions options;
        private readonly IApplicationInsights insight;

        public ExceptionHandlingMiddleware(RequestDelegate next, ExceptionHandlingOptions options, IApplicationInsights insight)
        {
            this.next = next;
            this.options = options;
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
                Exception ex = e as ApplicationInsightsAlreadyInspectedException;
                if (ex != null) ex = e.InnerException;
                else insight.Exception(e, "ExceptionHandlingMiddleware");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                var errorResponse = new ErrorResponse
                {
                    CorrelationId = CallContext.ClientContext.CorrelationId,
                    Message = options.HideErrorMessage ? "$(GeneralErrorMessage)" : e.Message,
                    Status = (int)((e as HttpStatusException)?.Status ?? StatusCode.InternalServerError)
                };
                
                context.Response.StatusCode = errorResponse.Status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(errorResponse));
            }
        }
    }

    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
