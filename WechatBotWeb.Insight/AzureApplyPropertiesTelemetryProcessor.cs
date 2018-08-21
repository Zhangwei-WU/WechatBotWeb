namespace WechatBotWeb.Insight
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using WechatBotWeb.Common;
    
    public class AzureApplyPropertiesTelemetryProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor next;
        public AzureApplyPropertiesTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;
        }
        public void Process(ITelemetry item)
        {
            if (CallContext.ClientContext!= null)
            {
                item.Context.Properties.Add(ApplicationInsightConstants.DeviceIdPropertyName, CallContext.ClientContext.ClientDeviceId);
                item.Context.Properties.Add(ApplicationInsightConstants.SessionIdPropertyName, CallContext.ClientContext.ClientSessionId);
            }

            next.Process(item);
        }
    }
}
