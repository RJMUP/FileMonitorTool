using System.IO;
using Serilog;

namespace FileMonitorTool.Services
{
    /// <summary>日志门面：内部委托给 Serilog（滚动文件 + 控制台），业务代码保持原有调用方式</summary>
    public static class LogService
    {
        /// <summary>初始化 Serilog：按天滚动 + 按大小滚动，自动清理超期日志</summary>
        public static void Configure(string logDir, int retainDays, int maxFileSizeMB)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.WithProperty("Source", "FileMonitorTool")
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(string.IsNullOrEmpty(logDir) ? "logs" : logDir, "log-.log"),
                    rollingInterval: RollingInterval.Day,       // 按天滚动
                    rollOnFileSizeLimit: true,                   // 超过大小新建文件
                    fileSizeLimitBytes: maxFileSizeMB > 0 ? maxFileSizeMB * 1024L * 1024L : 10 * 1024L * 1024L,
                    retainedFileCountLimit: retainDays > 0 ? retainDays : 30,   // 自动清理超期日志
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public static void Info(string msg) => Log.Information("{Message}", msg);
        public static void Warn(string msg) => Log.Warning("{Message}", msg);
        public static void Error(string msg) => Log.Error("{Message}", msg);

        /// <summary>程序退出前刷新并释放日志文件句柄</summary>
        public static void Shutdown() => Log.CloseAndFlush();
    }
}
