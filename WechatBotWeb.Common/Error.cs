namespace WechatBotWeb.Common
{
    using System;
    


    public interface IError : IStatus
    {
        int ErrorCode { get; set; }
        string Message { get; set; }
        string SourceMemberName { get; set; }
        string SourceFilePath { get; set; }
        int SourceLineNumber { get; set; }
        Exception ErrorDetails { get; set; }
    }

    public abstract class AbstractError<T> : IError where T : class, IError, new()
    {
        public StatusCode Status { get; set; } 

        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public string SourceMemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
        public Exception ErrorDetails { get; set; }

        public static T Error(int errorCode, string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            return new T
            {
                Status = StatusCode.InternalServerError,
                ErrorCode = errorCode,
                Message = message,
                SourceMemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber
            };
        }

        public static T Error(Exception e)
        {
            return new T
            {
                Status = StatusCode.InternalServerError,
                ErrorCode = e.HResult,
                ErrorDetails = e
            };
        }
    }
}
