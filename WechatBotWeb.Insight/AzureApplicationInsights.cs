namespace WechatBotWeb.Insight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using WechatBotWeb.Common;

    public class AzureApplicationInsightEvent : IApplicationInsightEvent
    {
        private AzureApplicationInsights insight;
        private Stopwatch watch;
        private bool disposed = false;

        internal AzureApplicationInsightEvent(string source, AzureApplicationInsights insight, bool startWatch)
        {
            this.Source = source;
            this.insight = insight;
            Init(startWatch);
        }

        public AzureApplicationInsightEvent(string source)
        {
            this.Source = source;
            Init(false);
        }

        public string Source { get; set; }
        public string Status { get; set; }
        public string Exception { get; set; }
        public IDictionary<string, string> Properties { get; private set; }
        public IDictionary<string, double> Metrics { get; private set; }

        private void Init(bool startWatch)
        {
            this.Properties = new Dictionary<string, string>();
            this.Metrics = new Dictionary<string, double>();

            if(startWatch)
            {
                watch = new Stopwatch();
                watch.Start();
            }
        }

        private void EventMe()
        {
            if (watch != null && watch.IsRunning)
            {
                watch.Stop();
                Metrics.Add(ApplicationInsightEventNames.WatchElapsedMetricName, watch.ElapsedMilliseconds);
            }

            if (insight != null) insight.Event(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                EventMe();
            }

            disposed = true;
        }
    }

    public class AzureApplicationInsights : IApplicationInsights
    {
        private TelemetryClient telemetry = new TelemetryClient();

        public AzureApplicationInsights()
        {
        }

        public LogLevel MinLogLevel { get; set; }

        #region log
        public void Error(string source, string message, params object[] args)
        {
            if (MinLogLevel <= LogLevel.Error) Log(LogLevel.Error, source, message, args);
        }

        public void Fatal(string source, string message, params object[] args)
        {
            if (MinLogLevel <= LogLevel.Fatal) Log(LogLevel.Fatal, source, message, args);
        }

        public void Info(string source, string message, params object[] args)
        {
            if (MinLogLevel <= LogLevel.Info) Log(LogLevel.Info, source, message, args);
        }
        
        public void Verb(string source, string message, params object[] args)
        {
            if (MinLogLevel <= LogLevel.Verb) Log(LogLevel.Verb, source, message, args);
        }

        public void Warn(string source, string message, params object[] args)
        {
            if (MinLogLevel <= LogLevel.Warn) Log(LogLevel.Warn, source, message, args);
        }

        private void Log(LogLevel level, string source, string message, object[] args)
        {
            if (args != null && args.Length != 0) message = string.Format(message, args);
            telemetry.TrackTrace(message, GetSeverity(level), GetProperties(source, null));
        }

        #endregion

        #region exception
        public void Exception(Exception exception, string source, params string[] properties)
        {
            telemetry.TrackException(exception, GetProperties(source, properties));
        }
        #endregion

        #region event
        public void Event(IApplicationInsightEvent eventData)
        {
            FillSystemProperties(eventData.Properties, eventData.Source);

            if (!string.IsNullOrEmpty(eventData.Status)) eventData.Properties.Add(ApplicationInsightEventNames.EventStatusPropertyName, eventData.Status);
            if (!string.IsNullOrEmpty(eventData.Exception)) eventData.Properties.Add(ApplicationInsightEventNames.EventExceptionPropertyName, eventData.Exception);

            telemetry.TrackEvent(eventData.Source, eventData.Properties, eventData.Metrics);
        }

        public void Event(string source, params string[] properties)
        {
            telemetry.TrackEvent(source, GetProperties(source, properties));
        }

        #endregion

        #region watch

        public T Watch<T>(Func<T> func, Action<T, IApplicationInsightEvent> checkResult, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    var result = func();
                    checkResult?.Invoke(result, e);
                    return result;
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }
        public T Watch<T>(Func<IApplicationInsightEvent, T> func, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    return func(e);
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }
        public void Watch(Action action, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }
        public void Watch(Action<IApplicationInsightEvent> action, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    action(e);
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }

        public IApplicationInsightEvent Watch(string source, params string[] properties)
        {
            var e = new AzureApplicationInsightEvent(source, this, true);
            if (properties != null && properties.Length != 0) for (var i = 0; i < properties.Length / 2; i++) e.Properties.Add(properties[i * 2], properties[i * 2 + 1]);
            return e;
        }

        public async Task<T> WatchAsync<T>(Func<Task<T>> func, Action<T, IApplicationInsightEvent> checkResult, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    var result = await func();
                    checkResult?.Invoke(result, e);
                    return result;
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }

        public async Task<T> WatchAsync<T>(Func<IApplicationInsightEvent, Task<T>> func, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    return await func(e);
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }

        public async Task WatchAsync(Func<Task> func, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    await func();
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }

        public async Task WatchAsync(Func<IApplicationInsightEvent, Task> func, string source, params string[] properties)
        {
            using (var e = Watch(source, properties))
            {
                try
                {
                    await func(e);
                }
                catch (Exception ex)
                {
                    e.Exception = ex.GetType().FullName;
                    Exception(ex, source, properties);
                    throw;
                }
            }
        }

        #endregion

        public void Flush()
        {
            telemetry.Flush();
        }

        private Dictionary<string, string> GetProperties(string source, string[] properties)
        {
            if ((properties.Length & 0x1) == 0x1) throw new ArgumentOutOfRangeException("properties");
            var cnt = properties == null ? 0 : properties.Length / 2;
            var props = new Dictionary<string, string>(cnt + 4);
            if (cnt != 0) for (var i = 0; i < cnt; i++) props.Add(properties[i * 2], properties[i * 2 + 1]);

            FillSystemProperties(props, source);

            return props;
        }

        private void FillSystemProperties(IDictionary<string, string> props, string source)
        {
            props.Add(ApplicationInsightEventNames.EventDeviceIdPropertyName, CallContext.ClientContext.ClientDeviceId);
            props.Add(ApplicationInsightEventNames.EventSessionIdPropertyName, CallContext.ClientContext.ClientSessionId);
            props.Add(ApplicationInsightEventNames.EventCorrelationIdPropertyName, CallContext.ClientContext.CorrelationId);
            props.Add(ApplicationInsightEventNames.EventSourcePropertyName, source);
        }

        private Microsoft.ApplicationInsights.DataContracts.SeverityLevel GetSeverity(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verb:
                    return Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Verbose;
                case LogLevel.Info:
                    return Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information;
                case LogLevel.Warn:
                    return Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning;
                case LogLevel.Error:
                    return Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error;
                case LogLevel.Fatal:
                    return Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
