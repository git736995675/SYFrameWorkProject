using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
namespace ZSYFramework.Core
{
    /// <summary>
    /// 日志等级，只开放当前等级以上的打印
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical,
        Close, //关闭
    }

    /// <summary>
    /// 日志模块
    /// </summary>
    public enum LogCategory
    {
        General,
        UI,
        Network,
        Battle,
        Resource,
        Audio,
        Task,
        Config,
        HotUpdate,
        Download,
    }

    /// <summary>
    /// 日志管理器。统一控制日志级别、分类输出，支持上传日志到服务器。
    /// </summary>
    public class LogManager : Singleton<LogManager>
    {
        // 配置项
        [SerializeField] private LogLevel globalLogLevel = LogLevel.Debug; // 全局日志级别

        public LogLevel GlobalLogLevel
        {
            set => globalLogLevel = value;
        }

        [SerializeField] private bool enableFileLogging = true; // 是否启用文件日志

        public bool EnableFileLogging
        {
            set => enableFileLogging = value;
        }

        [SerializeField] private bool enableStackTrace = false; // 是否记录堆栈信息

        public bool EnableStackTrace
        {
            set => enableStackTrace = value;
        }

        [SerializeField] private bool enableNetworkLogging = false; // 是否上报服务器

        private bool EnableNetworkLogging
        {
            set => enableNetworkLogging = value;
        }

        private const string LOGFileName = "game_log.txt"; // 日志文件名

        private const string LogServerUrl = "http://your-log-server/api/log"; //上报服务器的url

        private const int FileSizeLimit = 10; //日志文件大小限制

        // 分类开关（可通过配置文件或代码动态修改）
        public readonly Dictionary<LogCategory, bool> CategoryFilters = new Dictionary<LogCategory, bool>
        {
            { LogCategory.General, true },
            { LogCategory.UI, true },
            { LogCategory.Network, true },
            { LogCategory.Battle, true },
            { LogCategory.Resource, true },
            { LogCategory.Audio, true },
            { LogCategory.Task, true }
        };

        // 日志队列（异步写入）
        private Queue<string> _logQueue;
        private StreamWriter _logWriter;
        private bool _isWriting = false;

        protected void Awake()
        {
#if !UNITY_EDITOR
        globalLogLevel = LogLevel.Warning; // 正式包只记录Warning以上日志
        enableStackTrace = false;
#endif
        }

        private void OnDestroy()
        {
            if (_logWriter == null) return;
            _logWriter.Close();
            _logWriter.Dispose();
        }

        // 核心日志方法
        public void Log(LogLevel level, LogCategory category, string message, object context = null)
        {
            if ((int)level < (int)globalLogLevel) return; //开放等级
            if (!CategoryFilters[category]) return; //模块是否开放

            var formattedMessage = FormatLog(level, category, message, context);
            WriteToConsole(level, formattedMessage);
            if (enableFileLogging) EnqueueLog(formattedMessage);
            if (enableNetworkLogging) SendToServer(formattedMessage);
        }

        //打开日志文件目录
        public static void OpenLogFile()
        {
            var logFilePath = Path.Combine(Application.persistentDataPath, LOGFileName);
            System.Diagnostics.Process.Start(logFilePath);
        }

        // 格式化日志
        private string FormatLog(LogLevel level, LogCategory category, string message, object context)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var stackTrace = enableStackTrace ? Environment.StackTrace : "";
            var contextInfo = context != null ? context.ToString() : "";
            return $"[{timestamp}] Msg：{message} [{level}] [{category}] | Context: {contextInfo} {stackTrace}";
        }

        // 控制台输出（改色）
        private static void WriteToConsole(LogLevel level, string message)
        {
            var color = level switch
            {
                LogLevel.Debug => "white",
                LogLevel.Info => "cyan",
                LogLevel.Warning => "yellow",
                LogLevel.Error => "red",
                LogLevel.Critical => "magenta",
                _ => "white"
            };
            Debug.Log($"<color={color}>{message}</color>");
        }

        // 异步写入文件
        private async void EnqueueLog(string log)
        {
            if (_logWriter == null)
            {
                var logFilePath = Path.Combine(Application.persistentDataPath, LOGFileName);
                _logWriter = new StreamWriter(logFilePath, append: true);
                _logWriter.AutoFlush = false;
            }

            (_logQueue ??= new Queue<string>()).Enqueue(log);
            if (_isWriting) return;
            _isWriting = true;
            await Task.Run(() =>
            {
                while (_logQueue.Count > 0)
                {
                    var msg = _logQueue.Dequeue();
                    _logWriter.WriteLine(msg);
                }

                _logWriter.Flush();
                _isWriting = false;
            });

            RotateLogFiles();
        }

        //日志文件滚动策略
        private static void RotateLogFiles()
        {
            var logPath = Path.Combine(Application.persistentDataPath, LOGFileName);
            if (!File.Exists(logPath) || new FileInfo(logPath).Length <= FileSizeLimit * 1024 * 1024) return; // 10MB
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var newPath = Path.Combine(Application.persistentDataPath, $"game_log_{timestamp}.txt");
            try
            {
                File.Move(logPath, newPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //如需上报服务器，可在此扩展
        private static void SendToServer(string log)
        {
            var request = new UnityWebRequest(LogServerUrl, "POST");
            var body = System.Text.Encoding.UTF8.GetBytes(log);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest();
        }
    }
}