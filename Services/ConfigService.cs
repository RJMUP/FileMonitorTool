using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FileMonitorTool.Models;

namespace FileMonitorTool.Services
{
    public static class ConfigService
    {
        private const string FileName = "config.json";

        public static MonitorConfig Load()
        {
            if (!File.Exists(FileName))
            {
                var defaultCfg = new MonitorConfig
                {
                    // 首次生成的默认忽略规则，与 .gitignore 对齐
                    IgnoreRules = new List<string>
                    {
                        "\\.git",              // Git 元数据
                        "\\.node_modules",     // Node 依赖
                        "^bin$", "^obj$",      // .NET 构建输出
                        "^logs$",              // 日志目录
                        "^publish$",           // 发布目录
                        "^\\.vs$", "^\\.vscode$", "^\\.idea$",  // IDE 临时目录
                        ".*\\.user$", ".*\\.suo$",              // IDE 用户文件
                        ".*\\.tmp$",           // 临时文件
                        "^~\\$.*"              // Office 临时文件
                    }
                };
                Save(defaultCfg);
                Console.WriteLine($"[初始化] 已生成默认配置文件: {FileName}");
                return defaultCfg;
            }
            var json = File.ReadAllText(FileName);
            var cfg = JsonSerializer.Deserialize<MonitorConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return cfg ?? new MonitorConfig();
        }

        public static void Save(MonitorConfig cfg)
        {
            var json = JsonSerializer.Serialize(cfg,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileName, json);
        }
    }
}
