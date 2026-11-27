using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileMonitorTool.Services
{
    public class FileFilterService
    {
        private readonly List<Regex> _rules;
        public int RuleCount => _rules.Count;

        public FileFilterService(IEnumerable<string> rules)
        {
            _rules = (rules ?? Enumerable.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => new Regex(r, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();
        }

        public bool IsIgnored(string fullPath)
        {
            var normalized = fullPath.Replace('\\', '/');
            foreach (var rule in _rules)
            {
                if (rule.IsMatch(normalized)) return true;
                foreach (var part in normalized.Split('/'))
                    if (rule.IsMatch(part)) return true;
            }
            return false;
        }
    }
}
