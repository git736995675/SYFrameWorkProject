
namespace ZSYFramework.Core;
/// <summary>
/// 简化Log调用
/// </summary>
public static class SYLog
{
public static void Debug(string message, LogCategory category = LogCategory.General, object context = null)
    => LogManager.Instance.Log(LogLevel.Debug, category, message, context);

public static void Info(string message, LogCategory category = LogCategory.General, object context = null)
    => LogManager.Instance.Log(LogLevel.Info, category, message, context);

public static void Warning(string message, LogCategory category = LogCategory.General, object context = null)
    => LogManager.Instance.Log(LogLevel.Warning, category, message, context);

public static void Error(string message, LogCategory category = LogCategory.General, object context = null)
    => LogManager.Instance.Log(LogLevel.Error, category, message, context);

public static void Critical(string message, LogCategory category = LogCategory.General, object context = null)
    => LogManager.Instance.Log(LogLevel.Critical, category, message, context);

public static void LogSettings(LogLevel level = LogLevel.Error)
{
    LogManager.Instance.GlobalLogLevel = level;
}

public static void LogSettings(LogLevel level = LogLevel.Error, bool isEnableFileLog = false,
    bool isEnableStackTrace = false)
{
    LogManager.Instance.GlobalLogLevel = level;
    LogManager.Instance.EnableFileLogging = isEnableFileLog;
    LogManager.Instance.EnableStackTrace = isEnableStackTrace;
}
}
