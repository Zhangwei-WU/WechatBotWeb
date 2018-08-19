namespace WechatBotWeb.TableData.Entities
{
    using Microsoft.WindowsAzure.Storage.Table;
    using WechatBotWeb.IData;

    public class DeviceInfoEntity : TableEntity, IDeviceInfo
    {
        public string DeviceFP { get; set; }

        public string UserAgent { get; set; }

        public string Browser { get; set; }

        public string BrowserVersion { get; set; }

        public string OS { get; set; }

        public string OSVersion { get; set; }

        public string Device { get; set; }

        public string DeviceType { get; set; }

        public string DeviceVendor { get; set; }

        public string Mobile { get; set; }

        public string Language { get; set; }

        public string SystemLanguage { get; set; }

        public string TimeZone { get; set; }

        public string DPI { get; set; }

        public string LocalStorage { get; set; }

        public string SessionStorage { get; set; }

        public string Cookie { get; set; }

        public string ClientDeviceId { get; set; }
    }

    public class SessionInfoEntity : TableEntity, ISessionInfo
    {
        public string LocationLatitude { get; set; }

        public string LocationLongitude { get; set; }

        public string IPAddress { get; set; }

        public string Resolution { get; set; }

        public string AvailableResolution { get; set; }

        public string ClientSessionId { get; set; }

        public string ClientDeviceId { get; set; }
    }
}
