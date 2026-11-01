using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace qHRM.SharedSysFuncs
{
    /// <summary>
    /// Provides two-way AES-256 encryption and decryption for sensitive configuration
    /// credentials such as SMTP Service_AcctPwd.
    /// Supports transparent fallback for unencrypted legacy passwords.
    /// </summary>
    public static class CryptoSecurityHelper
    {
        private const string Prefix = "ENC:";
        private static readonly byte[] EncryptionKey = SHA256.Create().ComputeHash(
            Encoding.UTF8.GetBytes("qHRM_2026_Secure_SysConfig_AES256_Key#!"));

        /// <summary>
        /// Checks if a string has already been encrypted with the AES helper.
        /// </summary>
        public static bool IsEncrypted(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && text.StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Encrypts plaintext using AES-256-CBC with a random IV and returns 'ENC:{base64(IV + CipherText)}'.
        /// If already encrypted, returns the text unchanged.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            if (IsEncrypted(plainText))
                return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.GenerateIV();

                using (var ms = new MemoryStream())
                {
                    // Write the 16-byte IV at the beginning of the stream
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var encryptor = aes.CreateEncryptor())
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }

                    return Prefix + Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts an 'ENC:{base64}' string back to plaintext.
        /// If the string is unencrypted (legacy plaintext), it safely returns the input as-is.
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            if (!IsEncrypted(cipherText))
            {
                // Legacy plaintext fallback
                return cipherText;
            }

            try
            {
                string rawBase64 = cipherText.Substring(Prefix.Length);
                byte[] allBytes = Convert.FromBase64String(rawBase64);

                if (allBytes.Length < 16)
                    return cipherText;

                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;

                    byte[] iv = new byte[16];
                    Buffer.BlockCopy(allBytes, 0, iv, 0, 16);
                    aes.IV = iv;

                    using (var ms = new MemoryStream(allBytes, 16, allBytes.Length - 16))
                    using (var decryptor = aes.CreateDecryptor())
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // If decoding fails, return as-is for safety
                return cipherText;
            }
        }
    }
}
