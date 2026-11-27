using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FileMonitorTool.Services;

namespace FileMonitorTool
{
    class Program
    {
        // 静默模式：隐藏控制台窗口（配合开机启动使用）
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;

        static async Task Main(string[] args)
        {
            var silent = args.Any(a => a.Equals("--silent", StringComparison.OrdinalIgnoreCase));
            if (silent)
            {
                ShowWindow(GetConsoleWindow(), SW_HIDE);
                LogService.EnableFileLog = true;   // 静默模式强制写文件日志
            }

            try
            {
                Console.WriteLine("=== 本地文件监控 + 忽略过滤 + 去重备份 + 定时备份 ===");

                var cfg = ConfigService.Load();
                LogService.LogDir = cfg.LogFolder;
                if (!silent) LogService.EnableFileLog = cfg.EnableFileLog;

                LogService.Info($"监控目录 : {cfg.WatchFolder}");
                LogService.Info($"备份目录 : {cfg.BackupFolder}");
                LogService.Info($"忽略规则 : {string.Join(", ", cfg.IgnoreRules)}");
                var windowInfo = string.IsNullOrEmpty(cfg.ScheduledStartTime) && string.IsNullOrEmpty(cfg.ScheduledEndTime)
                    ? "全天" : $"{cfg.ScheduledStartTime}~{cfg.ScheduledEndTime}";
                LogService.Info($"定时备份 : {(cfg.EnableScheduledBackup ? "启用，间隔 " + cfg.ScheduledIntervalMinutes + " 分钟，时段 " + windowInfo : "未启用")}");
                LogService.Info($"运行模式 : {(silent ? "静默（--silent）" : "控制台")}");

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

                var filter = new FileFilterService(cfg.IgnoreRules);
                var backup = new BackupService(cfg, filter);
                var queue = new UploadQueueService(cfg, backup, filter);
                var watcher = new FileWatcherService(cfg, queue, filter);

                _ = queue.StartConsumerAsync();          // 队列消费者（实时备份）
                watcher.Start();                          // 文件监控
                var scheduler = new ScheduledBackupService(cfg, backup, filter);
                _ = scheduler.StartAsync();               // 定时备份

                LogService.Info("\n按 Ctrl+C 退出...");
                await Task.Delay(System.Threading.Timeout.Infinite);
            }
            catch (Exception ex)
            {
                // 静默运行时无人值守，任何启动异常必须落盘日志
                LogService.Error($"[启动失败] {ex}");
                if (!silent)
                {
                    Console.WriteLine("按任意键退出...");
                    Console.ReadKey();
                }
            }
        }
    }
}
