#if UNITY_EDITOR
using UnityEditor;

namespace SY
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