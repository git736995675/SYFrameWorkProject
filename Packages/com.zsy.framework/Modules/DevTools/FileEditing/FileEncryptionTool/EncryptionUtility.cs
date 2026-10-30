using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;
using ZSYFramework.Core.Json;
using var aes = Aes.Create();
using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
using var decryptor = aes.CreateDecryptor();
using var encryptor = aes.CreateEncryptor();
using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
using var md5Hasher = MD5.Create();
using var ms = new MemoryStream();
using var ms = new MemoryStream(buffer);
using var ms = new MemoryStream(encryptedBytes);
using var output = new MemoryStream();
using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
using var sr = new StreamReader(cs);


namespace ZSYFramework.DevTools;
/// <summary>
/// 加密算法工具集。
/// </summary>
public static class EncryptionUtility
{
private const string Key = "T0KFuFExfBGGdhxvHe9UgrYqJ6ZfOG842lX4sNjZyeM=";
private const string Iv = "XaS8xVOWudVKQwjqiGXcTw==";

// 生成新的密钥和IV
public static (byte[] key, byte[] iv) GenerateKeyAndIV()
{
    aes.KeySize = 256;
    aes.GenerateKey();
    aes.GenerateIV();
    return (aes.Key, aes.IV);
}

// 加密文件
public static void EncryptFile(string inputPath, string outputPath, byte[] key, byte[] iv)
{
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    inputStream.CopyTo(cryptoStream);
}

// 解密文件
public static void DecryptFile(string inputPath, string outputPath, byte[] key, byte[] iv)
{
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    cryptoStream.CopyTo(outputStream);
}

// 加密字节数组
public static byte[] EncryptBytes(byte[] plainBytes, byte[] key, byte[] iv)
{
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    cs.Write(plainBytes, 0, plainBytes.Length);
    cs.FlushFinalBlock();
    return ms.ToArray();
}

public static byte[] DecryptBytes(byte[] encryptedBytes)
{
    var key = Convert.FromBase64String(Key);
    var iv = Convert.FromBase64String(Iv);
    return DecryptBytes(encryptedBytes, key, iv);
}

// 解密字节数组
public static byte[] DecryptBytes(byte[] encryptedBytes, byte[] key, byte[] iv)
{
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    cs.CopyTo(output);
    return output.ToArray();
}

// 加密字符串（如果需要对配置文本进行加密）
public static string EncryptString(string plainText, byte[] key, byte[] iv)
{
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    var encryptor = aes.CreateEncryptor();
    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
    using (var sw = new StreamWriter(cs))
    {
        sw.Write(plainText);
    }

    return Convert.ToBase64String(ms.ToArray());
}

public static string DecryptString(string cipherText)
{
    var key = Convert.FromBase64String(Key);
    var iv = Convert.FromBase64String(Iv);
    return DecryptString(cipherText, key, iv);
}

// 解密字符串
public static string DecryptString(string cipherText, byte[] key, byte[] iv)
{
    var buffer = Convert.FromBase64String(cipherText);
    aes.Key = key;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;

    var decryptor = aes.CreateDecryptor();
    return sr.ReadToEnd();
}

//字符串转md5
public static string ComputeMD5Hash(string rawData)
{
    var data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(rawData));
    var sBuilder = new StringBuilder();

    foreach (var t in data)
    {
        sBuilder.Append(t.ToString("x2"));
    }

    return sBuilder.ToString();
}

//将路径中的文件名转md5
public static string RenameFilenameToMD5(string oriPath)
{
    var extension = Path.GetExtension(oriPath);
    var md5Name = ComputeMD5Hash(Path.GetFileNameWithoutExtension(oriPath));
    var directoryName = oriPath.Replace(Path.GetFileName(oriPath), "");
    var newName = !string.IsNullOrEmpty(directoryName)
        ? $"{directoryName}{md5Name}{extension}"
        : $"{md5Name}{extension}";
    return newName;
}
}
