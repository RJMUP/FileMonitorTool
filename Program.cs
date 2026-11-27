using System;
using System.IO;
using System.Linq;
using FileMonitorTool.Models;
using FileMonitorTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FileMonitorTool
{
    class Program
    {
        static void Main(string[] args)
        {
            // Windows 服务默认工作目录是 C:\windows\system32，
            // 统一切换到 exe 所在目录，保证 config.json / logs / 相对路径都以程序目录为准
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var cfg = ConfigService.Load();

            // 日志目录解析为绝对路径（相对路径一律以 exe 所在目录为基准）
            var logDir = Path.IsPathRooted(cfg.LogFolder)
                ? cfg.LogFolder
                : Path.Combine(AppContext.BaseDirectory, cfg.LogFolder);

            // 初始化 Serilog：按天/按大小滚动，自动清理超期日志
            LogService.Configure(logDir, cfg.LogRetainDays, cfg.LogMaxFileSizeMB);

            LogService.Info("=== 本地文件监控 + 忽略过滤 + 去重备份 + 定时备份（Windows 服务）===");
            LogService.Info($"监控目录 : {cfg.WatchFolder}");
            LogService.Info($"备份目录 : {cfg.BackupFolder}");
            LogService.Info($"忽略规则 : {string.Join(", ", cfg.IgnoreRules)}");

            if (!System.IO.Directory.Exists(cfg.WatchFolder))
            {
                LogService.Warn($"[启动中止] 源目录不存在: {cfg.WatchFolder}");
                return;
            }
            if (!System.IO.Directory.Exists(cfg.BackupFolder))
            {
                System.IO.Directory.CreateDirectory(cfg.BackupFolder);
                LogService.Info($"[提示] 已创建备份目录: {cfg.BackupFolder}");
            }

            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();   // 关闭框架自带日志
            builder.Logging.AddSerilog();       // 接入 Serilog

            builder.Services.AddWindowsService(o => o.ServiceName = "FileMonitorTool");
            builder.Services.AddSingleton(cfg);
            builder.Services.AddSingleton(new FileFilterService(cfg.IgnoreRules));
            builder.Services.AddSingleton<BackupService>();
            builder.Services.AddSingleton<UploadQueueService>();
            builder.Services.AddSingleton<FileWatcherService>();
            builder.Services.AddHostedService<FileMonitorWorker>();
            builder.Services.AddHostedService<ScheduledBackupService>();

            using var host = builder.Build();
            try
            {
                host.Run();
            }
            finally
            {
                LogService.Shutdown();   // 服务停止时刷新并释放日志文件
            }
        }
    }
}
