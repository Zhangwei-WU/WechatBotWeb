using System;
using System.Collections.Generic;
using System.Text;

namespace WechatBotWeb.Common
{
    public static class ApplicationInsightConstants
    {
        #region event property names
        public const string SourcePropertyName = "Source";
        public const string DeviceIdPropertyName = "DeviceId";
        public const string SessionIdPropertyName = "SessionId";
        #endregion
        
        #region event names
        public const string EventClientNewDeviceIdSource = "Client.NewDeviceId";
        public const string EventClientNewSessionIdSource = "Client.NewSessionId";
        #endregion
    }
}
