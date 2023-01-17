using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace CovidCertificate.Backend.Utils.Extensions
{
    public enum LogType
    {
        CosmosDb, Redis, BlobStorage, Generic
    }

    public static class LoggerExtensions
    {
        public static void LogInformation<T>(this ILogger<T> logger, LogType logType, string message, [CallerMemberName] string methodName = "")
        {
            var className = typeof(T).Name;
            logger.LogInformation("{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
        }

        public static void LogDebug<T>(this ILogger<T> logger, LogType logType, string message, [CallerMemberName] string methodName = "")
        {
            var className = typeof(T).Name;
            logger.LogDebug("{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
        }

        public static void LogWarning<T>(this ILogger<T> logger, LogType logType, string message, Exception exception = default, [CallerMemberName] string methodName = "")
        {
            var className = typeof(T).Name;
            if (exception == default)
            {
                logger.LogWarning("{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
            else
            {
                logger.LogWarning(exception, "{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
        }


        public static void LogError<T>(this ILogger<T> logger, LogType logType, string message, Exception exception = default, [CallerMemberName] string methodName = "")
        {
            var className = typeof(T).Name;
            if (exception == default)
            {
                logger.LogError("{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
            else
            {
                logger.LogError(exception, "{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
        }

        public static void LogCritical<T>(this ILogger<T> logger, LogType logType, string message, Exception exception = default, [CallerMemberName] string methodName = "")
        {
            var className = typeof(T).Name;
            if (exception == default)
            {
                logger.LogCritical("{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
            else
            {
                logger.LogCritical(exception, "{logType}:{className}:{methodName}:{message}", logType, className, methodName, message);
            }
        }

        public static void LogTraceAndDebug<T>(this ILogger<T> logger, LogType logType, string message)
        {
            var className = typeof(T).Name;
            logger.LogDebug("{logType}:{className}:{message}", logType, className, message);
            logger.LogTrace("{logType}:{className}:{message}", logType, className, message);
        }

        public static void LogTraceAndDebug(this ILogger logger,string message)
        {
            logger.LogTrace(message);
            logger.LogDebug(message);
        }
    }
}
