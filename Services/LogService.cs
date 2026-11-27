using System;
using System.IO;

namespace FileMonitorTool.Services
{
    /// <summary>简单日志：控制台输出 + 追加写入 logs/yyyyMMdd.log</summary>
    public static class LogService
    {
        private static readonly object _lock = new();
        public static bool EnableFileLog { get; set; } = true;
        public static string LogDir { get; set; } = "logs";

        public static void Info(string msg) => Write("INFO ", msg);
        public static void Warn(string msg) => Write("WARN ", msg);
        public static void Error(string msg) => Write("ERROR", msg);

        private static void Write(string level, string msg)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {msg}";
            Console.WriteLine(line);

            if (!EnableFileLog) return;
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(LogDir);
                    File.AppendAllText(
                        Path.Combine(LogDir, $"{DateTime.Now:yyyyMMdd}.log"),
                        line + Environment.NewLine);
                }
            }
            catch { /* 日志写入失败不影响主流程 */ }
        }
    }
}
