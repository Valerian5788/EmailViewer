using System;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;

public class AuthManager
{
    private const string AUTH_FILE = "auth.json";
    private static readonly byte[] Entropy = new byte[16] { 124,5,5,2,52,51,5,5,16,1,56, 11,96,41,55,5 };

    public static void SaveAuthToken(string token)
    {
        string encryptedToken = EncryptString(token);
        File.WriteAllText(AUTH_FILE, JsonConvert.SerializeObject(new { Token = encryptedToken }));
    }

    public static string LoadAuthToken()
    {
        if (File.Exists(AUTH_FILE))
        {
            string json = File.ReadAllText(AUTH_FILE);
            var data = JsonConvert.DeserializeAnonymousType(json, new { Token = "" });
            return DecryptString(data.Token);
        }
        return null;
    }

    public static void ClearAuthToken()
    {
        if (File.Exists(AUTH_FILE))
        {
            File.Delete(AUTH_FILE);
        }
    }

    private static string EncryptString(string input)
    {
        byte[] encryptedData = ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(input),
            Entropy,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedData);
    }

    private static string DecryptString(string encryptedData)
    {
        try
        {
            byte[] decryptedData = ProtectedData.Unprotect(
                Convert.FromBase64String(encryptedData),
                Entropy,
                DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(decryptedData);
        }
        catch
        {
            return null;
        }
    }
}