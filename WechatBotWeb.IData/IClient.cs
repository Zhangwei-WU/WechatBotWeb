
namespace WechatBotWeb.IData
{
    using WechatBotWeb.Common;

    public interface IDeviceInfo : IDevice
    {
        string UserAgent { get; }
        string Browser { get; }
        string BrowserVersion { get; }
        string OS { get; }
        string OSVersion { get; }
        string Device { get; }
        string DeviceType { get; }
        string DeviceVendor { get; }
        string Mobile { get; }
        string Language { get; }
        string SystemLanguage { get; }
        string TimeZone { get; }
        string DPI { get; }
        string LocalStorage { get; }
        string SessionStorage { get; }
        string Cookie { get; }
    }

    public interface ISessionInfo : ISession
    {
        string LocationLatitude { get; }
        string LocationLongitude { get; }
        string IPAddress { get; }
        string Resolution { get; }
        string AvailableResolution { get; }
    }

}
