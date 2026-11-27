using System;
using System.Threading;
using System.Threading.Tasks;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public class ScheduledBackupService
    {
        private readonly MonitorConfig _config;
        private readonly BackupService _backup;
        private CancellationTokenSource _cts;

        public ScheduledBackupService(MonitorConfig config, BackupService backup, FileFilterService filter)
        {
            _config = config;
            _backup = backup;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            return Task.Run(() => LoopAsync(_cts.Token));
        }

        private async Task LoopAsync(CancellationToken token)
        {
            if (!_config.EnableScheduledBackup)
            {
                Console.WriteLine("[定时备份] 未启用（EnableScheduledBackup=false）");
                return;
            }
            var interval = TimeSpan.FromMinutes(
                _config.ScheduledIntervalMinutes > 0 ? _config.ScheduledIntervalMinutes : 1);
            Console.WriteLine($"[定时备份] 已启动，每 {interval.TotalMinutes} 分钟执行一次 "
                            + (_config.FullMirrorOnScheduled ? "全量镜像" : "增量") + "\n");

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, token);
                if (token.IsCancellationRequested) break;
                try
                {
                    Console.WriteLine($"[定时备份] === {DateTime.Now:HH:mm:ss} 开始 ===");
                    _backup.SnapBackup(_config.FullMirrorOnScheduled);
                    Console.WriteLine($"[定时备份] === {DateTime.Now:HH:mm:ss} 完成 ===\n");
                }
                catch (Exception ex) { Console.WriteLine($"[定时备份] 出错: {ex.Message}"); }
            }
        }

        public void Stop() => _cts?.Cancel();
    }
}
