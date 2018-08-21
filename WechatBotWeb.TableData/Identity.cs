
namespace WechatBotWeb.TableData
{
    using WechatBotWeb.IData;

    public class UserIdentity : IUserIdentity
    {
        public IdentityType IdentityType { get; set; }

        public long ExpireIn { get; set; }

        public string ClientSessionId { get; set; }

        public string ClientDeviceId { get; set; }

        public string AuthenticationType { get; set; }

        public bool IsAuthenticated { get; set; }

        public string Name { get; set; }

        public UserVerificationLevel VerificationLevel { get; set; }
    }

    public class AnonymousIdentity : IAnonymousIdentity
    {
        public static AnonymousIdentity Anonymous(string deviceId, string sessionId)
        {
            return new AnonymousIdentity
            {
                IdentityType = IdentityType.Unknown,
                IsAuthenticated = false,
                ClientDeviceId = deviceId,
                ClientSessionId = sessionId
            };
        }

        public IdentityType IdentityType { get; set; }

        public long ExpireIn { get; set; }

        public string ClientSessionId { get; set; }

        public string ClientDeviceId { get; set; }

        public string AuthenticationType { get; set; }

        public bool IsAuthenticated { get; set; }

        public string Name { get; set; }
    }

    public class AppIdentity : IAppIdentity
    {
        public IdentityType IdentityType { get; set; }

        public long ExpireIn { get; set; }

        public string ClientSessionId { get; set; }

        public string ClientDeviceId { get; set; }

        public string AuthenticationType { get; set; }

        public bool IsAuthenticated { get; set; }

        public string Name { get; set; }
    }
}
