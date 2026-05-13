using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class FealiyyetJurnaliController : Controller
{
    private readonly AppDbContext _db;
    public FealiyyetJurnaliController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? user     = null,
        string? cedvel   = null,
        string? emel     = null,
        string? basTarix = null,
        string? sonTarix = null,
        int     page     = 1)
    {
        const int PageSize = 50;

        var query = _db.FealiyyetJurnali
            .Include(x => x.User)
            .AsNoTracking()
            .AsQueryable();

        // Filterlər
        if (!string.IsNullOrWhiteSpace(user))
            query = query.Where(x => x.User != null &&
                (x.User.UserName!.Contains(user) || x.User.Ad!.Contains(user) || x.User.Soyad!.Contains(user)));

        if (!string.IsNullOrWhiteSpace(cedvel))
            query = query.Where(x => x.CedvelAdi == cedvel);

        if (!string.IsNullOrWhiteSpace(emel))
            query = query.Where(x => x.Emeliyyat == emel);

        if (DateTime.TryParse(basTarix, out var bas))
            query = query.Where(x => x.Tarix >= bas);

        if (DateTime.TryParse(sonTarix, out var son))
            query = query.Where(x => x.Tarix < son.AddDays(1));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Tarix)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Filter üçün mövcud cədvəl adları
        var cedvellar = await _db.FealiyyetJurnali
            .Select(x => new { x.CedvelAdi, x.CedvelFarsi })
            .Distinct()
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Items       = items;
        ViewBag.Total       = total;
        ViewBag.Page        = page;
        ViewBag.PageSize    = PageSize;
        ViewBag.PageCount   = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Cedvellar   = cedvellar.DistinctBy(x => x.CedvelAdi).OrderBy(x => x.CedvelFarsi).ToList();
        ViewBag.FilterUser  = user;
        ViewBag.FilterCedvel = cedvel;
        ViewBag.FilterEmel  = emel;
        ViewBag.FilterBas   = basTarix;
        ViewBag.FilterSon   = sonTarix;

        return View();
    }
}
