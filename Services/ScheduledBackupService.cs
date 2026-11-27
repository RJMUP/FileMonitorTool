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
        private CancellationTokenSource? _cts;

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
                LogService.Info("[定时备份] 未启用（EnableScheduledBackup=false）");
                return;
            }
            var interval = TimeSpan.FromMinutes(
                _config.ScheduledIntervalMinutes > 0 ? _config.ScheduledIntervalMinutes : 1);
            TimeSpan windowStart = default, windowEnd = default;
            var hasWindow = TryParseTime(_config.ScheduledStartTime, out windowStart)
                         && TryParseTime(_config.ScheduledEndTime, out windowEnd);
            LogService.Info($"[定时备份] 已启动，每 {interval.TotalMinutes} 分钟执行一次（只新增和修改，不删除）"
                            + (hasWindow ? $"，仅 {windowStart:hh\\:mm}~{windowEnd:hh\\:mm} 执行" : "，全天执行") + "\n");

            var outOfWindow = false;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, token);
                if (token.IsCancellationRequested) break;

                if (hasWindow && !InWindow(DateTime.Now.TimeOfDay, windowStart, windowEnd))
                {
                    if (!outOfWindow)
                    {
                        outOfWindow = true;
                        LogService.Info($"[定时备份] 当前时间 {DateTime.Now:HH:mm} 不在执行时间段 "
                                        + $"{windowStart:hh\\:mm}~{windowEnd:hh\\:mm} 内，跳过");
                    }
                    continue;
                }
                outOfWindow = false;

                try
                {
                    LogService.Info($"[定时备份] === {DateTime.Now:HH:mm:ss} 开始 ===");
                    _backup.SnapBackup();
                    LogService.Info($"[定时备份] === {DateTime.Now:HH:mm:ss} 完成 ===\n");
                }
                catch (Exception ex) { LogService.Error($"[定时备份] 出错: {ex.Message}"); }
            }
        }

        /// <summary>解析 HH:mm 格式时间</summary>
        private static bool TryParseTime(string? value, out TimeSpan time)
            => TimeSpan.TryParseExact(value, "hh\\:mm", null, out time);

        /// <summary>判断当前时刻是否在时间段内（支持跨零点）</summary>
        private static bool InWindow(TimeSpan now, TimeSpan start, TimeSpan end)
            => start <= end ? now >= start && now <= end : now >= start || now <= end;

        public void Stop() => _cts?.Cancel();
    }
}
