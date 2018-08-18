using System;
using System.Collections.Generic;
using System.Text;
using WechatBotWeb.Common;

namespace WechatBotWeb.IData
{
    #region Authentication
    public enum UserAuthenticationCodeType
    {
        /// <summary>
        /// user must acknowledge the code then login
        /// target user may be empty
        /// </summary>
        AcknowledgeCode,
        /// <summary>
        /// user use this code to login directly
        /// must assign target user
        /// </summary>
        DirectLoginCode
    }

    public interface IUserAuthenticationCodeRequest
    {
        UserAuthenticationCodeType CodeType { get; }
        string Code { get; }
    }

    public interface ICreateUserAuthenticationCodeRequest : IUserAuthenticationCodeRequest
    {
        string TargetUser { get; }
    }

    public interface IAcknowledgeUserAuthenticationCodeRequest : IUserAuthenticationCodeRequest
    {
        string AcknowledgeUser { get; }
    }

    public interface IGetUserAuthenticationCodeRequest : IUserAuthenticationCodeRequest
    {
    }

    public interface IRefreshUserAuthenticationTokenRequest
    {
        string RefreshToken { get; }
    }
    
    public interface IAppAuthenticationToken : IStatus
    {
        string AccessToken { get; }
    }

    public interface IUserAuthenticationToken : IStatus
    {
        string AccessToken { get; }
        string RefreshToken { get; }
        long ExpireIn { get; }
    }
    
    public interface IUserAuthenticationCode : IStatus
    {
        string Code { get; }
        string TargetUser { get; }
        long ExpireIn { get; }
    }

    #endregion

}
