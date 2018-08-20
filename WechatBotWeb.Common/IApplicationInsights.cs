namespace WechatBotWeb.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public enum LogLevel
    {
        ALL = 0,
        Verb = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
        DISABLED = 999
    }
    
    public interface IApplicationInsightEvent : IDisposable
    {
        string EventSource { get; }
        string EventStatus { get; set; }
        string Exception { get; set; }
        IDictionary<string, string> Properties { get; }
        IDictionary<string, double> Metrics { get; }
    }

    public interface IApplicationInsights
    {
        #region perf counters
        /// <summary>
        /// watch task code snippet with a return value
        /// </summary>
        /// <typeparam name="T">return value type</typeparam>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>return value of func</returns>
        Task<T> WatchAsync<T>(Func<Task<T>> func, Action<T, IApplicationInsightEvent> checkResult, string source, params string[] properties);
        /// <summary>
        /// watch task code snipppet without a return value
        /// </summary>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>a task value of func</returns>
        Task WatchAsync(Func<Task> func, string source, params string[] properties);
        /// <summary>
        /// watch code snippet with a return value
        /// </summary>
        /// <typeparam name="T">return value type</typeparam>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>return value of code snippet</returns>
        T Watch<T>(Func<T> func, Action<T, IApplicationInsightEvent> checkResult, string source, params string[] properties);
        /// <summary>
        /// watch code snippet without a return value
        /// </summary>
        /// <param name="action">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        void Watch(Action action, string source, params string[] properties);

        /// <summary>
        /// watch task code snippet with a return value
        /// </summary>
        /// <typeparam name="T">return value type</typeparam>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>return value of func</returns>
        Task<T> WatchAsync<T>(Func<IApplicationInsightEvent, Task<T>> func, string source, params string[] properties);
        /// <summary>
        /// watch task code snipppet without a return value
        /// </summary>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>a task value of func</returns>
        Task WatchAsync(Func<IApplicationInsightEvent, Task> func, string source, params string[] properties);
        /// <summary>
        /// watch code snippet with a return value
        /// </summary>
        /// <typeparam name="T">return value type</typeparam>
        /// <param name="func">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>return value of code snippet</returns>
        T Watch<T>(Func<IApplicationInsightEvent, T> func, string source, params string[] properties);
        /// <summary>
        /// watch code snippet without a return value
        /// </summary>
        /// <param name="action">code snippet</param>
        /// <param name="properties">additional data for the watch</param>
        void Watch(Action<IApplicationInsightEvent> action, string source, params string[] properties);

        /// <summary>
        /// watch external code snippet
        /// </summary>
        /// <param name="properties">additional data for the watch</param>
        /// <returns>a watch handler</returns>
        /// <example>
        /// using(var watch = insights.Watch(...))
        /// {
        ///     // do time consumed something
        /// }
        /// </example>
        IApplicationInsightEvent Watch(string source, params string[] properties);
        #endregion

        #region log
        void Verb(string source, string message, params object[] args);
        void Info(string source, string message, params object[] args);
        void Warn(string source, string message, params object[] args);
        void Error(string source, string message, params object[] args);
        void Fatal(string source, string message, params object[] args);
        #endregion

        #region event
        void Event(IApplicationInsightEvent eventData);
        void Event(string source, params string[] properties);
        #endregion

        void Exception(Exception exception, string source, params string[] properties);

        void Flush();
    }

    public class ApplicationInsightsAlreadyInspectedException : Exception
    {
        public ApplicationInsightsAlreadyInspectedException(Exception innerException)
            :base(string.Empty, innerException)
        {

        }
    }
}
