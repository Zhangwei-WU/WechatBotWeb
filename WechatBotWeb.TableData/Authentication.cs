namespace WechatBotWeb.TableData
{
    using WechatBotWeb.IData;

    public class AppAuthenticationToken : IAppAuthenticationToken
    {
        public string AccessToken { get; set; }
    }

    public class UserAuthenticationCode : IUserAuthenticationCode
    {
        public string Code { get; set; }

        public string TargetUser { get; set; }

        public long ExpireIn { get; set; }
    }

    public class UserAuthenticationToken : IUserAuthenticationToken
    {
        public bool Validated { get; set; }
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public long ExpireIn { get; set; }
    }

}
