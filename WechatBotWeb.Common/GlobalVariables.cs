using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatBotWeb.Common
{
    public static class GlobalVariables
    {
        public const string DeviceIdName = "WB-Device-Id";
        public const string SessionIdName = "WB-Session-Id";
        public const string WebApiPath = "/api";

        public static bool IsProduction = true;
    }

    public static class ApplicationInsightEventNames
    {
        #region event property names
        public const string EventSourcePropertyName = "Source";
        public const string EventDeviceIdPropertyName = "DeviceId";
        public const string EventSessionIdPropertyName = "SessionId";
        public const string EventCorrelationIdPropertyName = "CorrelationId";
        public const string EventStatusPropertyName = "Status";
        public const string EventFailedPropertyName = "Failed";
        public const string EventExceptionPropertyName = "Exception";
        #endregion

        #region event metrics names
        public const string WatchElapsedMetricName = "Elapsed";
        #endregion


        #region event names
        public const string EventClientNewDeviceIdSource = "Client.NewDeviceId";
        public const string EventClientNewSessionIdSource = "Client.NewSessionId";
        public const string EventServerWebApiStatisticsSource = "Server.WebApi.Statistics";
        public const string EventCallAzureStorageTableSource = "Dependency.Azure.StorageTable";
        public const string EventCallAzureKeyVaultSource = "Dependency.Azure.KeyVault";
        public const string EventCallAzureStorageBlobSource = "Dependency.Azure.StorageBlob";
        #endregion
    }
}
