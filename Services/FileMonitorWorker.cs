using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FileMonitorTool.Services
{
    /// <summary>文件监控核心：启动监控与队列消费者，随服务生命周期自动启停</summary>
    public class FileMonitorWorker : BackgroundService
    {
        private readonly FileWatcherService _watcher;
        private readonly UploadQueueService _queue;

        public FileMonitorWorker(FileWatcherService watcher, UploadQueueService queue)
        {
            _watcher = watcher;
            _queue = queue;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher.Start();
            return _queue.StartConsumerAsync(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _watcher.Stop();
            return base.StopAsync(cancellationToken);
        }
    }
}
