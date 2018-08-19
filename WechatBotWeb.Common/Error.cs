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
}
