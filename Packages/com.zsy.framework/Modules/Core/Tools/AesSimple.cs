
namespace ZSYFramework.Core;
﻿namespace SY
{
/// <summary>
/// AES 加解密工具。提供字符串与字节数组的对称加密、解密方法。
/// </summary>
public abstract class AesSimple
{
    public static string JiaMi(string str)
    {
        int i;
        var len = str.Length;
        var miWen = new char[len]; //申请内存
        for (i = 0; i < len; i++)
        {
            miWen[i] = (char)(str[i] + 1);
        }
        return new string(miWen);
    }

    public static string JieMi(string miWen)
    {
        int i;
        var len = miWen.Length;
        var str = new char[len]; //申请内存
        for (i = 0; i < len; i++)
        {
            str[i] = (char)(miWen[i] - 1);
        }
        return new string(str);
    }
}
}
