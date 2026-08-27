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


    // Sıralama üçün — formatdan asılı olmayan, sadə oxunuş.
    // Yalnız GÖSTƏRMƏ sırasına təsir edir, nömrə vermir.
    private static int SonReqem(string? hevNom)
    {
        if (string.IsNullOrWhiteSpace(hevNom)) return 0;
        var son = hevNom.Trim().Split('-').LastOrDefault();
        return int.TryParse(son, out var n) ? n : 0;
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

    // GedenHevale sütun ölçüləri AppDbContext-dədir (SAA 50, CONTRAC_NOM 50…).
    // Əsasən BMI-dən gəlir; «Müqavilə №»/«Bəyannamə №» 27.08.2026-da 15 → 50 edildi.
    // Limitdən uzun dəyər SQL-in özündə «String or binary data would be truncated»
    // ilə BÜTÜN yazını sındırır və istifadəçi yalnız ümumi xəta səhifəsi görür —
    // real hadisə 27.08.2026 (VM): «Müqavilə №» xanasına 15-dən uzun mətn yazıldı,
    // CONTRAC_NOM nvarchar(15) qəbul etmədi, ekranda səbəbsiz «xəta baş verdi» çıxdı.
    // Limitlər AppDbContext-dəki HasMaxLength ilə EYNİ olmalıdır — sxem dəyişəndə
    // buranı da yenilə. View-lardakı maxlength atributları birinci qat qoruyucudur;
    // bu yoxlama isə server tərəfdə son sözdür (köhnə brauzer / əl ilə POST halı).
    private static string? UzunluqXetasi(GedenHevaleEditDto d) => UzunluqXetasi(
        d.Saa, d.HesNom, d.TipRes, d.ValTip, d.MenOlke, d.Olke,
        d.AlBank, d.HevTip, d.GonTip, d.ContracNom, d.DeclarNom);

    private static string? UzunluqXetasi(GedenHevaleCreateDto d) => UzunluqXetasi(
        d.Saa, d.HesNom, d.TipRes, d.ValTip, d.MenOlke, d.Olke,
        d.AlBank, d.HevTip, d.GonTip, d.ContracNom, d.DeclarNom);

    private static string? UzunluqXetasi(
        string? saa, string? hesNom, string? tipRes, string? valTip,
        string? menOlke, string? olke, string? alBank, string? hevTip,
        string? gonTip, string? contracNom, string? declarNom)
    {
        var limitler = new (string? Deyer, int Limit, string Ad)[]
        {
            (saa,        50, "Soyad Ad Ata (S.A.A.)"),
            (hesNom,     20, "Hesab №"),
            (tipRes,     16, "Rezident tipi"),
            (valTip,     10, "Valyuta"),
            (menOlke,    40, "Mənşə ölkə"),
            (olke,       40, "Təyinat ölkə"),
            (alBank,     40, "Alan bank"),
            (hevTip,    254, "Həvalə tipi"),
            (gonTip,     20, "Göndərən tipi"),
            (contracNom, 50, "Müqavilə №"),   // 27.08.2026: BMI-dəki 15-dən genişləndirildi
            (declarNom,  50, "Bəyannamə №"),  // 27.08.2026: BMI-dəki 15-dən genişləndirildi
        };
        foreach (var (deyer, limit, ad) in limitler)
        {
            var uzunluq = deyer?.Trim().Length ?? 0;
            if (uzunluq > limit)
                return $"«{ad}» maksimum {limit} simvol ola bilər (daxil edilən: {uzunluq}). Qeyd yazılmadı.";
        }
        return null;
    }

    public async Task<Result<string>> YaratAsync(GedenHevaleCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Saa))
            return Result<string>.Fail("Soyad Ad Ata (S.A.A.) boş ola bilməz.");

        // Uzunluq yoxlaması HƏR ŞEYDƏN ƏVVƏL — nömrə hesablamasından da qabaq
        // (uğursuz ola biləcək hər şey nömrədən əvvəl yoxlanmalıdır — CLAUDE.md).
        if (UzunluqXetasi(dto) is string uXeta)
            return Result<string>.Fail(uXeta);

        // İcraçı nömrəsi — cari istifadəçinin işçisindən (Isci.AppUserId → IcraciNo)
        var isci = (await _uow.Repository<FinNex.Domain.Entities.HR.Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        short? icraNo = (isci?.IcraciNo is int no && no > 0 && no <= short.MaxValue) ? (short)no : (short?)null;

        var tarix = dto.Tarix ?? DateTime.Now;
        var il = tarix.Year;

        // İl üzrə növbəti Həvalə № — BMI formatında: {YY}-T-{N}
        //
        // 18.08.2026: hesablama `HevaleNomreHelper`-ə köçürüldü. Səbəb — Əməliyyat
        // modulundakı «Pul köçürməsi» (Kocurme) EYNİ jurnala eyni formatda nömrə
        // verir, amma bu servisi görmürdü: Gedən həvalə 24-ü, Pul köçürməsi isə
        // 1-i təklif edirdi. İndi hər ikisi HƏR İKİ cədvələ baxır.
        // Silinmişlərin sayılması (QueryAll) və dublikat qoruyucusu helper-dədir.
        var hevNom = await HevaleNomreHelper.NovbetiAsync(
            _uow, il, HevaleNomreHelper.PulPrefiksi);

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

        if (UzunluqXetasi(dto) is string uXeta)
            return Result.Fail(uXeta);

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

        // Pul köçürməsindən yaranan sətir burada silinə bilməz — mənbə köçürmədir.
        // Burada silinsə köçürmə qeydi sahibsiz qalar və jurnalda izi itərdi;
        // köçürmə silinəndə bu sətir onsuz da avtomatik silinir (KocurmeService.SilAsync).
        // REDAKTƏ isə açıqdır: jurnalın yalnız köçürmədən gələn 5 sahəsi üstələnir,
        // əl ilə doldurulan sahələr (Ölkə, Hesab №, rezident tipi…) qalır.
        if (e.KocurmeId != null)
            return Result.Fail(
                "Bu həvalə «Pul köçürməsi»ndən yaranıb — Əməliyyat → Pul köçürməsi " +
                "səhifəsindən silinməlidir.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<GedenHevale>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Həvalə silindi.");
    }
}
