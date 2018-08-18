namespace WechatBotWeb.Common
{
    public class ClientContext : ISession
    {
        public string ClientDeviceId { get; set; }
        public string ClientSessionId { get; set; }
        public string CorrelationId { get; set; }
        public string IP { get; set; }
    }
}
