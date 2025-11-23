using System;
using Microsoft.Extensions.Logging;

namespace Abp.Logging
{
    /// <summary>
    /// Extensions for <see cref="ILogger"/>.
    /// </summary>
    public static class LoggerExtensions
    {
        public static void Log(this ILogger logger, LogSeverity severity, string message)
        {
            switch (severity)
            {
                case LogSeverity.Fatal:
                    logger.LogCritical(message);
                    break;
                case LogSeverity.Error:
                    logger.LogError(message);
                    break;
                case LogSeverity.Warn:
                    logger.LogWarning(message);
                    break;
                case LogSeverity.Info:
                    logger.LogInformation(message);
                    break;
                case LogSeverity.Debug:
                    logger.LogDebug(message);
                    break;
                default:
                    throw new AbpException("Unknown LogSeverity value: " + severity);
            }
        }

        public static void Log(this ILogger logger, LogSeverity severity, string message, Exception exception)
        {
            switch (severity)
            {
                case LogSeverity.Fatal:
                    logger.LogCritical(exception, message);
                    break;
                case LogSeverity.Error:
                    logger.LogError(exception, message);
                    break;
                case LogSeverity.Warn:
                    logger.LogWarning(exception, message);
                    break;
                case LogSeverity.Info:
                    logger.LogInformation(exception, message);
                    break;
                case LogSeverity.Debug:
                    logger.LogDebug(exception, message);
                    break;
                default:
                    throw new AbpException("Unknown LogSeverity value: " + severity);
            }
        }

        public static void Log(this ILogger logger, LogSeverity severity, Func<string> messageFactory)
        {
            // Only invoke the message factory if logging is enabled for this level
            if (!IsEnabled(logger, severity))
            {
                return;
            }

            Log(logger, severity, messageFactory());
        }

        private static bool IsEnabled(ILogger logger, LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Fatal => logger.IsEnabled(LogLevel.Critical),
                LogSeverity.Error => logger.IsEnabled(LogLevel.Error),
                LogSeverity.Warn => logger.IsEnabled(LogLevel.Warning),
                LogSeverity.Info => logger.IsEnabled(LogLevel.Information),
                LogSeverity.Debug => logger.IsEnabled(LogLevel.Debug),
                _ => false
            };
        }
    }
}