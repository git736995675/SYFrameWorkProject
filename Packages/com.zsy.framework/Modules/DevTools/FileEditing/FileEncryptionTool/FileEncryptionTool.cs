using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using ZSYFramework.Core.Json;

#if UNITY_EDITOR


namespace ZSYFramework.DevTools
{
/// <summary>
/// 文件加密工具窗口。
/// </summary>
public class FileEncryptionTool : EditorWindow
{
private string _keyString = "";
private string _ivString = "";
private byte[] _keyBytes = new byte[32]; // 256-bit key
private byte[] _ivBytes = new byte[16]; // 128-bit IV
private string _inputFolderPath = "";
private string _outputFolderPath = "";
private const string KeySavePath = "Encryption/Keys/";
private const string KeyName = "encryption_key";
private const string KeyExtension = "key";
private string _keySaveFullPath;
private bool _showByteView = true;
private bool _changeNameToMd5 = true;
private Vector2 _scrollPosition;

[MenuItem("SY-Tool/文件编辑/文件加解密工具")]
public static void ShowWindow()
{
    GetWindow<FileEncryptionTool>("文件加解密工具");
}

private void OnEnable()
{
    GneratePath();
    InitKeyFile();
}

private void OnGUI()
{
    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("文件加密解密工具", EditorStyles.boldLabel);
    EditorGUILayout.Space(10);

    // 密钥生成部分
    DrawKeySection();

    // 字节显示部分
    DrawByteDisplaySection();

    // 目录选择部分
    DrawDirectorySection();

    // 操作按钮部分
    DrawOperationButtons();

    EditorGUILayout.EndScrollView();
}

private void DrawKeySection()
{
    EditorGUILayout.LabelField("密钥设置", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);

    EditorGUILayout.BeginVertical(GUI.skin.box);

    if (GUILayout.Button("生成新密钥", GUILayout.Width(120)))
    {
        GenerateNewKey();
    }

    EditorGUILayout.Space(10);

    // 密钥输入/显示
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("Key (Base64):", GUILayout.Width(100));
    _keyString = EditorGUILayout.TextField(_keyString);
    if (GUILayout.Button("更新", GUILayout.Width(60)))
    {
        try
        {
            _keyBytes = Convert.FromBase64String(_keyString);
        }
        catch
        {
            EditorUtility.DisplayDialog("错误", "无效的Base64格式密钥", "确定");
        }
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("IV (Base64):", GUILayout.Width(100));
    _ivString = EditorGUILayout.TextField(_ivString);
    if (GUILayout.Button("更新", GUILayout.Width(60)))
    {
        try
        {
            _ivBytes = Convert.FromBase64String(_ivString);
        }
        catch
        {
            EditorUtility.DisplayDialog("错误", "无效的Base64格式IV", "确定");
        }
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(5);

    EditorGUILayout.LabelField($"密钥保存位置：{_keySaveFullPath} ", GUILayout.Width(1000));
    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("保存密钥到文件"))
    {
        SaveKeyToFile();
    }

    if (GUILayout.Button("从文件加载密钥"))
    {
        LoadKeyFromFile();
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndVertical();
    EditorGUILayout.Space(10);
}

private void DrawByteDisplaySection()
{
    EditorGUILayout.LabelField("字节数组显示", EditorStyles.boldLabel);
    _showByteView = EditorGUILayout.Toggle("显示字节视图", _showByteView);

    if (_showByteView)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("Key Bytes (32 bytes):");
        var keyBytesString = BitConverter.ToString(_keyBytes).Replace("-", " ");
        EditorGUILayout.HelpBox(keyBytesString, MessageType.None);

        EditorGUILayout.LabelField("IV Bytes (16 bytes):");
        var ivBytesString = BitConverter.ToString(_ivBytes).Replace("-", " ");
        EditorGUILayout.HelpBox(ivBytesString, MessageType.None);

        EditorGUILayout.EndVertical();
    }

    EditorGUILayout.Space(10);
}

private void DrawDirectorySection()
{
    EditorGUILayout.LabelField("目录设置", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);

    EditorGUILayout.BeginVertical(GUI.skin.box);

    // 输入目录
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("输入目录:", GUILayout.Width(80));
    _inputFolderPath = EditorGUILayout.TextField(_inputFolderPath);
    if (GUILayout.Button("浏览", GUILayout.Width(60)))
    {
        var path = EditorUtility.OpenFolderPanel("选择输入目录", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _inputFolderPath = path;
        }
    }

    EditorGUILayout.EndHorizontal();

    // 输出目录
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("输出目录:", GUILayout.Width(80));
    _outputFolderPath = EditorGUILayout.TextField(_outputFolderPath);
    if (GUILayout.Button("浏览", GUILayout.Width(60)))
    {
        var path = EditorUtility.OpenFolderPanel("选择输出目录", Application.dataPath, "");
        if (!string.IsNullOrEmpty(path))
        {
            _outputFolderPath = path;
        }
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndVertical();
    EditorGUILayout.Space(10);
}

private void DrawOperationButtons()
{
    EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
    EditorGUILayout.Space(5);
    _changeNameToMd5 = EditorGUILayout.Toggle("文件名是否改为md5", _changeNameToMd5);
    EditorGUILayout.BeginVertical(GUI.skin.box);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("加密目录下所有文件", GUILayout.Height(40)))
    {
        if (ValidateInput())
        {
            EncryptAllFiles();
        }
    }

    if (GUILayout.Button("解密目录下所有文件", GUILayout.Height(40)))
    {
        if (ValidateInput())
        {
            DecryptAllFiles();
        }
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space(10);

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("加密单个文件", GUILayout.Height(30)))
    {
        EncryptSingleFile();
    }

    if (GUILayout.Button("解密单个文件", GUILayout.Height(30)))
    {
        DecryptSingleFile();
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.BeginHorizontal();
    if (GUILayout.Button("加密单个文件中的字符串", GUILayout.Height(30)))
    {
        EncryptSingleStringToFile();
    }

    if (GUILayout.Button("解密单个文件中的字符串", GUILayout.Height(30)))
    {
        DecryptSingleStringToFile();
    }

    EditorGUILayout.EndHorizontal();

    EditorGUILayout.EndVertical();
}

private void GenerateNewKey()
{
    var aes = EncryptionUtility.GenerateKeyAndIV();
    _keyBytes = aes.key;
    _ivBytes = aes.iv;
    _keyString = Convert.ToBase64String(_keyBytes);
    _ivString = Convert.ToBase64String(_ivBytes);

    Debug.Log("新密钥已生成");
    Repaint();
}

private void SaveKeyToFile()
{
    var keyFilePath = EditorUtility.SaveFilePanel(
        "保存密钥文件",
        _keySaveFullPath,
        KeyName,
        KeyExtension);

    if (string.IsNullOrEmpty(_keySaveFullPath)) return;
    var keyData = new KeyData
    {
        KeyBase64 = _keyString,
        IVBase64 = _ivString,
        KeyBytes = _keyBytes,
        IVBytes = _ivBytes,
        CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    };

    var json = JsonUtility.ToJson(keyData, true);
    File.WriteAllText(keyFilePath, json);

    Debug.Log($"密钥已保存到: {keyFilePath}");
}

private void InitKeyFile()
{
    var keyPath = Path.Combine(_keySaveFullPath, KeyName);
    keyPath = $"{keyPath}.{KeyExtension}";
    if (string.IsNullOrEmpty(keyPath) || !File.Exists(keyPath)) return;
    try
    {
        var json = File.ReadAllText(keyPath);
        var keyData = JsonUtility.FromJson<KeyData>(json);

        _keyString = keyData.KeyBase64;
        _ivString = keyData.IVBase64;
        _keyBytes = keyData.KeyBytes;
        _ivBytes = keyData.IVBytes;

        Repaint();
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"加载密钥文件失败: {e.Message}", "确定");
    }
}

private void LoadKeyFromFile()
{
    var filePath = EditorUtility.OpenFilePanel("选择密钥文件", _keySaveFullPath, "key");

    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
    try
    {
        var json = File.ReadAllText(filePath);
        var keyData = JsonUtility.FromJson<KeyData>(json);

        _keyString = keyData.KeyBase64;
        _ivString = keyData.IVBase64;
        _keyBytes = keyData.KeyBytes;
        _ivBytes = keyData.IVBytes;

        Debug.Log($"密钥已从 {filePath} 加载");
        Repaint();
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"加载密钥文件失败: {e.Message}", "确定");
    }
}

private bool ValidateInput()
{
    if (_keyBytes is not { Length: 32 })
    {
        EditorUtility.DisplayDialog("错误", "密钥长度必须为32字节(256位)", "确定");
        return false;
    }

    if (_ivBytes is not { Length: 16 })
    {
        EditorUtility.DisplayDialog("错误", "IV长度必须为16字节(128位)", "确定");
        return false;
    }

    if (string.IsNullOrEmpty(_inputFolderPath) || !Directory.Exists(_inputFolderPath))
    {
        EditorUtility.DisplayDialog("错误", "请选择有效的输入目录", "确定");
        return false;
    }

    if (!Directory.Exists(_outputFolderPath))
    {
        Directory.CreateDirectory(_outputFolderPath);
    }

    return true;
}

private void EncryptAllFiles()
{
    try
    {
        var files = Directory.GetFiles(_inputFolderPath, "*.*", SearchOption.AllDirectories);
        var totalFiles = files.Length;
        var processedFiles = 0;

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath);
            if (ext is ".encrypted" or ".DS_Store" or ".meta")
            {
                continue;
            }

            var relativePath = filePath.Replace("\\", "/").Replace($"{_inputFolderPath}/", "");
            var outputPath = Path.Combine(_outputFolderPath, relativePath);
            var outputDir = Path.GetDirectoryName(outputPath);

            if (_changeNameToMd5)
            {
                outputPath = EncryptionUtility.RenameFilenameToMD5(outputPath);
            }

            if (!Directory.Exists(outputDir))
            {
                if (outputDir != null) Directory.CreateDirectory(outputDir);
            }

            EncryptionUtility.EncryptFile(filePath, outputPath, _keyBytes, _ivBytes);
            processedFiles++;

            // 更新进度条
            var progress = (float)processedFiles / totalFiles;
            EditorUtility.DisplayProgressBar("加密文件", $"正在加密: {Path.GetFileName(filePath)}", progress);
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("完成", $"成功加密 {processedFiles} 个文件", "确定");
        Debug.Log($"加密完成，输出目录: {_outputFolderPath}");
    }
    catch (Exception e)
    {
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("错误", $"加密失败: {e.Message}", "确定");
    }
}

private void DecryptAllFiles()
{
    try
    {
        var files = Directory.GetFiles(_inputFolderPath, "*.*", SearchOption.AllDirectories);
        var totalFiles = files.Length;
        var processedFiles = 0;

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath);
            if (ext is ".encrypted" or ".DS_Store" or ".meta")
            {
                continue;
            }

            var relativePath = filePath.Replace($"{_inputFolderPath}/", "");
            if (relativePath.EndsWith(".encrypted"))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 10); // 移除.encrypted扩展名
            }

            var outputPath = Path.Combine(_outputFolderPath, relativePath);
            var outputDir = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            EncryptionUtility.DecryptFile(filePath, outputPath, _keyBytes, _ivBytes);
            processedFiles++;

            // 更新进度条
            var progress = (float)processedFiles / totalFiles;
            EditorUtility.DisplayProgressBar("解密文件", $"正在解密: {Path.GetFileName(filePath)}", progress);
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("完成", $"成功解密 {processedFiles} 个文件", "确定");
        Debug.Log($"解密完成，输出目录: {_outputFolderPath}");
    }
    catch (Exception e)
    {
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("错误", $"解密失败: {e.Message}", "确定");
    }
}

private void EncryptSingleFile()
{
    var filePath = EditorUtility.OpenFilePanel("选择要加密的文件", Application.dataPath, "*");
    var extension = Path.GetExtension(filePath).Replace(".", "");
    if (string.IsNullOrEmpty(filePath)) return;
    var outputPath =
        EditorUtility.SaveFilePanel("保存加密文件", _outputFolderPath, Path.GetFileName(filePath), extension);

    if (string.IsNullOrEmpty(outputPath)) return;
    try
    {
        EncryptionUtility.EncryptFile(filePath, outputPath, _keyBytes, _ivBytes);
        EditorUtility.DisplayDialog("完成", "文件加密成功", "确定");
        Debug.Log($"文件已加密: {outputPath}");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"加密失败: {e.Message}", "确定");
    }
}

private void EncryptSingleStringToFile()
{
    var filePath = EditorUtility.OpenFilePanel("选择要加密的文件", Application.dataPath, "*");
    var extension = Path.GetExtension(filePath).Replace(".", "");
    if (string.IsNullOrEmpty(filePath)) return;
    var outputPath =
        EditorUtility.SaveFilePanel("保存加密文件", _outputFolderPath, Path.GetFileName(filePath), extension);

    if (string.IsNullOrEmpty(outputPath)) return;
    try
    {
        var fileAllText = File.ReadAllText(filePath);
        var encryptString = EncryptionUtility.EncryptString(fileAllText, _keyBytes, _ivBytes);
        File.WriteAllText(outputPath, encryptString);
        EditorUtility.DisplayDialog("完成", $"文件加密成功  {outputPath}", "确定");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"加密失败: {e.Message}", "确定");
    }
}


private void DecryptSingleStringToFile()
{
    var filePath = EditorUtility.OpenFilePanel("选择要解密的文件", Application.dataPath, "*");
    var extension = Path.GetExtension(filePath).Replace(".", "");
    if (string.IsNullOrEmpty(filePath)) return;
    var outputPath =
        EditorUtility.SaveFilePanel("保存解密文件", _outputFolderPath, Path.GetFileName(filePath), extension);

    if (string.IsNullOrEmpty(outputPath)) return;
    try
    {
        var fileAllText = File.ReadAllText(filePath);
        var encryptString = EncryptionUtility.DecryptString(fileAllText, _keyBytes, _ivBytes);
        File.WriteAllText(outputPath, encryptString);
        EditorUtility.DisplayDialog("完成", $"文件解密成功  {outputPath}", "确定");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"解密密失败: {e.Message}", "确定");
    }
}

private void DecryptSingleFile()
{
    var filePath = EditorUtility.OpenFilePanel("选择要解密的文件", Application.dataPath, "");

    if (string.IsNullOrEmpty(filePath)) return;
    var defaultName = Path.GetFileNameWithoutExtension(filePath);
    var extension = Path.GetExtension(filePath).Replace(".", "");
    if (defaultName.EndsWith(".encrypted"))
    {
        defaultName = defaultName.Substring(0, defaultName.Length - 10);
    }

    var outputPath = EditorUtility.SaveFilePanel("保存解密文件", _outputFolderPath, defaultName, extension);

    if (string.IsNullOrEmpty(outputPath)) return;
    try
    {
        EncryptionUtility.DecryptFile(filePath, outputPath, _keyBytes, _ivBytes);
        EditorUtility.DisplayDialog("完成", "文件解密成功", "确定");
        Debug.Log($"文件已解密: {outputPath}");
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("错误", $"解密失败: {e.Message}", "确定");
    }
}

private void GneratePath()
{
    if (string.IsNullOrEmpty(_outputFolderPath))
        _outputFolderPath = Application.streamingAssetsPath;

    if (!string.IsNullOrEmpty(_keySaveFullPath)) return;
    var parentPath = Directory.GetParent(Application.dataPath)?.FullName;
    _keySaveFullPath = Path.Combine(parentPath, KeySavePath);

    if (!Directory.Exists(_keySaveFullPath))
    {
        Directory.CreateDirectory(_keySaveFullPath);
    }
}

[System.Serializable]
private class KeyData
{
    public string KeyBase64;
    public string IVBase64;
    public byte[] KeyBytes;
    public byte[] IVBytes;
    public string CreatedTime;
}
}

}
#endif
