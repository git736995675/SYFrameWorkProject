#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Framework.Editor.CodeLineCounter
{
    public class CodeLineCounter : MonoBehaviour
    {
        [MenuItem("SY-Tool/工具/代码行数统计")]
        private static void CountLinesOfCode()
        {
            const string path = "Assets"; 
            var totalLines = 0;

            foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            {
                using var reader = new StreamReader(file);
                while (!reader.EndOfStream)
                {
                    reader.ReadLine();
                    totalLines++;
                }
            }

            Debug.Log($"Total lines of code in {path}: {totalLines}");
        }
    }
}
#endif