using System.Collections.Generic;

namespace FileMonitorTool.Models
{
    public class MonitorConfig
    {
        public string WatchFolder { get; set; } = @"C:\Data\Source";
        public string BackupFolder { get; set; } = @"C:\Data\Backup";
        public bool IncludeSubDirectories { get; set; } = true;
        public string Filter { get; set; } = "*.*";
        // 忽略规则在 config.json 中维护（与 .gitignore 对齐），模型不设默认值
        public List<string> IgnoreRules { get; set; } = new();
        public int WatchDebounceMs { get; set; } = 500;
        public int MaxQueueSize { get; set; } = 1000;
        public bool AutoBackup { get; set; } = true;
        public bool LogIgnored { get; set; } = true;

        // ===== 定时备份配置 =====
        public bool EnableScheduledBackup { get; set; } = true;
        public int ScheduledIntervalMinutes { get; set; } = 30;
        // 执行时间段（HH:mm，如 "09:00"）；留空表示不限制，支持跨零点（如 22:00~06:00）
        public string ScheduledStartTime { get; set; } = "";
        public string ScheduledEndTime { get; set; } = "";

        // ===== 日志配置 =====
        public bool EnableFileLog { get; set; } = true;
        public string LogFolder { get; set; } = "logs";
    }
}
