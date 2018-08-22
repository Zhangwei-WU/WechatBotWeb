namespace WechatBotWeb.Insight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using WechatBotWeb.Common;

    //public class AzureApplicationInsightEvent : IApplicationInsightEvent
    //{
    //    private AzureApplicationInsights insight;
    //    private Stopwatch watch;
    //    private bool disposed = false;

    //    internal AzureApplicationInsightEvent(string source, AzureApplicationInsights insight, bool startWatch)
    //    {
    //        this.EventSource = source;
    //        this.insight = insight;
    //        Init(startWatch);
    //    }

    //    public AzureApplicationInsightEvent(string source)
    //    {
    //        this.EventSource = source;
    //        Init(false);
    //    }

    //    public string EventSource { get; set; }
    //    public string EventStatus { get; set; }
    //    public string Exception { get; set; }
    //    public IDictionary<string, string> Properties { get; private set; }
    //    public IDictionary<string, double> Metrics { get; private set; }

    //    private void Init(bool startWatch)
    //    {
    //        this.Properties = new Dictionary<string, string>();
    //        this.Metrics = new Dictionary<string, double>();

    //        if(startWatch)
    //        {
    //            watch = new Stopwatch();
    //            watch.Start();
    //        }
    //    }

    //    private void EventMe()
    //    {
    //        if (watch != null && watch.IsRunning)
    //        {
    //            watch.Stop();
    //            Metrics.Add(ApplicationInsightEventNames.WatchElapsedMetricName, watch.ElapsedMilliseconds);
    //        }

    //        if (insight != null) insight.Event(this);
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (disposed) return;
    //        if (disposing)
    //        {
    //            EventMe();
    //        }

    //        disposed = true;
    //    }
    //}

    public class AzureApplicationInsights : IApplicationInsights
    {
        private TelemetryClient telemetry = new TelemetryClient();

        public AzureApplicationInsights()
        {
        }

        public void AddContext(string name, string value)
        {
            telemetry.Context.Properties.Add(name, value);
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
            telemetry.TrackException(exception, GetProperties(source, properties), null);
        }

        #endregion

        #region event

        public void Event(string source, params string[] properties)
        {
            telemetry.TrackEvent(source, GetProperties(source, properties));
        }

        #endregion

        //#region watch

        //public T Watch<T>(Func<T> func, Action<T, IApplicationInsightEvent> checkResult, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            var result = func();
        //            checkResult?.Invoke(result, e);
        //            return result;
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(ex, source, properties);
        //        }
        //    }
        //}
        //public T Watch<T>(Func<IApplicationInsightEvent, T> func, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            return func(e);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(e, ex, source, properties);
        //        }
        //    }
        //}
        //public void Watch(Action action, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            action();
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(e, ex, source, properties);
        //        }
        //    }
        //}
        //public void Watch(Action<IApplicationInsightEvent> action, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            action(e);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(e, ex, source, properties);
        //        }
        //    }
        //}

        ////public IApplicationInsightEvent Watch(string source, params string[] properties)
        ////{
        ////    var e = new AzureApplicationInsightEvent(source, this, true);
        ////    if (properties != null && properties.Length != 0) for (var i = 0; i < properties.Length / 2; i++) e.Properties.Add(properties[i * 2], properties[i * 2 + 1]);
        ////    return e;
        ////}

        //public async Task<T> WatchAsync<T>(Func<Task<T>> func, Action<T> result, string dependencyTypeName, params string[] properties)
        //{
        //    using (var op = telemetry.StartOperation<DependencyTelemetry>(""))
        //    {
        //        op.Telemetry.DependencyTypeName = dependencyTypeName;
        //        op.Telemetry.
        //        try
        //        {
        //            var r = await func();
        //            op.Telemetry.Success = true;
        //            op.Telemetry.
        //            return r;
        //        }
        //        catch (Exception ex)
        //        {
        //            op.Telemetry.Success = false;
        //            throw HandleExceptions(ex, dependencyTypeName, properties);
        //        }
        //        finally
        //        {
        //            telemetry.StopOperation(op);
        //        }
        //    }
        //}

        //public async Task<T> WatchAsync<T>(Func<IApplicationInsightEvent, Task<T>> func, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            return await func(e);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(e, ex, source, properties);
        //        }
        //    }
        //}

        //public async Task WatchAsync(Func<Task> func, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            await func();
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(e, ex, source, properties);
        //        }
        //    }
        //}

        //public async Task WatchAsync(Func<IApplicationInsightEvent, Task> func, string source, params string[] properties)
        //{
        //    using (var e = Watch(source, properties))
        //    {
        //        try
        //        {
        //            await func(e);
        //        }
        //        catch (Exception ex)
        //        {
        //            throw HandleExceptions(ex, source, properties);
        //        }
        //    }
        //}

        //private Exception HandleExceptions(Exception ex, string source, string[] properties)
        //{
        //    if (ex is ApplicationInsightsAlreadyInspectedException) return ex;

        //    Exception(ex, source, properties);
        //    return new ApplicationInsightsAlreadyInspectedException(ex);
        //}

        //#endregion

        public void Flush()
        {
            telemetry.Flush();
        }

        private Dictionary<string, string> GetProperties(string source, string[] properties)
        {
            if ((properties?.Length & 0x1) == 0x1) throw new ArgumentOutOfRangeException("properties");
            var cnt = properties == null ? 0 : properties.Length / 2;

            var props = new Dictionary<string, string>(cnt + 1);
            if (cnt != 0) for (var i = 0; i < cnt; i++) props.Add(properties[i * 2], properties[i * 2 + 1]);
            props.Add(ApplicationInsightConstants.SourcePropertyName, source);

            return props;
        }
        
        private SeverityLevel GetSeverity(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verb:
                    return SeverityLevel.Verbose;
                case LogLevel.Info:
                    return SeverityLevel.Information;
                case LogLevel.Warn:
                    return SeverityLevel.Warning;
                case LogLevel.Error:
                    return SeverityLevel.Error;
                case LogLevel.Fatal:
                    return SeverityLevel.Critical;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }
    }
}
