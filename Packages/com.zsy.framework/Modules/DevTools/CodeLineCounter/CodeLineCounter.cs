using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR


namespace ZSYFramework.DevTools
{
/// <summary>
/// 代码行数统计工具。统计工程代码规模。
/// </summary>
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
