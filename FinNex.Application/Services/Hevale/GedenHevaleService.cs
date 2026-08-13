using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Hevale;
using FinNex.Application.Interfaces.Hevale;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Hevale;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Hevale;

public class GedenHevaleService : IGedenHevaleService
{
    private readonly IUnitOfWork _uow;

    public GedenHevaleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ── GEDƏN HƏVALƏ NÖMRƏSİ — BMI formatı: {YY}-T-{N} ────────────────────
    //
    // 13.08.2026 yoxlaması (odb.geden_hevale, 9835 sətir):
    //   9696 sətir  "26-T-9"    → ortada LATIN T (bayt 84)   ← cari qayda
    //    138 sətir  "07-T-698"  → ortada KİRİL Т (208,162)   ← yalnız 2006–2007
    //      1 sətir  "07-T474"   → ikinci tire yoxdur (səhv giriş)
    //
    // Əvvəl kod `{YY}-{N}` yazırdı (məs. "26-24") — BMI ilə uyğunsuz idi.
    // İndi `{YY}-T-{N}` yazılır; T LATINDIR (cari illərdə işlənən variant).

    private const string GedenAyirici = "-T-";

    // Sıralama üçün — formatdan asılı olmayan, sadə oxunuş.
    // Yalnız GÖSTƏRMƏ sırasına təsir edir, nömrə vermir.
    private static int SonReqem(string? hevNom)
    {
        if (string.IsNullOrWhiteSpace(hevNom)) return 0;
        var son = hevNom.Trim().Split('-').LastOrDefault();
        return int.TryParse(son, out var n) ? n : 0;
    }

    // Nömrə VERMƏK üçün — yalnız "{YY}-T-{N}" şablonuna uyğun sətirlər sayılır.
    // Kiril "Т"-li köhnə sətirlər və formatsızlar (07-T474) nəzərə alınmır:
    // onlar 2006–2007-dədir, cari ilin sayğacına qarışmamalıdır.
    // Uyğun gəlməyən sətir üçün null qaytarılır (0 yox) — 0 "sıfırıncı nömrə"
    // kimi başa düşülüb max hesabını yanılda bilərdi.
    private static int? GedenNomre(string? hevNom, int il)
    {
        if (string.IsNullOrWhiteSpace(hevNom)) return null;

        var prefiks = $"{il % 100:D2}{GedenAyirici}";          // məs. "26-T-"
        var t = hevNom.Trim();
        if (!t.StartsWith(prefiks, StringComparison.Ordinal)) return null;

        var quyruq = t[prefiks.Length..];
        return int.TryParse(quyruq, out var n) ? n : null;
    }

    // İcraçı nömrəsi → işçi adı xəritəsi
    private async Task<Dictionary<int, string?>> IcraciAdMapAsync()
    {
        var isciler = await _uow.Repository<FinNex.Domain.Entities.HR.Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    // Jurnalda mövcud illər və icraçı nömrələri (filtr açılan siyahıları üçün).
    // Həvalədə `Il` sütunu yoxdur — il `Tarix`-dən çıxarılır.
    public async Task<HevaleFiltrMenbeDto> FiltrMenbeleriAsync()
    {
        var hamisi = await _uow.Repository<GedenHevale>()
            .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true);

        var adMap = await IcraciAdMapAsync();

        return new HevaleFiltrMenbeDto
        {
            Iller = hamisi.Where(x => x.Tarix.HasValue).Select(x => x.Tarix!.Value.Year)
                          .Distinct().OrderByDescending(x => x).ToList(),

            Icracilar = hamisi
                .Where(x => x.Icra.HasValue)
                .GroupBy(x => x.Icra!.Value)
                .Select(g => new HevaleIcraciDto
                {
                    No  = g.Key,
                    Ad  = adMap.TryGetValue(g.Key, out var ad) ? ad : null,
                    Say = g.Count()
                })
                .OrderBy(x => x.No)
                .ToList()
        };
    }

    public async Task<HevaleSehifeDto<GedenHevaleListDto>> HamisiniGetirAsync(HevaleFiltrDto? filtr = null)
    {
        var f  = HevaleFiltrDto.Normalla(filtr);
        var il = f.SorguIli;   // "bütün illər" seçilibsə null

        // İl, icraçı və tarix DB tərəfində süzülür; yalnız mətn axtarışı yaddaşdadır.
        var list = await _uow.Repository<GedenHevale>().HamisiniGetirAsync(
            predicate: x => !x.Silinib
                         && (il == null || (x.Tarix != null && x.Tarix.Value.Year == il))
                         && (f.IcraciNo == null || x.Icra == f.IcraciNo)
                         && (f.TarixFrom == null || (x.Tarix != null && x.Tarix >= f.TarixFrom))
                         && (f.TarixTo   == null || (x.Tarix != null && x.Tarix <= f.TarixTo)),
            izlemeden: true);

        if (!string.IsNullOrWhiteSpace(f.Axtaris))
        {
            var q = f.Axtaris.Trim();
            list = list.Where(x =>
                    (x.Saa        ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.HevNom     ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.HesNom     ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.AlBank     ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.Olke       ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.ContracNom ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var adMap = await IcraciAdMapAsync();

        // Səhifələmə süzgəclərdən SONRA — "Cəmi" filtrə uyğun sətir sayını göstərməlidir.
        return new HevaleSehifeDto<GedenHevaleListDto>
        {
            CemiSay      = list.Count,
            Sehife       = f.Sehife,
            SehifeOlcusu = f.SehifeOlcusu,
            Setirler     = list
            .OrderByDescending(x => x.Tarix)
            .ThenByDescending(x => SonReqem(x.HevNom))
            .Skip((f.Sehife - 1) * f.SehifeOlcusu)
            .Take(f.SehifeOlcusu)
            .Select(x => new GedenHevaleListDto
            {
                Id        = x.Id,
                HevNom    = x.HevNom,
                Tarix     = x.Tarix,
                Saa       = x.Saa,
                Mebleg    = x.Mebleg,
                ValTip    = x.ValTip,
                Olke      = x.Olke,
                AlBank    = x.AlBank,
                Icra      = x.Icra,
                IcraciAd  = (x.Icra.HasValue && adMap.TryGetValue(x.Icra.Value, out var ad)) ? ad : null,
                YaradanId = x.YaradanIcraciId,
                FaylYolu  = x.FaylYolu,
                FaylVar   = !string.IsNullOrEmpty(x.FaylYolu)
            })
            .ToList()
        };
    }

    public async Task<Result<string>> YaratAsync(GedenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result<string>.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        // İcraçı nömrəsi — cari istifadəçinin işçisindən (Isci.AppUserId → IcraciNo)
        var isci = (await _uow.Repository<FinNex.Domain.Entities.HR.Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        var tarix = dto.Tarix ?? DateTime.Now;
        var il = tarix.Year;

        // İl üzrə növbəti Həvalə № — BMI formatında: {YY}-T-{N}
        //
        // SİLİNMİŞLƏR DƏ SAYILIR (QueryAll) — 13.08.2026. Həvalə nömrəsi bir dəfə
        // veriləndən sonra sənəd artıq o nömrə ilə getib; qeydin silinməsi nömrəni
        // geri qaytarmır. `HamisiniGetirAsync` avtomatik `!Silinib` tətbiq edir
        // (EfRepositoryAsync:25) — onunla ən böyük nömrəli həvalə silinsə həmin nömrə
        // yenidən verilirdi. Aşağıdakı `movcudNomreler` yoxlaması da eyni siyahıdan
        // qurulur, ona görə silinmiş nömrə həm max-a, həm dublikat qoruyucusuna düşür.
        // Eyni prinsipin mövcud nümunəsi: SenedService versiya nömrəsi (SenedFayl).
        var heminIl = await _uow.Repository<GedenHevale>().QueryAll()
            .AsNoTracking()
            .Where(x => x.Tarix != null && x.Tarix.Value.Year == il)
            .ToListAsync();

        var novbeti = heminIl
            .Select(x => GedenNomre(x.HevNom, il))
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        // Təhlükəsizlik: gözlənilməz formatlı sətir səbəbindən hesablanan nömrə
        // artıq mövcuddursa boş nömrəyə qədər irəlilə. Jurnal nömrəsi təkrarlanmamalıdır.
        var movcudNomreler = heminIl
            .Where(x => !string.IsNullOrWhiteSpace(x.HevNom))
            .Select(x => x.HevNom!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string hevNom;
        do
        {
            hevNom = $"{il % 100:D2}{GedenAyirici}{novbeti}";
            novbeti++;
        }
        while (movcudNomreler.Contains(hevNom));

        var entity = new GedenHevale
        {
            HevNom     = hevNom,
            Tarix      = dto.Tarix,
            Saa        = dto.Saa?.Trim(),
            HesNom     = dto.HesNom?.Trim(),
            TipRes     = dto.TipRes?.Trim(),
            Mebleg     = dto.Mebleg,
            ValTip     = dto.ValTip?.Trim(),
            MenOlke    = dto.MenOlke?.Trim(),
            Olke       = dto.Olke?.Trim(),
            AlBank     = dto.AlBank?.Trim(),
            HevTip     = dto.HevTip?.Trim(),
            GonTip     = dto.GonTip?.Trim(),
            ContracNom = dto.ContracNom?.Trim(),
            DeclarNom  = dto.DeclarNom?.Trim(),
            Icra       = icraNo,
            FaylYolu   = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<GedenHevale>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<string>.Ok(hevNom, $"Gedən həvalə qeydə alındı — № {hevNom}.");
    }

    public async Task<GedenHevaleEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<GedenHevale>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new GedenHevaleEditDto
        {
            Id             = e.Id,
            Tarix          = e.Tarix,
            Saa            = e.Saa,
            HesNom         = e.HesNom,
            TipRes         = e.TipRes,
            Mebleg         = e.Mebleg,
            ValTip         = e.ValTip,
            MenOlke        = e.MenOlke,
            Olke           = e.Olke,
            AlBank         = e.AlBank,
            HevTip         = e.HevTip,
            GonTip         = e.GonTip,
            ContracNom     = e.ContracNom,
            DeclarNom      = e.DeclarNom,
            HevNom         = e.HevNom,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(GedenHevaleEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        var e = await _uow.Repository<GedenHevale>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        // Həvalə № (HevNom) dəyişməz qalır
        e.Tarix      = dto.Tarix;
        e.Saa        = dto.Saa?.Trim();
        e.HesNom     = dto.HesNom?.Trim();
        e.TipRes     = dto.TipRes?.Trim();
        e.Mebleg     = dto.Mebleg;
        e.ValTip     = dto.ValTip?.Trim();
        e.MenOlke    = dto.MenOlke?.Trim();
        e.Olke       = dto.Olke?.Trim();
        e.AlBank     = dto.AlBank?.Trim();
        e.HevTip     = dto.HevTip?.Trim();
        e.GonTip     = dto.GonTip?.Trim();
        e.ContracNom = dto.ContracNom?.Trim();
        e.DeclarNom  = dto.DeclarNom?.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<GedenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<GedenHevale>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Həvalə tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<GedenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə silindi.");
    }
}
