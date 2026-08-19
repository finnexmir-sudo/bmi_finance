using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Domain.Entities.Avtopark;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Avtopark;

/// <summary>
/// Maşın müddətləri — sığorta, texniki baxış, yağ dəyişmə…
///
/// UZADILMA QAYDASI: köhnə sətir SİLİNMİR, <c>Aktivdir=false</c> olur və yeni
/// sətir əlavə edilir. Beləliklə «bu maşının sığortası hansı illərdə nə qədərə
/// uzadılıb» tarixçəsi qalır. Xəbərdarlıq yalnız aktiv sətirlərə baxır.
/// </summary>
public class MasinMuddetService : IMasinMuddetService
{
    private readonly IUnitOfWork _uow;

    public MasinMuddetService(IUnitOfWork uow) => _uow = uow;

    // ══ NÖVLƏR ════════════════════════════════════════════════════════════

    public async Task<IList<MasinMuddetNovuDto>> NovleriGetirAsync(bool yalnizAktiv = false)
    {
        var sorgu = _uow.Repository<MasinMuddetNovu>().Query().AsNoTracking();
        if (yalnizAktiv) sorgu = sorgu.Where(x => x.Aktivdir);

        return await sorgu
            .OrderBy(x => x.Sira).ThenBy(x => x.Ad)
            .Select(x => new MasinMuddetNovuDto
            {
                Id = x.Id,
                Ad = x.Ad,
                XeberdarliqGun = x.XeberdarliqGun,
                Aktivdir = x.Aktivdir,
                Sira = x.Sira
            })
            .ToListAsync();
    }

    private async Task<Result?> NovYoxlaAsync(MasinMuddetNovuDto dto)
    {
        var ad = (dto.Ad ?? "").Trim();
        if (ad.Length == 0) return Result.Fail("Növün adı mütləqdir.");
        if (dto.XeberdarliqGun is < 0 or > 365)
            return Result.Fail("Xəbərdarlıq 0–365 gün aralığında olmalıdır.");

        var varmi = await _uow.Repository<MasinMuddetNovu>().Query().AsNoTracking()
            .AnyAsync(x => x.Id != dto.Id && x.Ad == ad);

        return varmi ? Result.Fail($"«{ad}» adlı növ artıq mövcuddur.") : null;
    }

    public async Task<Result<int>> NovYaratAsync(MasinMuddetNovuDto dto, int userId)
    {
        var xeta = await NovYoxlaAsync(dto);
        if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");

        var e = new MasinMuddetNovu
        {
            Ad = dto.Ad.Trim(),
            XeberdarliqGun = dto.XeberdarliqGun,
            Aktivdir = dto.Aktivdir,
            Sira = dto.Sira,
            YaradanIcraciId = userId
        };

        await _uow.Repository<MasinMuddetNovu>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result<int>.Ok(e.Id, $"«{e.Ad}» növü əlavə edildi.");
    }

    public async Task<Result> NovYenileAsync(MasinMuddetNovuDto dto, int userId)
    {
        var xeta = await NovYoxlaAsync(dto);
        if (xeta != null) return xeta;

        var e = await _uow.Repository<MasinMuddetNovu>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Növ tapılmadı.");

        e.Ad = dto.Ad.Trim();
        e.XeberdarliqGun = dto.XeberdarliqGun;
        e.Aktivdir = dto.Aktivdir;
        e.Sira = dto.Sira;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuddetNovu>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Növ yeniləndi.");
    }

    public async Task<Result> NovSilAsync(int id, int userId)
    {
        var e = await _uow.Repository<MasinMuddetNovu>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Növ tapılmadı.");

        // İşlənən növ silinmir — silinsə mövcud qeydlərin adı boş görünərdi.
        // Əvəzinə «Aktivdir = false» təklif olunur: köhnə sətirlər qalır,
        // yeni qeyd formasında görünmür.
        var islenir = await _uow.Repository<MasinMuddet>().Query().AsNoTracking()
            .AnyAsync(x => x.NovId == id);

        if (islenir)
            return Result.Fail(
                "Bu növ artıq müddət qeydlərində istifadə olunur — silmək əvəzinə «Aktivdir» seçimini götürün.");

        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<MasinMuddetNovu>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Növ silindi.");
    }

    // ══ MÜDDƏT QEYDLƏRİ ═══════════════════════════════════════════════════

    private IQueryable<MasinMuddet> Baza() =>
        _uow.Repository<MasinMuddet>().Query().AsNoTracking()
            .Include(x => x.Masin)
            .Include(x => x.Nov);

    private static MasinMuddetDto Map(MasinMuddet x) => new()
    {
        Id = x.Id,
        MasinId = x.MasinId,
        MasinAdi = x.Masin == null
            ? ""
            : ($"{x.Masin.Marka} {x.Masin.Model}".Trim().Length > 0
                ? $"{x.Masin.Marka} {x.Masin.Model}".Trim()
                : x.Masin.DovletNomresi),
        MasinNomresi = x.Masin?.DovletNomresi ?? "",
        NovId = x.NovId,
        NovAdi = x.Nov?.Ad ?? "",
        SonTarix = x.SonTarix,
        XeberdarliqGun = x.XeberdarliqGun,
        Mebleg = x.Mebleg,
        SenedFaylYolu = x.SenedFaylYolu,
        SenedFaylAdi = x.SenedFaylAdi,
        Qeyd = x.Qeyd,
        Aktivdir = x.Aktivdir,
        XeberdarliqGonderilib = x.XeberdarliqGonderilib
    };

    public async Task<IList<MasinMuddetDto>> HamisiniGetirAsync(int? masinId = null, bool yalnizAktiv = true)
    {
        var sorgu = Baza();
        if (yalnizAktiv) sorgu = sorgu.Where(x => x.Aktivdir);
        if (masinId is > 0) sorgu = sorgu.Where(x => x.MasinId == masinId!.Value);

        var list = await sorgu.OrderBy(x => x.SonTarix).ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<IList<MasinMuddetDto>> YaxinlasanlarAsync(int gun = 30)
    {
        var hedd = DateTime.Today.AddDays(gun);

        // Keçmişlər də daxildir (`SonTarix <= hedd` alt sərhəd qoymur) —
        // vaxtı keçmiş sığorta gizlədilməməlidir, əksinə ən təcili odur.
        var list = await Baza()
            .Where(x => x.Aktivdir && x.SonTarix <= hedd)
            .OrderBy(x => x.SonTarix)
            .ToListAsync();

        return list.Select(Map).ToList();
    }

    public async Task<MasinMuddetDto?> GetirAsync(int id)
    {
        var e = await Baza().FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? null : Map(e);
    }

    private async Task<Result?> YoxlaAsync(MasinMuddetCreateDto dto)
    {
        if (dto.MasinId <= 0) return Result.Fail("Maşın seçilməlidir.");
        if (dto.NovId <= 0) return Result.Fail("Növ seçilməlidir.");
        if (dto.SonTarix == default) return Result.Fail("Son tarix seçilməlidir.");
        if (dto.XeberdarliqGun is < 0 or > 365)
            return Result.Fail("Xəbərdarlıq 0–365 gün aralığında olmalıdır.");
        if (dto.Mebleg is < 0) return Result.Fail("Məbləğ mənfi ola bilməz.");

        var masinVar = await _uow.Repository<Masin>().Query().AsNoTracking()
            .AnyAsync(x => x.Id == dto.MasinId);
        if (!masinVar) return Result.Fail("Maşın tapılmadı.");

        var novVar = await _uow.Repository<MasinMuddetNovu>().Query().AsNoTracking()
            .AnyAsync(x => x.Id == dto.NovId);
        if (!novVar) return Result.Fail("Növ tapılmadı.");

        return null;
    }

    public async Task<Result<int>> YaratAsync(MasinMuddetCreateDto dto, int userId)
    {
        var xeta = await YoxlaAsync(dto);
        if (xeta != null) return Result<int>.Fail(xeta.Message ?? "Məlumat yanlışdır.");

        // Eyni maşın + eyni növ üçün İKİ aktiv sətir ola bilməz — olsaydı
        // xəbərdarlıq iki dəfə gedər, «bu maşının sığortası nə vaxt bitir»
        // sualının iki cavabı olardı. Köhnəsi avtomatik passivləşdirilir.
        var kohneler = await _uow.Repository<MasinMuddet>()
            .HamisiniGetirAsync(x => x.MasinId == dto.MasinId && x.NovId == dto.NovId && x.Aktivdir && !x.Silinib);

        foreach (var k in kohneler)
        {
            k.Aktivdir = false;
            k.YenileyenIcraciId = userId;
            k.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<MasinMuddet>().YenileAsync(k);
        }

        var e = new MasinMuddet
        {
            MasinId = dto.MasinId,
            NovId = dto.NovId,
            SonTarix = dto.SonTarix.Date,
            XeberdarliqGun = dto.XeberdarliqGun,
            Mebleg = dto.Mebleg,
            SenedFaylYolu = dto.SenedFaylYolu,
            SenedFaylAdi = dto.SenedFaylAdi,
            Qeyd = dto.Qeyd?.Trim(),
            Aktivdir = true,
            XeberdarliqGonderilib = false,
            YaradanIcraciId = userId
        };

        await _uow.Repository<MasinMuddet>().YaratAsync(e);
        await _uow.YaddaSaxlaAsync();

        var mesaj = kohneler.Count > 0
            ? $"Müddət yeniləndi — köhnə qeyd tarixçəyə keçdi. Yeni son tarix: {e.SonTarix:dd.MM.yyyy}."
            : $"Müddət əlavə edildi — son tarix {e.SonTarix:dd.MM.yyyy}.";

        return Result<int>.Ok(e.Id, mesaj);
    }

    public async Task<Result> YenileAsync(MasinMuddetCreateDto dto, int userId)
    {
        var xeta = await YoxlaAsync(dto);
        if (xeta != null) return xeta;

        var e = await _uow.Repository<MasinMuddet>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Müddət qeydi tapılmadı.");

        // Son tarix dəyişirsə xəbərdarlıq bayrağı SIFIRLANIR — yoxsa uzadılmış
        // müddət üçün yeni xəbərdarlıq heç vaxt getməzdi (bayraq true qalardı).
        if (e.SonTarix.Date != dto.SonTarix.Date)
        {
            e.XeberdarliqGonderilib = false;
            e.XeberdarliqTarixi = null;
        }

        e.MasinId = dto.MasinId;
        e.NovId = dto.NovId;
        e.SonTarix = dto.SonTarix.Date;
        e.XeberdarliqGun = dto.XeberdarliqGun;
        e.Mebleg = dto.Mebleg;
        e.Qeyd = dto.Qeyd?.Trim();

        // Fayl yalnız YENİSİ yüklənəndə üstələnir. Şərtsiz yazsaq, fayl
        // seçilməyən redaktə mövcud sənədi səssizcə silərdi
        // (CLAUDE.md — şərtli sahə + default parametr = səssiz data itkisi).
        if (!string.IsNullOrWhiteSpace(dto.SenedFaylYolu))
        {
            e.SenedFaylYolu = dto.SenedFaylYolu;
            e.SenedFaylAdi = dto.SenedFaylAdi;
        }

        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<MasinMuddet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Müddət qeydi yeniləndi.");
    }

    public async Task<Result<int>> UzatAsync(
        int kohneId, DateTime yeniSonTarix, decimal? mebleg, string? qeyd, int userId)
    {
        var kohne = await _uow.Repository<MasinMuddet>().GetirAsync(x => x.Id == kohneId && !x.Silinib);
        if (kohne == null) return Result<int>.Fail("Müddət qeydi tapılmadı.");

        if (yeniSonTarix.Date <= kohne.SonTarix.Date)
            return Result<int>.Fail(
                $"Yeni son tarix köhnəsindən ({kohne.SonTarix:dd.MM.yyyy}) sonra olmalıdır.");

        // YaratAsync köhnəni özü passivləşdirir — burada təkrar etmirik ki,
        // iki yerdə eyni qayda qalmasın.
        return await YaratAsync(new MasinMuddetCreateDto
        {
            MasinId = kohne.MasinId,
            NovId = kohne.NovId,
            SonTarix = yeniSonTarix,
            XeberdarliqGun = kohne.XeberdarliqGun,
            Mebleg = mebleg,
            Qeyd = qeyd
        }, userId);
    }

    public async Task<Result> SilAsync(int id, int userId)
    {
        var e = await _uow.Repository<MasinMuddet>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Müddət qeydi tapılmadı.");

        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<MasinMuddet>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Müddət qeydi silindi.");
    }

    // ══ XƏBƏRDARLIQ ALICILARI ═════════════════════════════════════════════

    public async Task<IList<AvtoparkAliciDto>> AlicilarAsync(bool yalnizAktiv = false)
    {
        var sorgu = _uow.Repository<AvtoparkXeberdarliqAlicisi>().Query().AsNoTracking()
            .Include(x => x.Isci);

        var list = yalnizAktiv
            ? await sorgu.Where(x => x.Aktivdir).ToListAsync()
            : await sorgu.ToListAsync();

        // Çıxmış işçi alıcı siyahısında qalmamalıdır — bildiriş ona getsə
        // heç kim oxumaz və «xəbər verildi» sayılardı.
        list = list.Where(x => x.Isci != null && x.Isci.Status == IsciStatus.Aktiv && !x.Isci.Silinib).ToList();

        var sobeler = await SobeAdlariAsync(list.Select(x => x.IsciId));

        return list
            .Select(x => new AvtoparkAliciDto
            {
                Id = x.Id,
                IsciId = x.IsciId,
                IsciAdSoyad = $"{x.Isci.Ad} {x.Isci.Soyad}".Trim(),
                SobeAdi = sobeler.TryGetValue(x.IsciId, out var s) ? s : null,
                Aktivdir = x.Aktivdir
            })
            // İşçi siyahıları üçün layihə qaydası: Sira → Ad → Soyad.
            .OrderBy(x => x.IsciAdSoyad)
            .ToList();
    }

    private async Task<Dictionary<int, string>> SobeAdlariAsync(IEnumerable<int> isciIdler)
    {
        var idler = isciIdler.Distinct().ToList();
        if (idler.Count == 0) return new Dictionary<int, string>();

        var teyinatlar = await _uow.Repository<IsciTeyinat>().Query().AsNoTracking()
            .Include(t => t.Departament)
            .Where(t => idler.Contains(t.IsciId) && t.Aktivdir && !t.Silinib)
            .ToListAsync();

        return teyinatlar
            .GroupBy(t => t.IsciId)
            .ToDictionary(
                g => g.Key,
                g => (g.FirstOrDefault(t => t.Esasdir) ?? g.First()).Departament?.Ad ?? "");
    }

    public async Task<Result> AliciElaveEtAsync(int isciId, int userId)
    {
        if (isciId <= 0) return Result.Fail("İşçi seçilməlidir.");

        var isci = await _uow.Repository<Isci>().GetirAsync(x => x.Id == isciId && !x.Silinib, izlemeden: true);
        if (isci == null) return Result.Fail("İşçi tapılmadı.");

        var movcud = await _uow.Repository<AvtoparkXeberdarliqAlicisi>()
            .GetirAsync(x => x.IsciId == isciId && !x.Silinib);

        // Əvvəl əlavə edilib sonra passivləşdirilibsə — yenidən aktivləşdiririk,
        // ikinci sətir yaratmırıq (yoxsa bildiriş dublikat gedərdi).
        if (movcud != null)
        {
            if (movcud.Aktivdir)
                return Result.Fail($"{isci.Ad} {isci.Soyad} artıq siyahıdadır.");

            movcud.Aktivdir = true;
            movcud.YenileyenIcraciId = userId;
            movcud.YenilenmeTarixi = DateTime.Now;
            await _uow.Repository<AvtoparkXeberdarliqAlicisi>().YenileAsync(movcud);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok($"{isci.Ad} {isci.Soyad} yenidən aktivləşdirildi.");
        }

        await _uow.Repository<AvtoparkXeberdarliqAlicisi>().YaratAsync(new AvtoparkXeberdarliqAlicisi
        {
            IsciId = isciId,
            Aktivdir = true,
            YaradanIcraciId = userId
        });
        await _uow.YaddaSaxlaAsync();

        return Result.Ok($"{isci.Ad} {isci.Soyad} xəbərdarlıq siyahısına əlavə edildi.");
    }

    public async Task<Result> AliciSilAsync(int id, int userId)
    {
        var e = await _uow.Repository<AvtoparkXeberdarliqAlicisi>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Qeyd tapılmadı.");

        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<AvtoparkXeberdarliqAlicisi>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Siyahıdan çıxarıldı.");
    }

    public async Task<Result> AliciAktivlikDeyisAsync(int id, bool aktivdir, int userId)
    {
        var e = await _uow.Repository<AvtoparkXeberdarliqAlicisi>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Qeyd tapılmadı.");

        e.Aktivdir = aktivdir;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi = DateTime.Now;

        await _uow.Repository<AvtoparkXeberdarliqAlicisi>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok(aktivdir ? "Aktivləşdirildi." : "Deaktiv edildi.");
    }
}
