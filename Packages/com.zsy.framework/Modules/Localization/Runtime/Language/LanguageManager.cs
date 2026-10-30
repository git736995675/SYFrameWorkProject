using System;
using UnityEngine;
using ZSYFramework.Core;


namespace ZSYFramework.Localization
{
/// <summary>
/// 多语言管理
/// </summary>
public class LanguageManager : MonoSingleton<LanguageManager>
{
/// <summary>
/// 多语言修改监听
/// </summary>
public static Action OnLocalizeChangedListener;


/// <summary>
/// 本地存储的key
/// </summary>
private string _languageSaveKey = "LanguageSettingKey";

/// <summary>
/// 多语言设置
/// </summary>
private LanguageSetting _languageSetting;

/// <summary>
/// 默认语言
/// </summary>
private string _defaultLanguage = LanguageConst.zh;

/// <summary>
/// 当前语言
/// </summary>
private CountryAndCode _currentCountry;

public CountryAndCode CurrentCountry
{
    get
    {
        if (_currentCountry == null)
        {
            ResetCountry(_defaultLanguage);
        }

        return _currentCountry;
    }
}

public CountryAndCode[] CountryS
{
    get
    {
        if(_languageSetting==null)
            _languageSetting = FindObjectOfType<LanguageSetting>();

        if (_languageSetting == null)
        {
            Debug.LogError("场景中未发现=>LanguageSetting");
        }
        return _languageSetting.countryS;
    }
}

/// <summary>
/// 刷新当前字体和语言
/// </summary>
private void ResetCountry(string targetLanguage)
{
    CountryAndCode defaultCountry = null;
    //刷新当前语言
    foreach (var country in _languageSetting.countryS)
    {
        if (_defaultLanguage == country.ShortName)
        {
            defaultCountry = country;
        }

        if (targetLanguage != country.ShortName) continue;
        _currentCountry = country;
        PlayerPrefs.SetString(_languageSaveKey, _currentCountry.ShortName);
        return;
    }

    _currentCountry = defaultCountry;
}

protected override void Awake()
{
    base.Awake();

    InitLanguage();
}

/// <summary>
///	初始化语言类型
/// </summary>
/// <param name="lang"></param>
public void InitLanguage()
{
    _languageSetting = FindObjectOfType<LanguageSetting>();

    if (_languageSetting == null)
    {
        Debug.LogError("场景中未发现=>LanguageSetting");
        return;
    }

    var settingLanguage = PlayerPrefs.HasKey(_languageSaveKey)
        ? PlayerPrefs.GetString(_languageSaveKey)
        : GetLocalLanguage();
    ResetCountry(settingLanguage);
}

/// <summary>
/// 获取系统语言
/// </summary>
/// <returns></returns>
private string GetLocalLanguage()
{
#if !UNITY_EDITOR && UNITY_ANDROID
return AndroidManager.GetLocalLanguage();
#else
    return _defaultLanguage;
#endif
}

/// <summary>
/// 设置语言
/// </summary>
public void SetLanguage(string language)
{
    if (language == CurrentCountry.ShortName)
    {
        return;
    }

    ResetCountry(language);
    OnLocalizeChangedListener?.Invoke();
}

/// <summary>
/// 设置语言
/// </summary>
public void SetLanguage(int index)
{
    if (_languageSetting.countryS == null)
    {
        return;
    }

    if (index >= _languageSetting.countryS.Length)
    {
        return;
    }

    var cac = _languageSetting.countryS[index];

    SetLanguage(cac.ShortName);
}

/// <summary>
/// 返回语言
/// </summary>
/// <returns></returns>
public string GetLanguage()
{
    return CurrentCountry.ShortName;
}

/// <summary>
/// 获取当前语言下标
/// </summary>
/// <returns></returns>
public int GetIndex()
{
    for (var i = 0; i < _languageSetting.countryS.Length; i++)
    {
        if (CurrentCountry.ShortName == _languageSetting.countryS[i].ShortName)
        {
            return i;
        }
    }

    return 0;
}

/// <summary>
/// 方便调用
/// </summary>
public static void GetStr(int key, Action<string> action, params object[] param)
{
    if (param is not { Length: > 0 })
    {
        GetLangByKey(key, action, Instance.CurrentCountry);
    }
    else
    {
        GetLangByKey(key, str => { action?.Invoke(string.Format(str, param)); }, Instance.CurrentCountry);
    }
}

/// <summary>
/// 方便调用
/// </summary>
public static void GetStr(int key, CountryAndCode language, Action<string> action, params object[] param)
{
    if (param is not { Length: > 0 })
    {
        GetLangByKey(key, action, language);
    }
    else
    {
        GetLangByKey(key, str => { action?.Invoke(string.Format(str, param)); }, language);
    }
}

/// <summary>
/// 获取对应的语言的文字
/// </summary>
private static void GetLangByKey(int key, Action<string> action, CountryAndCode curLange = null)
{
    StrConfig.Get(key, config =>
    {
        var country = curLange ?? Instance.CurrentCountry;

        if (config == null || country == null)
        {
            action?.Invoke("");
            return;
        }

        var obj = config.GetType().GetProperty(country.StrKey)?.GetValue(config);
        var str = obj?.ToString();

        if (string.IsNullOrEmpty(str))
        {
            str = config.ENGLISH;
        }

        action?.Invoke(str);
    });
}
}
}
