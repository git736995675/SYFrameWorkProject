using System;
using System.Collections.Generic;
using System.Linq;
using ZSYFramework.Core.Json;
using ZSYFramework.Table;


namespace ZSYFramework.Localization;

/// <summary>
/// 语言设置表契约类。由配置表生成的数据结构，定义语言与字体的映射模式。
/// </summary>
public class LanguageSettingConfig  
{ 

	/// 字符表 
	public int Id { get; set; }
	/// 对应语言中全称 
	public string Country { get; set; }
	/// 缩写 
	public string ShortName { get; set; }
	/// 中文翻译 
	public string Annotate { get; set; }
	/// 字体资源名称 
	public string FontAssetName { get; set; }
	/// TMP字体资源名称(未配置时为空, 回退 FontAssetName 对应逻辑) 
	public string TMPFontAssetName { get; set; }
	/// 在UI中的排序 
	public int Sort { get; set; }
	/// 在多语言表中的属性名称 
	public string StrKey { get; set; }
	/// 是否开启 
	public bool IsUse { get; set; } 


private const string ConfigName = "LanguageSettingConfig" ; 

private static Dictionary<string,LanguageSettingConfig> Data { get; set; } 

private static List<LanguageSettingConfig> _dataArray;


public static void LoadConfig(Action onComplete)
{
     if (Data != null)
     {
        onComplete?.Invoke(); 
        return;
     } 
     ConfigUtils.LoadConfigTextAsset<Dictionary<string,LanguageSettingConfig>>(ConfigName, (data) =>
     {
         Data = data;
         _dataArray = Data.Values.ToList();
         onComplete?.Invoke();
     });
}

public static void LoadConfig(string bundleName, string assetName, Action onComplete)
{
    if (Data != null)
    {
        onComplete?.Invoke(); 
        return;
    } 
    ConfigUtils.LoadConfigTextAsset(bundleName, assetName, (textAsset) =>
    {
        SetConfig(textAsset.text);
        onComplete?.Invoke();
    });
}

private static void SetConfig(string text)
{
    Data = JsonMapper.ToObject<Dictionary<string,LanguageSettingConfig>>(text);
    _dataArray = Data.Values.ToList();
}
    
public static void Get(int id,Action<LanguageSettingConfig>  callback) 
{ 
    LoadConfig(() =>
    {
        Data.TryGetValue(id.ToString(), out var config);
        callback?.Invoke(config);
    });
}  

public static void GetAll(Action<List<LanguageSettingConfig>>  callback) 
{ 
    LoadConfig(() =>
    {
        callback?.Invoke(_dataArray);
    });
}  

//------------------------------------------------ͬ������������ǰ����
public static LanguageSettingConfig Get(int id)
{
    Data.TryGetValue(id.ToString(), out var config);
    return config;
}

public static List<LanguageSettingConfig> GetAll()
{
    return _dataArray;
}
}
