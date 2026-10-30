using ZSYFramework.Localization.Arabic;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using ZSYFramework.Core.Json;

#if SY_TMP
#endif

namespace ZSYFramework.Localization
{
/// <summary>
/// 本地化文本组件。挂载于 UI 文本，按当前语言自动刷新显示内容。
/// </summary>
public class LocalizationText : MonoBehaviour
{
[SerializeField] private int localizationKey; // 文本的key
[SerializeField] private bool isFontOpenLocalize = true; // 字体是否开启本地化

private Text _textComponent;
#if SY_TMP
private TMP_Text _tmpTextComponent;
#endif
private object[] _param;
private bool _useTextMeshPro = false;

private void Awake()
{
#if SY_TMP
    if (GetComponent<TMP_Text>() != null)
    {
        _useTextMeshPro = true;
        _tmpTextComponent = GetComponent<TMP_Text>();
    }
    else
#endif
    {
        _useTextMeshPro = false;
        _textComponent = GetComponent<Text>();
    }

    UpdateText();

    if (Application.isPlaying)
    {
        LanguageManager.OnLocalizeChangedListener += UpdateText;
    }
}

protected void OnDestroy()
{
    if (Application.isPlaying)
    {
        LanguageManager.OnLocalizeChangedListener -= UpdateText;
    }
}

/// <summary>
/// 重新本地化，用于游戏内切换语言时调用
/// </summary>
private void UpdateText()
{
    if (isFontOpenLocalize)
    {
#if SY_TMP
        if (_useTextMeshPro)
        {
            if (LanguageManager.Instance.CurrentCountry.TMPFontAsset)
                _tmpTextComponent.font = LanguageManager.Instance.CurrentCountry.TMPFontAsset;
        }
        else
#endif
        {
            if (LanguageManager.Instance.CurrentCountry.Font)
                _textComponent.font = LanguageManager.Instance.CurrentCountry.Font;
        }
    }

    LanguageManager.GetStr(localizationKey, text =>
    {
        if (ArabicTextTool.HasArabicChar(text))
        {
            DoSetArabText(text);
        }
        else
        {
            SetText(text);
        }
    }, _param);
}

private void DoSetArabText(string orgStr, float width = 0.0f, float height = 0.0f)
{
    SetText(orgStr);
}

// ReSharper disable Unity.PerformanceAnalysis
/// <summary>
/// 编辑器模式修改
/// </summary>
public void EditorOnLocalize(CountryAndCode language)
{
    if (isFontOpenLocalize)
    {
#if SY_TMP
        if (GetComponent<TMP_Text>() != null)
        {
            _useTextMeshPro = true;
            _tmpTextComponent = GetComponent<TMP_Text>();

            if (language.TMPFontAsset)
                _tmpTextComponent.font = language.TMPFontAsset;
        }
#endif

        if (GetComponent<Text>() != null)
        {
            _useTextMeshPro = false;
            _textComponent = GetComponent<Text>();

            if (language.Font)
                _textComponent.font = language.Font;
        }
    }

    LanguageManager.GetStr(localizationKey, language, text =>
    {
        if (ArabicTextTool.HasArabicChar(text))
        {
            DoSetArabText(text);
        }
        else
        {
            SetText(text);
        }
    }, _param);
}

public void SetText(int key, params object[] param)
{
    localizationKey = key;
    _param = param;
    UpdateText();
}

public void SetText(params object[] param)
{
    _param = param;
    UpdateText();
}

public void SetText(string text)
{
#if SY_TMP
    if (_useTextMeshPro)
    {
        if (_tmpTextComponent)
            _tmpTextComponent.text = text;
    }
    else
#endif
    {
        if (_textComponent)
            _textComponent.text = text;
    }
}
}
}
