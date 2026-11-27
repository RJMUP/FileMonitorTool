using System;
using System.Threading.Tasks;
using FileMonitorTool.Services;

namespace FileMonitorTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 本地文件监控 + 忽略过滤 + 去重备份 + 定时备份 ===");

            var cfg = ConfigService.Load();
            Console.WriteLine($"监控目录 : {cfg.WatchFolder}");
            Console.WriteLine($"备份目录 : {cfg.BackupFolder}");
            Console.WriteLine($"忽略规则 : {string.Join(", ", cfg.IgnoreRules)}");
            Console.WriteLine($"定时备份 : {(cfg.EnableScheduledBackup ? "启用，间隔 " + cfg.ScheduledIntervalMinutes + " 分钟，模式 " + (cfg.FullMirrorOnScheduled ? "全量镜像" : "增量") : "未启用")}");

            if (!System.IO.Directory.Exists(cfg.WatchFolder))
            {
                Console.WriteLine($"[警告] 源目录不存在: {cfg.WatchFolder}");
                return;
            }
            if (!System.IO.Directory.Exists(cfg.BackupFolder))
            {
                System.IO.Directory.CreateDirectory(cfg.BackupFolder);
                Console.WriteLine($"[提示] 已创建备份目录: {cfg.BackupFolder}");
            }

            var filter = new FileFilterService(cfg.IgnoreRules);
            var backup = new BackupService(cfg, filter);
            var queue = new UploadQueueService(cfg, backup);
            var watcher = new FileWatcherService(cfg, queue, filter);

            _ = queue.StartConsumerAsync();          // 队列消费者（实时备份）
            watcher.Start();                          // 文件监控
            var scheduler = new ScheduledBackupService(cfg, backup, filter);
            _ = scheduler.StartAsync();               // ⭐ 定时备份

            Console.WriteLine("\n按 Ctrl+C 退出...");
            await Task.Delay(System.Threading.Timeout.Infinite);
        }
    }
}
