namespace WechatBotWeb.TableData
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using WechatBotWeb.Common;
    using WechatBotWeb.IData;
    using WechatBotWeb.TableData.Entities;

    public class ClientManagementService : IClientManagementService
    {
        public const string DeviceInfoTableName = "Devices";
        public const string SessionInfoTableName = "Sessions";

        private static SemaphoreSlim semaphoreForInitialize = new SemaphoreSlim(1, 1);

        private IApplicationInsights insight;
        private CloudTableClient tableClient;
        private KeyVaultClient keyVaultClient;
        private string tableIdentifier;
        private bool initialized = false;

        public ClientManagementService(IApplicationInsights insight, KeyVaultClient keyVault, string tableIdentifier)
        {
            this.insight = insight;
            this.keyVaultClient = keyVault;
            this.tableIdentifier = tableIdentifier;
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
            if (!initialized) await InitializeAsync();

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
                (r, e) => e.EventStatus = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

            if (status >= 400) throw new HttpStatusException("EntityErrorInsert:DeviceInfo", status);
        }

        public async Task UpdateSessionInfo(ISessionInfo session)
        {
            if (!initialized) await InitializeAsync();

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
                (r, e) => e.EventStatus = r.ToString(),
                ApplicationInsightEventNames.EventCallAzureStorageTableSource, "Method", "InsertAsync", "TableName", table.Name);

            if (status >= 400) throw new HttpStatusException("EntityErrorInsert:SessionInfo", status);
        }

        private async Task InitializeAsync()
        {
            if (initialized) return;

            await semaphoreForInitialize.WaitAsync();

            if (initialized) return;

            try
            {
                tableClient = CloudStorageAccount.Parse((await keyVaultClient.GetSecretAsync(tableIdentifier)).Value).CreateCloudTableClient();

                await tableClient.GetTableReference(DeviceInfoTableName).CreateIfNotExistsAsync();
                await tableClient.GetTableReference(SessionInfoTableName).CreateIfNotExistsAsync();

                initialized = true;
            }
            finally
            {
                semaphoreForInitialize.Release();
            }
        }
    }
}
