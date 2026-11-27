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
                var defaultCfg = new MonitorConfig();
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
