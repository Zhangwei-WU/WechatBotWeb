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
        private IApplicationInsights insight;

        public ExceptionHandlingMiddleware(RequestDelegate next, IApplicationInsights insight)
        {
            this.next = next;
            this.insight = insight;
        }

        public async Task Invoke(HttpContext context, ExceptionHandlingOptions options)
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

                var errorResponse = new ErrorResponse
                {
                    CorrelationId = CallContext.ClientContext.CorrelationId,
                };

                var httpStatusException = ex as HttpStatusException;

                if (httpStatusException != null)
                {
                    errorResponse.Status = (int)httpStatusException.Status;
                    errorResponse.Message = httpStatusException.Message;
                }
                else
                {
                    errorResponse.Status = 500;
                    errorResponse.Message = ex.Message;
                }

                if (!options.HideErrorMessage) errorResponse.Message = "$(GeneralErrorMessage)";

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
