namespace WechatBotWeb.Common
{
    public interface IDevice
    {
        string ClientDeviceId { get; }
    }

    public interface ISession : IDevice
    {
        string ClientSessionId { get; }
    }


}
