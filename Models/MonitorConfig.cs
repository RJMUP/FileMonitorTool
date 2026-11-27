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
        public string LogFolder { get; set; } = "logs";
        // 日志保留天数：超过自动删除
        public int LogRetainDays { get; set; } = 30;
        // 单个日志文件大小上限（MB）：超过后新建文件滚动写入
        public int LogMaxFileSizeMB { get; set; } = 10;
    }
}
