using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Yardim;
using FinNex.Application.Helpers.Yardim;
using FinNex.Application.Interfaces.Yardim;
using FinNex.Domain.Entities.Yardim;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Yardim;

/// <summary>
/// Səhifə təlimatları servisi (27.08.2026).
/// Mətn bazadadır — admin onu deploy gözləmədən redaktə edir.
/// </summary>
public class SehifeYardimiService : ISehifeYardimiService
{
    private readonly IUnitOfWork _uow;

    public SehifeYardimiService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Açar həmişə eyni qaydada normallaşdırılır — böyük/kiçik hərf fərqi
    // yardımı «tapılmadı» edərdi və heç bir xəta çıxmazdı.
    private static string Norm(string? acar)
        => string.IsNullOrWhiteSpace(acar) ? "" : acar.Trim().ToLowerInvariant();

    public async Task<YardimPanelDto> PanelAsync(string acar, bool adminmi)
    {
        var a = Norm(acar);
        var e = await _uow.Repository<SehifeYardimi>()
            .GetirAsync(x => x.Acar == a && !x.Silinib, izlemeden: true);

        // Qeyd yoxdursa İSTİSNA ATMIRIQ — panel «hələ yazılmayıb» göstərsin.
        // Səhifənin özü yardım üzündən sınmamalıdır.
        if (e == null)
            return new YardimPanelDto { Var = false, Acar = a };

        // Yalnız admin üçün işarələnmiş qeyd adi istifadəçiyə YOX kimi görünür —
        // «var, amma göstərmirəm» sualı yaratmasın.
        if (e.YalnizAdmin && !adminmi)
            return new YardimPanelDto { Var = false, Acar = a };

        return new YardimPanelDto
        {
            Id         = e.Id,
            Basliq     = e.Basliq,
            Modul      = e.Modul,
            Xulase     = e.Xulase,
            // «Hazırlanır» rejimində yarımçıq mətn göstərmirik.
            // Formatla(): admin SADƏ MƏTN yazır, HTML burada — GÖSTƏRMƏ anında —
            // qurulur. Bazadakı mətn toxunulmur, yoxsa redaktəyə girəndə admin
            // öz yazdığını yox, maşının HTML-ini görərdi.
            Metn       = e.Hazirlanir ? null : YardimMetn.Formatla(e.Metn),
            Slug       = e.Slug,
            Hazirlanir = e.Hazirlanir,
            Var        = true,
            Acar       = e.Acar,
            Yenilenme  = e.YenilenmeTarixi ?? e.YaradilmaTarixi
        };
    }

    public async Task<YardimPanelDto?> SlugaGoreAsync(string slug, bool adminmi)
    {
        var s = Norm(slug);
        var e = await _uow.Repository<SehifeYardimi>()
            .GetirAsync(x => x.Slug == s && !x.Silinib, izlemeden: true);
        if (e == null || (e.YalnizAdmin && !adminmi)) return null;

        return new YardimPanelDto
        {
            Id = e.Id, Basliq = e.Basliq, Modul = e.Modul, Xulase = e.Xulase,
            Metn = e.Hazirlanir ? null : YardimMetn.Formatla(e.Metn), Slug = e.Slug,
            Hazirlanir = e.Hazirlanir, Var = true, Acar = e.Acar,
            Yenilenme = e.YenilenmeTarixi ?? e.YaradilmaTarixi
        };
    }

    public async Task<IReadOnlyList<YardimListDto>> SiyahiAsync(string? axtaris, bool adminmi)
    {
        var q = _uow.Repository<SehifeYardimi>().Query().AsNoTracking();
        if (!adminmi) q = q.Where(x => !x.YalnizAdmin);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var t = axtaris.Trim();
            q = q.Where(x => x.Basliq.Contains(t)
                          || (x.Modul  != null && x.Modul.Contains(t))
                          || (x.Xulase != null && x.Xulase.Contains(t)));
        }

        return await q
            .OrderBy(x => x.Modul).ThenBy(x => x.Basliq)
            .Select(x => new YardimListDto
            {
                Id = x.Id, Basliq = x.Basliq, Modul = x.Modul, Xulase = x.Xulase,
                Slug = x.Slug, Acar = x.Acar, Hazirlanir = x.Hazirlanir,
                BaxisSayi = x.BaxisSayi,
                Yenilenme = x.YenilenmeTarixi ?? x.YaradilmaTarixi
            })
            .ToListAsync();
    }

    public async Task<YardimUpsertDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<SehifeYardimi>()
            .GetirAsync(x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new YardimUpsertDto
        {
            Id = e.Id, Acar = e.Acar, Slug = e.Slug, Basliq = e.Basliq,
            Modul = e.Modul, Xulase = e.Xulase, Metn = e.Metn,
            Hazirlanir = e.Hazirlanir, YalnizAdmin = e.YalnizAdmin
        };
    }

    public Task<YardimUpsertDto> YeniMelumatAsync(string acar)
        => Task.FromResult(new YardimUpsertDto { Acar = Norm(acar) });

    public async Task<Result> YaddaSaxlaAsync(YardimUpsertDto dto, int userId)
    {
        var acar = Norm(dto.Acar);
        if (string.IsNullOrWhiteSpace(acar))
            return Result.Fail("Səhifə açarı boş ola bilməz.");
        if (string.IsNullOrWhiteSpace(dto.Basliq))
            return Result.Fail("Başlıq boş ola bilməz.");

        // Slug boş gələrsə başlıqdan qurulur; yenə boş çıxarsa açardan.
        var slug = Norm(dto.Slug);
        if (string.IsNullOrWhiteSpace(slug)) slug = YardimAcar.Slugla(dto.Basliq);
        if (string.IsNullOrWhiteSpace(slug)) slug = acar.Replace('/', '-');

        var repo = _uow.Repository<SehifeYardimi>();

        // Unikallıq yoxlaması BAZADAN ƏVVƏL — unikal indeks onsuz da tutur, amma
        // SQL istisnası istifadəçiyə anlaşılmaz mətn göstərər. Silinmişlər də
        // sayılır (QueryAll), çünki indeks onları da əhatə edir.
        var acarTutan = await repo.QueryAll()
            .FirstOrDefaultAsync(x => x.Acar == acar && x.Id != dto.Id);
        if (acarTutan != null)
            return Result.Fail($"«{acar}» açarı üçün təlimat artıq mövcuddur — onu redaktə edin.");

        var slugTutan = await repo.QueryAll()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Id != dto.Id);
        if (slugTutan != null)
            return Result.Fail($"«{slug}» qısa ünvanı başqa təlimatda işlənir — başqa ad seçin.");

        if (dto.Id > 0)
        {
            var e = await repo.GetirAsync(x => x.Id == dto.Id && !x.Silinib);
            if (e == null) return Result.Fail("Təlimat tapılmadı.");

            e.Acar = acar; e.Slug = slug;
            e.Basliq = dto.Basliq!.Trim();
            e.Modul  = dto.Modul?.Trim();
            e.Xulase = dto.Xulase?.Trim();
            e.Metn   = dto.Metn ?? "";
            e.Hazirlanir  = dto.Hazirlanir;
            e.YalnizAdmin = dto.YalnizAdmin;
            e.YenileyenIcraciId = userId;
            e.YenilenmeTarixi   = DateTime.Now;

            await repo.YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
            return Result.Ok("Təlimat yeniləndi.");
        }

        await repo.YaratAsync(new SehifeYardimi
        {
            Acar = acar, Slug = slug,
            Basliq = dto.Basliq!.Trim(),
            Modul  = dto.Modul?.Trim(),
            Xulase = dto.Xulase?.Trim(),
            Metn   = dto.Metn ?? "",
            Hazirlanir  = dto.Hazirlanir,
            YalnizAdmin = dto.YalnizAdmin,
            YaradanIcraciId = userId
        });
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Təlimat yaradıldı.");
    }

    public async Task<Result> SilAsync(int id, int userId)
    {
        var repo = _uow.Repository<SehifeYardimi>();
        var e = await repo.GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Təlimat tapılmadı.");

        e.Silinib = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;
        // Açar/slug unikal indeksdə qalır — silinmiş qeyd yenisinin qarşısını
        // almasın deyə onları boşaldırıq (indeks silinmişləri də əhatə edir).
        e.Acar = $"silinmis:{e.Id}";
        e.Slug = $"silinmis-{e.Id}";

        await repo.YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Təlimat silindi.");
    }

    public async Task BaxisArtirAsync(string acar)
    {
        try
        {
            var a = Norm(acar);
            var repo = _uow.Repository<SehifeYardimi>();
            var e = await repo.GetirAsync(x => x.Acar == a && !x.Silinib);
            if (e == null) return;
            e.BaxisSayi += 1;
            await repo.YenileAsync(e);
            await _uow.YaddaSaxlaAsync();
        }
        catch
        {
            // Sayğac statistikadır — yazıla bilməsə panel yenə açılmalıdır.
            // Boş catch qəsdəndir və YALNIZ buradadır (əsas axına təsiri yoxdur).
        }
    }

    public async Task<IReadOnlyList<YardimEhateDto>> EhateAsync(IEnumerable<string> acarlar)
    {
        var siyahi = acarlar.Select(Norm).Where(x => x.Length > 0).Distinct().ToList();

        var movcud = await _uow.Repository<SehifeYardimi>().Query().AsNoTracking()
            .Where(x => siyahi.Contains(x.Acar))
            .Select(x => new { x.Acar, x.Basliq, x.Hazirlanir })
            .ToListAsync();
        var map = movcud.ToDictionary(x => x.Acar, x => x);

        return siyahi.Select(a =>
        {
            var hisse = a.Split('/');
            map.TryGetValue(a, out var m);
            return new YardimEhateDto
            {
                Acar       = a,
                Sahe       = hisse.Length > 0 ? hisse[0] : null,
                Kontroller = hisse.Length > 1 ? hisse[1] : null,
                Emel       = hisse.Length > 2 ? hisse[2] : null,
                Yazilib    = m != null,
                Hazirlanir = m?.Hazirlanir ?? false,
                Basliq     = m?.Basliq
            };
        })
        .OrderBy(x => x.Yazilib).ThenBy(x => x.Sahe).ThenBy(x => x.Kontroller)
        .ToList();
    }
}
