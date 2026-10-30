using System;
using System.Collections.Generic;
using System.Linq;
using ZSYFramework.Core.Json;
using ZSYFramework.Table;


namespace ZSYFramework.Localization;

/// <summary>
/// 多语言文本表契约类。由配置表生成，Key-Value 形式的翻译条目结构。
/// </summary>
public class StrConfig  
{ 

	/// 多语言 
	public int Id { get; set; }
	/// 中文简体zh 
	public string CHINESE { get; set; }
	/// 英语en 
	public string ENGLISH { get; set; }
	/// 印地语hi 
	public string HINDI { get; set; }
	/// 葡萄牙语pt 
	public string PORTUGUESE { get; set; }
	/// 土耳其tr 
	public string TURKEY { get; set; }
	/// 印尼语id 
	public string INDONESIAN { get; set; }
	/// 西语es 
	public string SPANISH { get; set; }
	/// 日语ja 
	public string JAPANESE { get; set; }
	/// 韩语ko 
	public string KOREAN { get; set; }
	/// 德语de 
	public string GERMAN { get; set; }
	/// 法语fr 
	public string FRENCH { get; set; } 


private const string ConfigName = "StrConfig" ; 

private static Dictionary<string,StrConfig> Data { get; set; } 

private static List<StrConfig> _dataArray;


public static void LoadConfig(Action onComplete)
{
     if (Data != null)
     {
        onComplete?.Invoke(); 
        return;
     } 
     ConfigUtils.LoadConfigTextAsset<Dictionary<string,StrConfig>>(ConfigName, (data) =>
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
    Data = JsonMapper.ToObject<Dictionary<string,StrConfig>>(text);
    _dataArray = Data.Values.ToList();
}
    
public static void Get(int id,Action<StrConfig>  callback) 
{ 
    LoadConfig(() =>
    {
        Data.TryGetValue(id.ToString(), out var config);
        callback?.Invoke(config);
    });
}  

public static void GetAll(Action<List<StrConfig>>  callback) 
{ 
    LoadConfig(() =>
    {
        callback?.Invoke(_dataArray);
    });
}  

//------------------------------------------------ͬ������������ǰ����
public static StrConfig Get(int id)
{
    Data.TryGetValue(id.ToString(), out var config);
    return config;
}

public static List<StrConfig> GetAll()
{
    return _dataArray;
}
}
