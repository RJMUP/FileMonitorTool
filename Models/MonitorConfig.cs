using System.Collections.Generic;

namespace FileMonitorTool.Models
{
    public class MonitorConfig
    {
        public string WatchFolder { get; set; } = @"C:\Data\Source";
        public string BackupFolder { get; set; } = @"C:\Data\Backup";
        public bool IncludeSubDirectories { get; set; } = true;
        public string Filter { get; set; } = "*.*";
        public List<string> IgnoreRules { get; set; } = new()
        { "\\.git", "\\.node_modules", ".*\\.tmp$", "^~\\$.*" };
        public int WatchDebounceMs { get; set; } = 500;
        public int MaxQueueSize { get; set; } = 1000;
        public bool AutoBackup { get; set; } = true;
        public bool LogIgnored { get; set; } = true;

        // ===== 定时备份配置 =====
        public bool EnableScheduledBackup { get; set; } = true;
        public int ScheduledIntervalMinutes { get; set; } = 30;
        public bool FullMirrorOnScheduled { get; set; } = false;
    }
}
