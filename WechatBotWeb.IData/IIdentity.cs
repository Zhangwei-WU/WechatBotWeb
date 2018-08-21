using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using WechatBotWeb.Common;

namespace WechatBotWeb.IData
{
    #region Identity

    public enum IdentityType
    {
        Unknown,
        App,
        User,
        Bot
    }

    public enum UserVerificationLevel
    {
        /// <summary>
        /// user is not verified
        /// </summary>
        NotVerified = 0,
        /// <summary>
        /// user is signed in by refresh token and in another session
        /// </summary>
        AutoSignIn = 1,
        /// <summary>
        /// user is signed in by strong sign in
        /// </summary>
        StrongSignIn = 2
    }

    public interface ISessionIdentity : ISession, IIdentity
    {
        IdentityType IdentityType { get; }
        long ExpireIn { get; }
    }

    public interface IAppIdentity : ISessionIdentity
    {
    }

    public interface IUserIdentity : ISessionIdentity
    {
        UserVerificationLevel VerificationLevel {get;}
    }

    public interface IBotIdentity : ISessionIdentity
    {
    }

    public interface IAnonymousIdentity : ISessionIdentity
    {
    }
    #endregion

}
