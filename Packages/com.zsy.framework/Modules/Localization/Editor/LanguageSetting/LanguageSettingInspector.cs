using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Localization;

#if SY_TMP
#endif

namespace ZSYFramework.Localization.Editor
{
[CustomEditor(typeof(LanguageSetting))]
/// <summary>
/// 语言设置资产的 Inspector 定制。可视化编辑语言与字体映射。
/// </summary>
public class LanguageSettingInspector : UnityEditor.Editor
{

public override void OnInspectorGUI()
{
    base.OnInspectorGUI();
    
    if (GUILayout.Button("重置"))
    {
        ResetLanguage();
    }

    if (GUI.changed)
    {
        RefreshText();
    }
}

/// <summary>
/// 通过配置文件，重置多语言设置
/// </summary>
private void ResetLanguage()
{
    // 获取项目中所有字体文件
    var allFonts = AssetDatabase.FindAssets("t:Font").Select(AssetDatabase.GUIDToAssetPath);
#if SY_TMP
    var allTmps = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath);
#endif
   
    LanguageSettingConfig.GetAll((configs) =>
    {
        if (configs == null)
        {
            return;
        }

        var list = new List<CountryAndCode>();
        foreach (var setting in configs)
        {
            if (!setting.IsUse)
            {
                continue;
            }

            var code = new CountryAndCode
            {
                Country = setting.Country,
                ShortName = setting.ShortName,
                Annotate = setting.Annotate,
                Sort = setting.Sort,
                StrKey = setting.StrKey,
                FontAssetName = setting.FontAssetName
            };

            // 加载字体资源文件
            if (setting.FontAssetName.Equals("Arial"))
            {
                code.Font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            else
            {
                foreach (var fontPath in allFonts)
                {
                    var fontName = Path.GetFileNameWithoutExtension(fontPath);
                    if (fontName != setting.FontAssetName) continue;
                    code.Font = (Font)AssetDatabase.LoadAssetAtPath(fontPath,typeof(Font));
                    break;
                }
            }
            
#if SY_TMP
            foreach (var fontPath in allTmps)
            {
                var fontName = Path.GetFileNameWithoutExtension(fontPath);
                if (fontName != setting.TMPFontAssetName) continue;
                code.TMPFontAsset = (TMP_FontAsset)AssetDatabase.LoadAssetAtPath(fontPath,typeof(TMP_FontAsset));
                break;
            }
#endif

            list.Add(code);
        }

        list.Sort((a, b) => a.Sort - b.Sort);
        var languageSetting = (LanguageSetting)target;
        languageSetting.countryS = list.ToArray();
        EditorUtility.SetDirty(target);
        EditorUtility.ClearDirty(target);
        serializedObject.ApplyModifiedProperties();
        Repaint();
    });
}


private void RefreshText()
{
    var languageSetting = (LanguageSetting)target;
    var localizationTexts = FindObjectsOfType<LocalizationText>(true).ToList();

    foreach (var country in languageSetting.countryS)
    {
        if (country.Annotate == languageSetting.currentCountry.ToString())
        {
            localizationTexts.ForEach(item =>
            {
                item.EditorOnLocalize(country);
                item.enabled = true;
            });
        }
    }

    serializedObject.ApplyModifiedProperties();
    Repaint();
}
}
}
