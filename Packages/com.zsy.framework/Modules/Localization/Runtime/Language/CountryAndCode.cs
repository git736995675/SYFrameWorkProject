using System;
using TMPro;
using UnityEngine;

#if SY_TMP
#endif

namespace ZSYFramework.Localization
{
/// <summary>
/// 国家简称和在下拉框中的位置
/// </summary>
[Serializable]
/// <summary>
/// 国家与语言代码映射表。维护支持的语言地区列表及对应字体。
/// </summary>
public class CountryAndCode
{
public string Annotate; //注释

public string Country; //全称

public string ShortName; //简称

[NonSerialized]public string FontAssetName; // 字体的名称

public int Sort; //排序

public string StrKey; //再StrConfig中的key

public Font Font; //对应字体

#if SY_TMP
public TMP_FontAsset TMPFontAsset; //tmp字体
#endif
}

/// <summary>
/// 国家/地区描述结构：代码、名称、字体等信息。
/// </summary>
public enum Country
{
英文,
中文,
繁体中文,
印地语,
葡萄牙语,
阿拉伯语,
法语,
印尼语,
土耳其语,
德语,
西语,
日本,
韩语,
俄语,
意大利语,
越南语,
}

/// <summary>
/// 语言
/// </summary>
public struct LanguageConst
{
public const string zh = "zh"; //中文
public const string en = "en"; //英文
public const string hi = "hi"; //印地语
public const string pt = "pt"; //葡萄牙
public const string ar = "ar"; //阿拉伯语
public const string fr = "fr"; //法语
public const string id = "id"; //印尼语
public const string tr = "tr"; //土耳其语
public const string de = "de"; //德语
public const string es = "es"; //西语
public const string ja = "ja"; //日语
public const string ko = "ko"; //韩语
public const string tc = "tc"; //繁体中文
}
}
