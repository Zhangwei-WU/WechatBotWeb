
namespace WechatBotWeb.Models
{
    using WechatBotWeb.IData;

    public class CreateUserAuthenticationCodeRequest : ICreateUserAuthenticationCodeRequest
    {
        public UserAuthenticationCodeType CodeType { get; set; }
        public string Code { get; set; }
        public string TargetUser { get; set; }
    }

    public class AcknowledgeUserAuthenticationCodeRequest : IAcknowledgeUserAuthenticationCodeRequest
    {
        public UserAuthenticationCodeType CodeType { get; set; }

        public string Code { get; set; }

        public string AcknowledgeUser { get; set; }
    }

    public class GetUserAuthenticationCodeRequest : IGetUserAuthenticationCodeRequest
    {
        public UserAuthenticationCodeType CodeType { get; set; }

        public string Code { get; set; }
    }

    public class RefreshUserAuthenticationTokenRequest : IRefreshUserAuthenticationTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
