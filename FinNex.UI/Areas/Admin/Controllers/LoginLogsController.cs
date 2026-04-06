using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class LoginLogsController : Controller
{
    private readonly AppDbContext _db;

    public LoginLogsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        const int pageSize = 50;

        var query = _db.LoginLogs.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x =>
                x.UserName.Contains(search) ||
                (x.FullName != null && x.FullName.Contains(search)) ||
                (x.IpAddress != null && x.IpAddress.Contains(search)));
        }

        if (status == "success")
            query = query.Where(x => x.IsSuccess);
        else if (status == "fail")
            query = query.Where(x => !x.IsSuccess);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var logs = await query
            .OrderByDescending(x => x.LoginTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LoginLogVM
            {
                Id = x.Id,
                UserName = x.UserName,
                FullName = x.FullName,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                IsSuccess = x.IsSuccess,
                FailReason = x.FailReason,
                LoginTime = x.LoginTime
            })
            .ToListAsync();

        var vm = new LoginLogPageVM
        {
            Logs = logs,
            SearchTerm = search,
            StatusFilter = status,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        ViewData["Title"] = "Login Logs";
        return View(vm);
    }
}
