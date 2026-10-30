using UnityEditor;
using ZSYFramework.Core;


namespace ZSYFramework.DevTools
{
/// <summary>
/// 日志编辑器窗口。可视化查看与过滤运行日志。
/// </summary>
public class LogEditor : EditorWindow
{
[MenuItem("SY-Tool/开发工具/打开日志文件", false, 2)]
public static void OpenFile()
{
    LogManager.OpenLogFile();
}
} 
}
