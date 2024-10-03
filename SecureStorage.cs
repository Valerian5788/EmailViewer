using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace EmailViewer
{
    public class SecureStorage
    {
        private const string CONFIG_FILE = "config.enc";
        private static readonly byte[] Salt = new byte[] { 8, 2, 4, 6, 1, 9, 5, 7 };

        public static void SaveEncrypted(string key, string value, string password)
        {
            var config = LoadConfig(password);
            config[key] = value;
            SaveConfig(config, password);
        }


        public static string GetEncrypted(string key, string password)
        {
            var config = LoadConfig(password);
            return config.TryGetValue(key, out var value) ? value : null;
        }

        private static Dictionary<string, string> LoadConfig(string password)
        {
            if (!File.Exists(CONFIG_FILE))
                return new Dictionary<string, string>();

            var encryptedData = File.ReadAllBytes(CONFIG_FILE);
            var decrypted = Decrypt(encryptedData, password);
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(decrypted)
                ?? new Dictionary<string, string>();
        }

        private static void SaveConfig(Dictionary<string, string> config, string password)
        {
            var json = JsonConvert.SerializeObject(config);
            var encryptedData = Encrypt(json, password);
            File.WriteAllBytes(CONFIG_FILE, encryptedData);
        }

        private static byte[] Encrypt(string plainText, string password)
        {
            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Salt, 10000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
                sw.Write(plainText);

            return ms.ToArray();
        }

        private static string Decrypt(byte[] cipherText, string password)
        {
            using var aes = Aes.Create();
            var key = new Rfc2898DeriveBytes(password, Salt, 10000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}