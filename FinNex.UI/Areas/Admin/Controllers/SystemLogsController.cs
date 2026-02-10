using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SystemLogsController : Controller
{
    private readonly IWebHostEnvironment _env;
    private static readonly Regex LogLineRegex = new(
        @"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}[\.,]\d+)\s+\[(\w+)\]\s+(.*)",
        RegexOptions.Compiled);

    public SystemLogsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index(string? level, string? search, int page = 1)
    {
        const int pageSize = 50;
        var logsDir = Path.Combine(_env.ContentRootPath, "Logs");

        var vm = new SystemLogPageVM
        {
            SelectedLevel = level,
            SearchTerm = search,
            CurrentPage = page,
            PageSize = pageSize
        };

        if (!Directory.Exists(logsDir))
        {
            ViewData["Title"] = "System Logs";
            return View(vm);
        }

        var logFiles = Directory.GetFiles(logsDir, "*.txt")
            .Concat(Directory.GetFiles(logsDir, "*.log"))
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .ToList();

        if (!logFiles.Any())
        {
            ViewData["Title"] = "System Logs";
            return View(vm);
        }

        var allLogs = new List<SystemLogVM>();

        foreach (var file in logFiles)
        {
            var lines = System.IO.File.ReadAllLines(file);
            SystemLogVM? current = null;

            foreach (var line in lines)
            {
                var match = LogLineRegex.Match(line);
                if (match.Success)
                {
                    if (current != null)
                        allLogs.Add(current);

                    current = new SystemLogVM
                    {
                        Timestamp = DateTime.TryParse(match.Groups[1].Value.Replace(',', '.'), out var ts)
                            ? ts
                            : DateTime.MinValue,
                        Level = match.Groups[2].Value,
                        Message = match.Groups[3].Value
                    };
                }
                else if (current != null)
                {
                    current.Message += Environment.NewLine + line;
                }
            }

            if (current != null)
                allLogs.Add(current);
        }

        // Sort newest first
        allLogs = allLogs.OrderByDescending(l => l.Timestamp).ToList();

        // Filter by level
        if (!string.IsNullOrEmpty(level))
        {
            allLogs = allLogs
                .Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Filter by search term
        if (!string.IsNullOrEmpty(search))
        {
            allLogs = allLogs
                .Where(l => l.Message.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        vm.TotalPages = (int)Math.Ceiling(allLogs.Count / (double)pageSize);
        vm.CurrentPage = Math.Clamp(page, 1, Math.Max(1, vm.TotalPages));
        vm.Logs = allLogs
            .Skip((vm.CurrentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewData["Title"] = "System Logs";
        return View(vm);
    }
}
