using System;
using Microsoft.Extensions.Logging;

namespace Abp.Logging
{
    /// <summary>
    /// Null logger that does nothing. Used as a default logger.
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static NullLogger Instance { get; } = new NullLogger();

        private NullLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Do nothing
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
