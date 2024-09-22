using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailViewer
{
    public class OneDriveIntegration
    {
        private static string oneDriveRootPath;
        private const string ONEDRIVE_BASE_URL = "https://1drv.ms/";
        private const string SHARE_ID = "s!Aos4jtA_FWlQjhTF93YNtpjKbzXx";

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

            string relativePath = localPath.Substring(oneDriveRootPath.Length).TrimStart('\\', '/');
            string encodedPath = Uri.EscapeDataString(relativePath);
            string oneDriveLink = $"{ONEDRIVE_BASE_URL}{SHARE_ID}?path=/{encodedPath}";

            Logger.Log($"Generated OneDrive link: {oneDriveLink}");
            return oneDriveLink;
        }
    }
}
