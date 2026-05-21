using FinNex.Application.Interfaces;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services;

public class SistemAyarService : ISistemAyarService
{
    private readonly AppDbContext _db;

    public SistemAyarService(AppDbContext db) => _db = db;

    public async Task<SistemAyar> GetirAsync()
        => await _db.SistemAyarlari.AsNoTracking().FirstOrDefaultAsync()
           ?? new SistemAyar();
}
