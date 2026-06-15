using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Game.Core
{
    public sealed class SecureTokenStorage : ITokenStorage
    {
        private const string Prefix = "Sec.";
        private readonly byte[] _key;

        public SecureTokenStorage()
        {
            var seed = $"{SystemInfo.deviceUniqueIdentifier}:project-fill";
            using var sha = SHA256.Create();
            _key = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        }

        public string Get(string key, string defaultValue = "")
        {
            var raw = PlayerPrefs.GetString(Prefix + key, "");
            if (string.IsNullOrEmpty(raw)) return defaultValue;
            try { return Decrypt(raw); }
            catch { return defaultValue; }
        }

        public void Set(string key, string value) =>
            PlayerPrefs.SetString(Prefix + key, Encrypt(value ?? ""));

        public void Delete(string key) => PlayerPrefs.DeleteKey(Prefix + key);

        public void Save() => PlayerPrefs.Save();

        private string Encrypt(string plain)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            using var enc = aes.CreateEncryptor();
            var cipherBytes = enc.TransformFinalBlock(Encoding.UTF8.GetBytes(plain), 0, plain.Length);
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return Convert.ToBase64String(result);
        }

        private string Decrypt(string cipher)
        {
            var data = Convert.FromBase64String(cipher);
            using var aes = Aes.Create();
            aes.Key = _key;
            var iv = new byte[aes.BlockSize / 8];
            var cipherBytes = new byte[data.Length - iv.Length];
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(data, iv.Length, cipherBytes, 0, cipherBytes.Length);
            aes.IV = iv;
            using var dec = aes.CreateDecryptor();
            return Encoding.UTF8.GetString(dec.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length));
        }
    }
}
