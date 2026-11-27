using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public class QueueItem
    {
        public string FullPath { get; set; } = string.Empty;
        public WatcherChangeTypes ChangeType { get; set; }
    }

    public class UploadQueueService
    {
        private readonly Channel<QueueItem> _queue;
        private readonly ConcurrentDictionary<string, DateTime> _recent;
        private readonly TimeSpan _debounce;
        private readonly BackupService _backup;
        private readonly FileFilterService _filter;

        private DateTime _lastCleanTime = DateTime.MinValue;

        public UploadQueueService(MonitorConfig config, BackupService backup, FileFilterService filter)
        {
            _backup = backup;
            _filter = filter;
            _debounce = TimeSpan.FromMilliseconds(config.WatchDebounceMs);
            _queue = Channel.CreateBounded<QueueItem>(new BoundedChannelOptions(config.MaxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            _recent = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task<bool> EnqueueAsync(string fullPath, WatcherChangeTypes changeType)
        {
            // 忽略规则在入队前过滤，避免无谓入队与复制
            if (_filter.IsIgnored(fullPath))
            {
                LogService.Info($"[忽略] {fullPath}");
                return false;
            }

            var now = DateTime.Now;
            if (_recent.TryGetValue(fullPath, out var last) && (now - last) < _debounce)
                return false;
            _recent[fullPath] = now;

            // 每个防抖窗口最多做一次全量清理，避免每次入队都全表扫描
            if (now - _lastCleanTime > _debounce)
            {
                _lastCleanTime = now;
                CleanExpired(now);
            }

            await _queue.Writer.WriteAsync(new QueueItem { FullPath = fullPath, ChangeType = changeType });
            LogService.Info($"[入队] {fullPath} ({changeType}) (待处理: {_queue.Reader.Count})");
            return true;
        }

        private void CleanExpired(DateTime now)
        {
            foreach (var kv in _recent)
                if (now - kv.Value > _debounce) _recent.TryRemove(kv.Key, out _);
        }

        public async Task StartConsumerAsync()
        {
            await foreach (var item in _queue.Reader.ReadAllAsync())
            {
                try
                {
                    if (item.ChangeType == WatcherChangeTypes.Deleted)
                        LogService.Info($"[删除·不联动] {item.FullPath}（备份目录保留原文件）");
                    else
                        _backup.BackupFile(item.FullPath);
                }
                catch (Exception ex)
                {
                    LogService.Error($"处理 {item.FullPath}: {ex.Message}");
                }
            }
        }
    }
}
