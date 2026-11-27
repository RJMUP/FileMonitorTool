using System;
using System.IO;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public class BackupService
    {
        private readonly MonitorConfig _config;
        private readonly FileFilterService _filter;

        public BackupService(MonitorConfig config, FileFilterService filter)
        {
            _config = config;
            _filter = filter;
        }

        public void BackupFile(string sourcePath)
        {
            if (!_config.AutoBackup || string.IsNullOrEmpty(_config.BackupFolder))
            {
                LogService.Info($"[待备份·未启用自动备份] {sourcePath}");
                return;
            }

            // 最终防线：即使事件侧漏过滤，也不会复制被忽略的文件
            if (_filter.IsIgnored(sourcePath)) return;

            var relative = Path.GetRelativePath(_config.WatchFolder, sourcePath);
            if (relative.StartsWith(".."))
            {
                LogService.Warn($"[跳过·越界路径] {sourcePath}");
                return;
            }
            var destPath = Path.Combine(_config.BackupFolder, relative);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

            if (File.Exists(destPath) &&
                File.GetLastWriteTime(sourcePath) <= File.GetLastWriteTime(destPath))
                return;

            File.Copy(sourcePath, destPath, overwrite: true);
            LogService.Info($"[已备份] {sourcePath} -> {destPath}");
        }

        /// <summary>定时备份：只新增和修改，从不删除备份目录中的任何文件</summary>
        public int SnapBackup()
        {
            if (!Directory.Exists(_config.WatchFolder)) return 0;
            var count = 0;
            var searchOption = _config.IncludeSubDirectories
                ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(_config.WatchFolder, _config.Filter ?? "*.*", searchOption))
            {
                if (_filter.IsIgnored(file)) continue;
                BackupFile(file);
                count++;
            }
            LogService.Info($"[定时备份] 处理 {count} 个文件");
            return count;
        }
    }
}
