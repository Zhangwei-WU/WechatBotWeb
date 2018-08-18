using System;
using System.Collections.Generic;
using System.Text;
using WechatBotWeb.Common;
using WechatBotWeb.IData;

namespace WechatBotWeb.TableData
{
    public class AppAuthenticationToken : AbstractError<AppAuthenticationToken>, IAppAuthenticationToken
    {
        public static AppAuthenticationToken UnAuthorized = new AppAuthenticationToken { Status = StatusCode.Unauthorized };
        public static AppAuthenticationToken BadRequest = new AppAuthenticationToken { Status = StatusCode.BadRequest };
        public string AccessToken { get; set; }
    }

    public class UserAuthenticationCode : AbstractError<UserAuthenticationCode>, IUserAuthenticationCode
    {
        public static UserAuthenticationCode UnAuthorized = new UserAuthenticationCode { Status = StatusCode.Unauthorized };
        public static UserAuthenticationCode BadRequest = new UserAuthenticationCode { Status = StatusCode.BadRequest };
        public static UserAuthenticationCode Expired = new UserAuthenticationCode { Status = StatusCode.Expired };
        

        public string Code { get; set; }

        public string TargetUser { get; set; }

        public long ExpireIn { get; set; }
    }

    public class UserAuthenticationToken : AbstractError<UserAuthenticationToken>, IUserAuthenticationToken
    {
        public static UserAuthenticationToken Pending = new UserAuthenticationToken { Status = StatusCode.Pending };
        public static UserAuthenticationToken Expired = new UserAuthenticationToken { Status = StatusCode.Expired };
        public static UserAuthenticationToken UnAuthorized = new UserAuthenticationToken { Status = StatusCode.Unauthorized };
        public static UserAuthenticationToken BadRequest = new UserAuthenticationToken { Status = StatusCode.BadRequest };
        

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public long ExpireIn { get; set; }
    }

}
