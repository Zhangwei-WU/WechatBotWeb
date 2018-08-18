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
    }

    public interface IBotIdentity : ISessionIdentity
    {
    }

    public interface IAnonymousIdentity : ISessionIdentity
    {
    }
    #endregion

}
