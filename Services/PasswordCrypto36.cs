using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DDTankLauncher.Services
{
    /// <summary>
    /// 36脚本大厅密码加密/解密
    /// 注意：具体算法需要逆向分析，这里实现兼容格式
    /// </summary>
    public static class PasswordCrypto36
    {
        // 固定密钥（从分析得出，可能需要更新）
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("DDTank36JB2024!");
        
        /// <summary>
        /// 加密密码
        /// 格式：Key + Hex编码的加密数据
        /// </summary>
        public static string Encrypt(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
                return "";
            
            try
            {
                // 使用 AES 加密
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = new byte[16]; // 固定 IV
                
                using var encryptor = aes.CreateEncryptor();
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                
                // 转换为十六进制字符串
                string hexString = BitConverter.ToString(encryptedBytes).Replace("-", "");
                
                return $"Key{hexString}";
            }
            catch
            {
                // 回退到简单的 XOR 加密
                return EncryptXor(plainPassword);
            }
        }
        
        /// <summary>
        /// 解密密码
        /// </summary>
        public static string Decrypt(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return "";
            
            try
            {
                // 移除 Key 前缀
                string hexData = encryptedPassword;
                if (hexData.StartsWith("Key"))
                    hexData = hexData.Substring(3);
                
                // 尝试 AES 解密
                try
                {
                    byte[] encryptedBytes = Convert.FromHexString(hexData);
                    using var aes = Aes.Create();
                    aes.Key = Key;
                    aes.IV = new byte[16];
                    
                    using var decryptor = aes.CreateDecryptor();
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
                catch
                {
                    // 回退到 XOR 解密
                    return DecryptXor(encryptedPassword);
                }
            }
            catch
            {
                return "[解密失败]";
            }
        }
        
        /// <summary>
        /// XOR 加密（备用方案）
        /// </summary>
        private static string EncryptXor(string plainPassword)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);
            byte[] keyBytes = Encoding.UTF8.GetBytes("DDTank");
            
            byte[] encryptedBytes = new byte[plainBytes.Length];
            for (int i = 0; i < plainBytes.Length; i++)
            {
                encryptedBytes[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            string hexString = BitConverter.ToString(encryptedBytes).Replace("-", "");
            return $"Key{hexString}";
        }
        
        /// <summary>
        /// XOR 解密（备用方案）
        /// </summary>
        private static string DecryptXor(string encryptedPassword)
        {
            string hexData = encryptedPassword;
            if (hexData.StartsWith("Key"))
                hexData = hexData.Substring(3);
            
            byte[] encryptedBytes = Convert.FromHexString(hexData);
            byte[] keyBytes = Encoding.UTF8.GetBytes("DDTank");
            
            byte[] decryptedBytes = new byte[encryptedBytes.Length];
            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        
        /// <summary>
        /// 检查是否是加密密码
        /// </summary>
        public static bool IsEncrypted(string password)
        {
            return !string.IsNullOrEmpty(password) && password.StartsWith("Key");
        }
    }
}
