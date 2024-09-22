using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailViewer
{
    public static class Logger
    {
        public static void Log(string message)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
