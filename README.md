# FileMonitorTool

一个 .NET 8 控制台工具：监控源目录文件变化，实时把"未被忽略规则命中"的文件复制到备份目录，
并支持定时（周期）备份。

## 快速开始

1. 编辑 config.json，设置 WatchFolder（源）与 BackupFolder（备份）
2. 运行：dotnet run

## 定时备份配置

* EnableScheduledBackup: 是否启用定时备份
* ScheduledIntervalMinutes: 间隔分钟数
* FullMirrorOnScheduled: true=全量镜像(含清理冗余文件)，false=增量



### 文件结构

FileMonitorTool/

├── FileMonitorTool.csproj        # .NET 8 项目文件

├── Program.cs                    # 程序入口

├── config.json                   # 配置文件

├── README.md                     # 使用说明

├── Models/

│   └── MonitorConfig.cs          # 配置模型

└── Services/

&#x20;   ├── ConfigService.cs          # 配置加载/保存

&#x20;   ├── FileFilterService.cs      # 忽略规则过滤

&#x20;   ├── BackupService.cs          # 核心备份逻辑（实时+定时复用）

&#x20;   ├── UploadQueueService.cs     # 去重队列（生产者-消费者）

&#x20;   ├── FileWatcherService.cs     # 文件监控

&#x20;   └── ScheduledBackupService.cs # ⭐ 定时备份服务（新增）

