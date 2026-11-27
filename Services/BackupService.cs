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
                Console.WriteLine($"[待备份·未启用自动备份] {sourcePath}");
                return;
            }
            var relative = Path.GetRelativePath(_config.WatchFolder, sourcePath);
            if (relative.StartsWith(".."))
            {
                Console.WriteLine($"[跳过·越界路径] {sourcePath}");
                return;
            }
            var destPath = Path.Combine(_config.BackupFolder, relative);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);

            if (File.Exists(destPath) &&
                File.GetLastWriteTime(sourcePath) <= File.GetLastWriteTime(destPath))
                return;

            File.Copy(sourcePath, destPath, overwrite: true);
            Console.WriteLine($"[已备份] {sourcePath} -> {destPath}");
        }

        public int SnapBackup(bool isFull = false)
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
            int removed = isFull ? RemoveOrphans() : 0;
            Console.WriteLine($"[定时·{(isFull ? "全量镜像" : "增量备份")}] 处理 {count} 个文件" + (isFull ? $"，清理冗余 {removed} 个" : ""));
            return count;
        }

        private int RemoveOrphans()
        {
            int removed = 0;
            if (!Directory.Exists(_config.BackupFolder)) return 0;
            var searchOption = _config.IncludeSubDirectories
                ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.EnumerateFiles(_config.BackupFolder, "*.*", searchOption))
            {
                var relative = Path.GetRelativePath(_config.BackupFolder, file);
                if (relative.StartsWith("..")) continue;
                var srcPath = Path.Combine(_config.WatchFolder, relative);
                if (!File.Exists(srcPath) || _filter.IsIgnored(srcPath))
                {
                    File.Delete(file);
                    removed++;
                    Console.WriteLine($"[清理冗余] {file}");
                }
            }
            return removed;
        }
    }
}
