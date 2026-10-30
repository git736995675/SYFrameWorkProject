using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using ZSYFramework.Localization;
using ZSYFramework.Core.Json;

namespace ZSYFramework.DevTools.Utils;
/// <summary>
/// 检测项目中是否存在 TextMeshPro 程序集，自动维护 SY_TMP 编译宏。
/// 有 TMP 包 → 加 SY_TMP，LocalizationText 等走 TMP 分支；
/// 无 TMP 包 → 删 SY_TMP，自动回退 Unity 内置 UI.Text，保证编译通过。
/// </summary>
[InitializeOnLoad]
/// <summary>
/// TMP 宏检查器。检测 TextMeshPro 包并自动管理 SY_TMP 宏定义。
/// </summary>
public static class TmpDefineSymbolChecker
{
private const string Symbol = "SY_TMP";
private static bool _checking;

static TmpDefineSymbolChecker()
{
    // 延迟到编辑器空闲时执行，避免导入早期修改 PlayerSettings 触发重导入循环
    EditorApplication.delayCall += Check;
    CompilationPipeline.compilationFinished += _ =>
    {
        if (!_checking) EditorApplication.delayCall += Check;
    };
}

public static void Check()
{
    if (_checking) return;
    _checking = true;
    try
    {
        var hasTmp = CompilationPipeline.GetAssemblies()
            .Any(a => a.name == "Unity.TextMeshPro");

        var targets = new[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS
        };

        var changed = false;
        foreach (var group in targets)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var list = symbols.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
            var contains = list.Contains(Symbol);

            if (hasTmp && !contains)
            {
                list.Add(Symbol);
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", list));
                changed = true;
            }
            else if (!hasTmp && contains)
            {
                list.Remove(Symbol);
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", list));
                changed = true;
            }
        }

        if (changed)
        {
            UnityEngine.Debug.Log($"[TmpDefineSymbolChecker] 检测到 TMP 程序集: {hasTmp}，SY_TMP 宏已同步。");
        }
    }
    finally
    {
        _checking = false;
    }
}
}
