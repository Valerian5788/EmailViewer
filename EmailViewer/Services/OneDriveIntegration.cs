using System;
using System.IO;
using EmailViewer.Helpers;

namespace EmailViewer.Services
{
    public class OneDriveIntegration
    {
        private static string oneDriveRootPath;
        private const string ONEDRIVE_BASE_URL = "https://1drv.ms/u/s!";
        private static string RESOURCE_ID => Environment.GetEnvironmentVariable("ONEDRIVE_RESSOURCEID");

        public static void SetOneDriveRootPath(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Log($"OneDrive root path does not exist: {path}");
                throw new DirectoryNotFoundException("The specified OneDrive folder does not exist.");
            }
            oneDriveRootPath = path;
            Logger.Log($"OneDrive root path set to: {path}");
        }

        public static string GetOneDriveLink(string localPath)
        {
            Logger.Log($"Attempting to generate OneDrive link for: {localPath}");

            if (string.IsNullOrEmpty(oneDriveRootPath))
            {
                Logger.Log("OneDrive root path has not been set.");
                throw new InvalidOperationException("OneDrive root path has not been set.");
            }

            if (string.IsNullOrEmpty(localPath))
            {
                Logger.Log("Local path is null or empty.");
                throw new ArgumentNullException(nameof(localPath));
            }

            if (!localPath.StartsWith(oneDriveRootPath, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"File is not in the OneDrive folder. OneDrive root: {oneDriveRootPath}, File path: {localPath}");
                throw new ArgumentException("The file is not in the OneDrive folder.", nameof(localPath));
            }

            // Generate a unique identifier for the file
            string fileIdentifier = GenerateFileIdentifier(localPath);

            // Construct the OneDrive link
            string oneDriveLink = $"{ONEDRIVE_BASE_URL}{RESOURCE_ID}{fileIdentifier}";

            Logger.Log($"Generated OneDrive link: {oneDriveLink}");
            return oneDriveLink;
        }

        private static string GenerateFileIdentifier(string localPath)
        {
            // This is a simplified method to generate a unique identifier
            // In a real-world scenario, you might want to use a more sophisticated approach
            string relativePath = localPath.Substring(oneDriveRootPath.Length).TrimStart('\\', '/');
            string hash = BitConverter.ToString(System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(relativePath))).Replace("-", "").Substring(0, 15);
            return hash;
        }
    }
}