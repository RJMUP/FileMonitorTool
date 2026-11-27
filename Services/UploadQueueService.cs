using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public class UploadQueueService
    {
        private readonly Channel<string> _queue;
        private readonly ConcurrentDictionary<string, DateTime> _recent;
        private readonly TimeSpan _debounce;
        private readonly BackupService _backup;

        public UploadQueueService(MonitorConfig config, BackupService backup)
        {
            _backup = backup;
            _debounce = TimeSpan.FromMilliseconds(config.WatchDebounceMs);
            _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(config.MaxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            _recent = new ConcurrentDictionary<string, DateTime>();
        }

        public async Task<bool> EnqueueAsync(string fullPath)
        {
            var now = DateTime.Now;
            if (_recent.TryGetValue(fullPath, out var last) && (now - last) < _debounce)
                return false;
            _recent[fullPath] = now;
            CleanExpired(now);

            await _queue.Writer.WriteAsync(fullPath);
            Console.WriteLine($"[入队] {fullPath} (待处理: {_queue.Reader.Count})");
            return true;
        }

        private void CleanExpired(DateTime now)
        {
            foreach (var kv in _recent)
                if (now - kv.Value > _debounce) _recent.TryRemove(kv.Key, out _);
        }

        public async Task StartConsumerAsync()
        {
            await foreach (var fullPath in _queue.Reader.ReadAllAsync())
            {
                try { _backup.BackupFile(fullPath); }
                catch (Exception ex) { Console.WriteLine($"[错误] 处理 {fullPath}: {ex.Message}"); }
            }
        }
    }
}
