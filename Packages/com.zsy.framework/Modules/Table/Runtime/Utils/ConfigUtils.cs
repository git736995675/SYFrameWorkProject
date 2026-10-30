using System;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using UnityEngine;
using ZSYFramework.Core.Json;
using ZSYFramework.AssetBundle;
using ZSYFramework.Core;
using ZSYFramework.Core.Json;

namespace ZSYFramework.Table
{
/// <summary>
/// 配置表工具集。提供 Excel 读取、配置资产加载等表格系统核心方法。
/// </summary>
public class ConfigUtils
{
private static string _rootDirectory;

private static string RootDirectory
{
    get
    {
        if (!string.IsNullOrEmpty(_rootDirectory)) return _rootDirectory;
#if UNITY_EDITOR
        const string scriptsName = "ConfigUtils";
        var path = UnityEditor.AssetDatabase.FindAssets(scriptsName);
        var root = Application.dataPath.Replace("Assets", "");
        _rootDirectory = root + UnityEditor.AssetDatabase.GUIDToAssetPath(path[0]).Replace((@"Utils/" + scriptsName + ".cs"), "");
#endif
        return _rootDirectory;
    }
}

/// <summary>
/// 模板存放位置
/// </summary>
public static string ModelPath => RootDirectory + "Model/";


/// <summary>
/// 表格读取
/// </summary>
public static DataSet ReadExcel(string filePath)
{
    try
    {
        if (string.IsNullOrEmpty(filePath))
        {
            SYLog.Error("表格路径为空！！！",LogCategory.Config);
            return null;
        }

        var fileName = Path.GetFileName(filePath);
        var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        IExcelDataReader excelDataReader ;
        if (fileName.Contains(".csv"))
        {
            var csvConfig = new ExcelReaderConfiguration
            {
                FallbackEncoding = Encoding.GetEncoding("GB2312")
            };
            excelDataReader = ExcelReaderFactory.CreateCsvReader(fileStream, csvConfig);
        }
        else
        {
            excelDataReader = ExcelReaderFactory.CreateOpenXmlReader(fileStream);
        }

        // 表格数据全部读取到result里(引入：DataSet（using System.Data;）
        var result = excelDataReader.AsDataSet();
        fileStream.Close();
        excelDataReader.Close();
        return result;
    }
    catch (Exception e)
    {
        SYLog.Error($"表格读取失败==>{filePath}  error==>{e}",LogCategory.Config);
        throw;
    }
}

/// <summary>
/// 打开表格
/// </summary>
public static void OpenExcel(string filePath)
{
    if (string.IsNullOrEmpty(filePath))
    {
        Debug.LogError("OpenExcel  表格路径为空！！！");
        return;
    }

    switch (Application.platform)
    {
        case RuntimePlatform.WindowsEditor:
            System.Diagnostics.Process.Start(filePath);
            break;
        case RuntimePlatform.OSXEditor:
            System.Diagnostics.Process.Start("open", filePath);
            break;
    }
}

/// <summary>
/// 加载configTextAssets文件
/// </summary>
/// <param name="assetName"></param>
/// <param name="typeName"></param>
/// <typeparam name="T"></typeparam>
/// <returns></returns>
public static void LoadConfigTextAsset<T>(string assetName,Action<T>  callback)
{
    ABLoadManager.Instance.LoadTextAsset(assetName, (json) =>
    {
        //json = AesSimple.JieMi(json);
        var config = JsonMapper.ToObject<T>(json.text);
        callback?.Invoke(config);
    });
}

public static void LoadConfigTextAsset(string bundleName ,string assetName,Action<TextAsset>  callback)
{
    ABLoadManager.Instance.Load<TextAsset>(bundleName,assetName, (json) =>
    {
        callback?.Invoke(json);
    });
}
}
}
