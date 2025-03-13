using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class MD5Helper
{
    public static string CalculateMD5(string filePath)
    {
        if (File.Exists(filePath))
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return BytesToHexString(hashBytes);
                }
            }
        }
        return null;
    }
    
    // 将字节数组转换为十六进制字符串
    private static string BytesToHexString(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2")); // 将每个字节转换为两位十六进制数
        }
        return sb.ToString();
    }
}
