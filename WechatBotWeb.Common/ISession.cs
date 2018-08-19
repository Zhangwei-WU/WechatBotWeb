namespace WechatBotWeb.Common
{
    public interface IDevice
    {
        string ClientDeviceId { get; set; }
    }

    public interface ISession : IDevice
    {
        string ClientSessionId { get; set; }
    }


}
