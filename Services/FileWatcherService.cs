using System;
using System.IO;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public class FileWatcherService
    {
        private readonly UploadQueueService _queue;
        private readonly FileFilterService _filter;
        private FileSystemWatcher _watcher;

        public FileWatcherService(MonitorConfig config, UploadQueueService queue, FileFilterService filter)
        {
            _queue = queue;
            _filter = filter;
            _watcher = new FileSystemWatcher
            {
                Path = config.WatchFolder,
                IncludeSubdirectories = config.IncludeSubDirectories,
                Filter = config.Filter ?? "*.*",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName

                             | NotifyFilters.DirectoryName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size
            };
        }

        public void Start()
        {
            _watcher.Created += OnChange;
            _watcher.Changed += OnChange;
            _watcher.Renamed += (s, e) => _ = _queue.EnqueueAsync(e.FullPath, e.ChangeType);
            _watcher.Deleted += (s, e) => _ = _queue.EnqueueAsync(e.FullPath, WatcherChangeTypes.Deleted);
            LogService.Info($"监控已启动: {_watcher.Path}");
            LogService.Info($"忽略规则数: {_filter.RuleCount}");
        }

        private void OnChange(object sender, FileSystemEventArgs e)
            => _ = _queue.EnqueueAsync(e.FullPath, e.ChangeType);
        public void Stop() => _watcher?.Dispose();
    }
}