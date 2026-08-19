using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain.Entities.Avtopark;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Avtopark;

/// <summary>
/// Maşın kartı — yaratma, redaktə, siyahı.
///
/// Siyahı sətrinə iki hesablanan sahə qoşulur: maşın hazırda kimdədir
/// («Çıxıb» statusunda açıq müraciət) və ən yaxın bitən müddət. İkisi də
/// AYRI sorğularla, `AsNoTracking` ilə gətirilir — filtered `Include` ilə
/// gətirilsəydi eyni kontekstdə ikinci tracking sorğusu naviqasiyanı
/// «düzəldib» filtri effektsiz edərdi (CLAUDE.md — EF Core fixup tələsi).
/// </summary>
public class MasinService : IMasinService
{
    private readonly IUnitOfWork _uow;

    public MasinService(IUnitOfWork uow) => _uow = uow;

    /// <summary>Maşını TUTAN statuslar — bunlarda maşın başqasına verilə bilməz.</summary>
    internal static readonly MasinMuracietStatus[] DiriStatuslar =
    {
        MasinMuracietStatus.Gozlemede,
        MasinMuracietStatus.Tesdiqlenib,
        MasinMuracietStatus.Cixib
    };

    public async Task<IList<MasinDto>> HamisiniGetirAsync(bool yalnizAktiv = false)
    {
        var sorgu = _uow.Repository<Masin>().Query().AsNoTracking()
            .Include(x => x.Departament)
            .Include(x => x.TehkimSurucu)
            .AsQueryable();

        if (yalnizAktiv)
            sorgu = sorgu.Where(x => x.Status == MasinStatus.Aktiv);

        var list = await sorgu
            .OrderBy(x => x.Marka).ThenBy(x => x.Model).ThenBy(x => x.DovletNomresi)
            .ToListAsync();

        var idler = list.Select(x => x.Id).ToList();

        // Hazırda çöldə olanlar — hər maşın üçün ən çoxu bir sətir (servis
        // ikinci açıq çıxışa icazə vermir), amma zəmanət kimi qruplaşdırılır.
        var acigCixislar = await _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
            .Include(x => x.Isci)
            .Where(x => idler.Contains(x.MasinId) && x.Status == MasinMuracietStatus.Cixib)
            .ToListAsync();

        var cixisMap = acigCixislar
            .GroupBy(x => x.MasinId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CixisTarixi).First());

        // Ən yaxın bitən AKTİV müddət (keçmişlər də daxildir — keçmiş müddət
        // gizlədilməməlidir, əksinə ən təcili odur).
        var muddetler = await _uow.Repository<MasinMuddet>().Query().AsNoTracking()
            .Include(x => x.Nov)
            .Where(x => idler.Contains(x.MasinId) && x.Aktivdir)
            .ToListAsync();

        var muddetMap = muddetler
            .GroupBy(x => x.MasinId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SonTarix).First());

        return list.Select(x =>
        {
            var dto = Map(x);
            if (cixisMap.TryGetValue(x.Id, out var c))
            {
                dto.IndiColdedir = true;
                dto.IndiKimde = $"{c.Isci?.Ad} {c.Isci?.Soyad}".Trim();
            }
            if (muddetMap.TryGetValue(x.Id, out var m))
            {
                dto.EnYaxinMuddetTarixi = m.SonTarix;
                dto.EnYaxinMuddetAdi = m.Nov?.Ad;
            }
            return dto;
        }).ToList();
    }

    public async Task<MasinDto?> GetirAsync(int id)
    {
        var e = await _uow.Repository<Masin>().Query().AsNoTracking()
            .Include(x => x.Departament)
            .Include(x => x.TehkimSurucu)
            .FirstOrDefaultAsync(x => x.Id == id);

        return e == null ? null : Map(e);
    }

    private static MasinDto Map(Masin x) => new()
    {
        Id = x.Id,
        DovletNomresi = x.DovletNomresi,
        Marka = x.Marka,
        Model = x.Model,
        BuraxilisIli = x.BuraxilisIli,
        Reng = x.Reng,
        Ban = x.Ban,
        Vin = x.Vin,
        Novu = x.Novu,
        DepartamentId = x.DepartamentId,
        DepartamentAdi = x.Departament?.Ad,
        TehkimSurucuId = x.TehkimSurucuId,
        TehkimSurucuAdi = x.TehkimSurucu == null
            ? null
            : $"{x.TehkimSurucu.Ad} {x.TehkimSurucu.Soyad}".Trim(),
        Status = x.Status,
        Qeyd = x.Qeyd
    };

    /// <summary>
    /// Yazma yollarının ORTAQ validasiyası — yaratma və redaktə eyni qaydadan
    /// keçsin. İki nüsxə saxlansa biri mütləq köhnə qalar.
    /// </summary>
    private async Task<Result?> YoxlaAsync(MasinCreateDto dto)
    {
        var nomre = (dto.DovletNomresi ?? "").Trim();
        if (nomre.Length == 0)
            return Result.Fail("Dövlət nömrəsi mütləqdir.");

        if (dto.BuraxilisIli is < 1900 or > 2100)
            return Result.Fail("Buraxılış ili düzgün deyil.");

        // Dublikat nömrə yoxlaması — YALNIZ silinməmişlər arasında.
        // Silinmiş maşının nömrəsi yenidən istifadə oluna bilər (maşın satılıb,
        // eyni nömrə yeni maşına keçib). Bu, jurnal nömrəsi DEYİL — geri
        // qaytarılmayan sayğac qaydası buraya aid deyil.
        var varmi = await _uow.Repository<Masin>().Query().AsNoTracking()
            .AnyAsync(x => x.Id != dto.Id && x.DovletNomresi == nomre);

        if (varmi)
            return Result.Fail($"«{nomre}» nömrəli maşın artıq mövcuddur.");

        return null;
    }

    public async Task<Result<int>> YaratAsync(MasinCreateDto dto, int userId)
    {
        var xeta = await YoxlaAsync(dto);
        if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");

        var e = new Masin
        {
            DovletNomresi = dto.DovletNomresi.Trim(),
            Marka = dto.Marka?.Trim(),
            Model = dto.Model?.Trim(),
            BuraxilisIli = dto.BuraxilisIli,
            Reng = dto.Reng?.Trim(),
            Ban = dto.Ban?.Trim(),
            Vin = dto.Vin?.Trim(),
            Novu = dto.Novu?.Trim(),
            DepartamentId = dto.DepartamentId,
            TehkimSurucuId = dto.TehkimSurucuId,
            Status = dto.Status,
            Qeyd = dto.Qeyd?.Trim(),
            YaradanIcraciId = userId
        };

        await _uow.Repository<Masin>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(e.Id, $"«{e.DovletNomresi}» əlavə edildi.");
    }

    public async Task<Result> YenileAsync(MasinCreateDto dto, int userId)
    {
        var xeta = await YoxlaAsync(dto);
        if (xeta != null) return xeta;

        var e = await _uow.Repository<Masin>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Maşın tapılmadı.");

        // Maşın çöldə ikən «Təmirdə»/«İstifadədən çıxıb» edilə bilməz — açıq
        // çıxış qapanmamış maşını sıradan çıxarsaq, kassa «Gəldi» düyməsini
        // basa bilməz və jurnal əbədi açıq qalar.
        if (dto.Status != MasinStatus.Aktiv && e.Status == MasinStatus.Aktiv)
        {
            var acigVar = await _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
                .AnyAsync(x => x.MasinId == e.Id && x.Status == MasinMuracietStatus.Cixib);

            if (acigVar)
                return Result.Fail(
                    "Maşın hazırda çöldədir — statusu dəyişmək üçün əvvəlcə kassa «Gəldi» qeydini etməlidir.");
        }

        e.DovletNomresi = dto.DovletNomresi.Trim();
        e.Marka = dto.Marka?.Trim();
        e.Model = dto.Model?.Trim();
        e.BuraxilisIli = dto.BuraxilisIli;
        e.Reng = dto.Reng?.Trim();
        e.Ban = dto.Ban?.Trim();
        e.Vin = dto.Vin?.Trim();
        e.Novu = dto.Novu?.Trim();
        e.DepartamentId = dto.DepartamentId;
        e.TehkimSurucuId = dto.TehkimSurucuId;
        e.Status = dto.Status;
        e.Qeyd = dto.Qeyd?.Trim();
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<Masin>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Maşın yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId)
    {
        var e = await _uow.Repository<Masin>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Maşın tapılmadı.");

        // Diri müraciəti olan maşın silinmir — silinsə həmin müraciətlər
        // heç bir ekranda görünməz, amma bazada «Çıxıb» qalar.
        var diriVar = await _uow.Repository<MasinMuraciet>().Query().AsNoTracking()
            .AnyAsync(x => x.MasinId == id && DiriStatuslar.Contains(x.Status));

        if (diriVar)
            return Result.Fail(
                "Bu maşının açıq müraciəti var — əvvəlcə onlar bağlanmalı (qayıdış/ləğv) və ya imtina edilməlidir.");

        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<Masin>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Maşın silindi.");
    }
}
