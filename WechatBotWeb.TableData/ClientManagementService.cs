namespace WechatBotWeb.TableData
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Threading.Tasks;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;
    using WechatBotWeb.TableData.Entities;

    public class ClientManagementService : IClientManagementService
    {
        public const string DeviceInfoTableName = "Devices";
        public const string SessionInfoTableName = "Sessions";

        private IApplicationInsights insight;
        private CloudTableClient tableClient;
        public ClientManagementService(IApplicationInsights insight, CloudTableClient tableClient)
        {
            this.insight = insight;
            this.tableClient = tableClient;
        }

        public Task<bool> IsDeviceExist(IDevice device)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsSessionExist(ISession session)
        {
            throw new NotImplementedException();
        }

        public async Task SaveDeviceInfo(IDeviceInfo device)
        {
            var entity = new DeviceInfoEntity
            {
                PartitionKey = device.ClientDeviceId,
                RowKey = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),

                Browser = device.Browser,
                BrowserVersion = device.BrowserVersion,
                ClientDeviceId = device.ClientDeviceId,
                Cookie = device.Cookie,
                Device = device.Device,
                DeviceFP = device.DeviceFP,
                DeviceType = device.DeviceType,
                DeviceVendor = device.DeviceVendor,
                DPI = device.DPI,
                Language = device.Language,
                LocalStorage = device.LocalStorage,
                Mobile = device.Mobile,
                OS = device.OS,
                OSVersion = device.OSVersion,
                SessionStorage = device.SessionStorage,
                SystemLanguage = device.SystemLanguage,
                TimeZone = device.TimeZone,
                UserAgent = device.UserAgent
            };

            var table = tableClient.GetTableReference(DeviceInfoTableName);

            var status = await insight.WatchAsync(
                async () => await table.InsertAsync(entity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

            if (status >= 400) insight.Warn("ClientManagementService", "Error inserting device info");
        }

        public async Task UpdateSessionInfo(ISessionInfo session)
        {
            var entity = new SessionInfoEntity
            {
                PartitionKey = session.ClientDeviceId,
                RowKey = session.ClientSessionId + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),

                AvailableResolution = session.AvailableResolution,
                ClientDeviceId = session.ClientDeviceId,
                ClientSessionId = session.ClientSessionId,
                IPAddress = session.IPAddress,
                LocationLatitude = session.LocationLatitude,
                LocationLongitude = session.LocationLongitude,
                Resolution = session.Resolution,
            };

            var table = tableClient.GetTableReference(SessionInfoTableName);

            var status = await insight.WatchAsync(
                async () => await table.InsertAsync(entity),
                (r, e) => e.Status = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

            if (status >= 400) insight.Warn("ClientManagementService", "Error inserting session info");
        }
    }
}
