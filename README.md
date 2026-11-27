# FileMonitorTool

一个 .NET 10 控制台工具：监控源目录文件变化，实时把"未被忽略规则命中"的文件复制到备份目录，
并支持定时（周期）备份与文件日志。备份目录只新增和修改，从不删除——源文件被删除或重命名后，
备份中仍保留历史副本，可随时找回。

## 快速开始

1. 编辑 config.json，设置 WatchFolder（源）与 BackupFolder（备份）
2. 运行：dotnet run

## 主要功能

* 实时监控：基于 FileSystemWatcher 监听文件创建/修改/重命名/删除
* 忽略过滤：正则规则（整路径 + 逐路径段匹配），入队前过滤 + 复制前双重防线
* 去重防抖：相同文件在 WatchDebounceMs 窗口内只入队一次
* 删除保留：源文件删除后，备份目录中的对应文件保留不动（历史副本可找回）
* 定时备份：按周期执行增量备份（只新增和修改，不删除任何备份文件）
* 文件日志：控制台输出 + 按 logs/yyyyMMdd.log 追加，可配置关闭

## 使用教程

### 第 1 步：环境准备

* 安装 .NET 10 SDK（`dotnet --info` 可查看版本）
* 确认源目录存在（如 `D:\Work\Project`），备份目录可以不存在，程序会自动创建

### 第 2 步：修改配置

编辑项目根目录下的 `config.json`，至少修改以下两项：

```json
{
  "WatchFolder": "D:\\Work\\Project",
  "BackupFolder": "E:\\Backup\\Project"
}
```

> 注意：JSON 中 Windows 路径的反斜杠需要写成 `\\`。

### 第 3 步：启动工具

```bash
dotnet run
```

启动后可看到类似输出：

```
=== 本地文件监控 + 忽略过滤 + 去重备份 + 定时备份 ===
监控目录 : D:\Work\Project
备份目录 : E:\Backup\Project
忽略规则 : \.git, \.node_modules, ^bin$, ...
定时备份 : 启用，间隔 30 分钟，模式 增量，时段 全天
```

保持窗口运行即可，按 `Ctrl+C` 退出。

### 第 4 步：验证备份效果

在源目录中新建或修改任意文件（如 `a.txt`），窗口会出现：

```
2026-11-27 10:00:00 [INFO ] [入队] D:\Work\Project\a.txt (Created) (待处理: 0)
2026-11-27 10:00:00 [INFO ] [已备份] D:\Work\Project\a.txt -> E:\Backup\Project\a.txt
```

同时检查：

* 备份目录中出现了同样的文件（子目录结构保持一致）
* `logs/` 目录下生成了当天的日志文件 `20261127.log`
* 新建 `bin\test.dll` 或 `.git\config` 等命中忽略规则的文件，不会备份，日志显示 `[忽略]`

### 第 5 步：验证删除保留

删除源目录中的 `a.txt`，日志显示：

```
[删除·不联动] D:\Work\Project\a.txt（备份目录保留原文件）
```

备份目录中的 `a.txt` 不会被删除，仍可从 `E:\Backup\Project\a.txt` 找回。

### 常见场景配置

**场景 1：只在工作时间做定时备份**

```json
"ScheduledStartTime": "09:00",
"ScheduledEndTime": "18:00"
```

实时监控不受影响，仍全天生效。

**场景 2：防止误删——删除自动保留历史副本**

无需任何配置，这是本工具的默认行为：源文件被删除后，备份目录中的对应文件
不会被联动删除，可随时手动从备份目录找回。

**场景 3：只想备份某类文件**

```json
"Filter": "*.docx"
```

**场景 4：不监控子目录**

```json
"IncludeSubDirectories": false
```

**场景 5：源文件被误删，从备份找回**

直接到备份目录中按相同相对路径复制回来即可。由于备份目录只增改不删，
即使文件早已被删除，只要删除前备份过，副本仍然存在。

### 常见问题（FAQ）

| 现象 | 原因与处理 |
|---|---|
| 启动即提示 `[警告] 源目录不存在` | 检查 WatchFolder 路径是否正确 |
| 文件改动频繁导致日志刷屏 | 正常现象，防抖（WatchDebounceMs）会自动合并短时间内的重复事件 |
| 队列提示 `待处理` 数字持续增长 | 大批量复制文件所致，程序会按队列顺序自动消化；可调大 MaxQueueSize |
| 修改 config.json 后不生效 | 需重启程序（`Ctrl+C` 后重新 `dotnet run`），配置只在启动时加载 |
| 忽略规则不生效 | 规则是正则表达式，注意转义（如 `\.` 表示点号）；目录规则用 `^目录名$` 命中任意层级同名目录 |
| 备份目录出现源目录已删除的文件 | 设计如此：备份目录只新增和修改、从不删除，删除的文件可作为历史副本找回 |

## 开机启动 + 静默运行

程序支持 `--silent` 参数：启动后自动隐藏控制台窗口，并**强制写入文件日志**（即使 EnableFileLog=false），
任何启动异常也会落盘到 `logs/yyyyMMdd.log`，适合无人值守运行。

### 方式一（推荐）：计划任务，开机即启动（真静默，无窗口闪现）

以管理员身份运行 PowerShell：

```powershell
# 1. 发布程序
dotnet publish -c Release -r win-x64 --self-contained false -o publish

# 2. 注册开机启动计划任务（SYSTEM 账户运行，不显示任何窗口）
schtasks /Create /TN "FileMonitorTool" /SC ONSTART /RU SYSTEM `
  /TR "\"D:\工作目录\2.ZlStudy\FileMonitorTool\publish\FileMonitorTool.exe\" --silent" /F
```

> SYSTEM 账户运行时注意：访问的是系统级环境，映射的网络驱动器（如 Z:\）不可见，
> 请使用 UNC 路径（如 `\\server\share`）或本地路径。

管理任务：

```powershell
schtasks /Run    /TN "FileMonitorTool"   # 手动启动
schtasks /End    /TN "FileMonitorTool"   # 手动停止
schtasks /Delete /TN "FileMonitorTool" /F # 卸载
```

### 方式二：登录时启动（窗口会闪现一下后隐藏）

```powershell
schtasks /Create /TN "FileMonitorTool" /SC ONLOGON `
  /TR "\"D:\工作目录\2.ZlStudy\FileMonitorTool\publish\FileMonitorTool.exe\" --silent" /F
```

适合需要访问当前用户资源的场景（如桌面目录），登录后窗口自动隐藏。

### 方式三：手动测试静默运行

```powershell
dotnet run -- --silent
```

启动后窗口立即隐藏，只看得到 `logs/` 下的日志文件在增长。调试时可去掉 `--silent` 观察控制台输出。

## 配置说明

### 基础

* WatchFolder: 源目录
* BackupFolder: 备份目录
* IncludeSubDirectories: 是否递归子目录
* Filter: 文件通配符
* IgnoreRules: 忽略规则（正则，忽略大小写）；默认值与 .gitignore 对齐（bin/obj/logs/publish/.vs/.vscode/.idea、*.user、*.suo、*.tmp、~$* 等）
* WatchDebounceMs: 防抖毫秒数
* MaxQueueSize: 队列上限（满时阻塞写入）
* AutoBackup: 是否自动备份

### 定时备份

* EnableScheduledBackup: 是否启用定时备份
* ScheduledIntervalMinutes: 间隔分钟数
* ScheduledStartTime / ScheduledEndTime: 仅在该时间段内执行，格式 HH:mm（如 "09:00" / "18:00"），支持跨零点（如 "22:00" / "06:00"）；两者留空表示全天执行

### 日志

* EnableFileLog: 是否写入文件日志
* LogFolder: 日志目录（按天分文件）

## 文件结构

FileMonitorTool/

├── FileMonitorTool.csproj        # .NET 10 项目文件

├── Program.cs                    # 程序入口

├── config.json                   # 配置文件

├── README.md                     # 使用说明

├── Models/

│   └── MonitorConfig.cs          # 配置模型

└── Services/

    ├── ConfigService.cs          # 配置加载/保存

    ├── FileFilterService.cs      # 忽略规则过滤

    ├── LogService.cs             # 控制台 + 文件日志

    ├── BackupService.cs          # 核心备份逻辑（实时+定时复用，只增改不删）

    ├── UploadQueueService.cs     # 去重队列（生产者-消费者，按事件类型分流）

    ├── FileWatcherService.cs     # 文件监控

    └── ScheduledBackupService.cs # 定时备份服务
