using System.Security.Cryptography;
using System.Text;


namespace ZSYFramework.Core
{
/// <summary>
/// 功能描述：
/// </summary>
public class UUIDGenerator
{
public static string GenerateUUID(string input)
{
    // 使用SHA256进行哈希处理
    using (SHA256 sha256 = SHA256.Create())
    {
        // 将输入字符串转换为字节数组
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // 将字节数组转换为32位的十六进制字符串
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        // 返回32位的UUID（不包含连字符）
        return sb.ToString().Substring(0, 32); // 保证返回32个字符
    }
}
}
}
