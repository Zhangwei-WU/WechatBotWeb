namespace WechatBotWeb.TableData.Entities
{
    using Microsoft.WindowsAzure.Storage.Table;

    public class AcknowledgeCodeEntity : TableEntity
    {
        // partition key: code
        // row key: time stamp

        public string ReqDeviceId { get; set; }
        public string ReqSessionId { get; set; }
        public string TargetUser { get; set; }
        public int Status { get; set; }
        public long ExpireIn { get; set; }
        public string AckBy { get; set; }
        public long Ackime { get; set; }
        public string AckVia { get; set; }
    }

    public class AcknowledgeCodeStatusEntity : TableEntity
    {
        // partition key: code
        // row key: 0000000
        public long AvailableAfter { get; set; }
        public string CurrentRowKey { get; set; }
    }

    public class DirectLoginCodeEntity : TableEntity
    {
        public string ReqVia { get; set; }
        public string TargetUser { get; set; }
        public int Status { get; set; }
        public long ExpireIn { get; set; }
        public long AckTime { get; set; }
        public string AckDeviceId { get; set; }
        public string AckSessionId { get; set; }
    }

    public class RefreshTokenEntity : TableEntity
    {
        // partition key: 
        // row key: refreshtoken

        public long CreateTime { get; set; }
        public long ExpireTime { get; set; }
        public long ClaimTime { get; set; }
        public string DeviceId { get; set; }
        public string TargetUser { get; set; }
    }
}
