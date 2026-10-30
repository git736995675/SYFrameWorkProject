#if UNITY_EDITOR
using SY;
using UnityEditor;

namespace ZSYFramework.Editor.Log
{
    public class LogEditor : EditorWindow
    {
        [MenuItem("SY_Tools/打开日志文件")]
        public static void OpenFile()
        {
            LogManager.OpenLogFile();
        }
    } 
}
#endif