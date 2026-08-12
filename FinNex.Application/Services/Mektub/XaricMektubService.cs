using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Mektub;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Mektub;

public class XaricMektubService : IXaricMektubService
{
    private readonly IUnitOfWork _uow;

    public XaricMektubService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // QEY_NOM formatı BMI-dən gəlir: "İL-NÖMRƏ" (məs. "2026-651"), uzunluğu max 9.
    // Sadə `int.TryParse("2026-651")` UĞURSUZ olur və 0 qaytarır — nəticədə il üzrə
    // max+1 hesabı sıfırdan başlayır və mövcud nömrələri təkrarlayır (real hadisə:
    // BMI 2026-651-də ikən FinNex "№ 1/2026" verdi). Ona görə tiredən sonrakı hissə
    // oxunur; tire yoxdursa (köhnə/sadə dəyər) bütün sətir rəqəm kimi sınanır.
    private static int ParseNum(string? s)
    {
        var t = s?.Trim();
        if (string.IsNullOrEmpty(t)) return 0;

        var tire = t.LastIndexOf('-');
        if (tire >= 0 && tire < t.Length - 1)
            t = t[(tire + 1)..];

        return int.TryParse(t, out var n) ? n : 0;
    }

    // Yeni qeydin Qeydiyyat №-si BMI ilə EYNİ formatda yazılır ki, köhnə və yeni
    // sətirlər eyni jurnalda düzgün oxunsun/sıralansın.
    private static string QeyNomYarat(int il, int nomre) =>
        $"{il}-{nomre.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    // Jurnalda mövcud illər və icraçı nömrələri (filtr açılan siyahıları üçün).
    public async Task<MektubFiltrMenbeDto> FiltrMenbeleriAsync()
    {
        var hamisi = await _uow.Repository<XaricMektub>()
            .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true);

        var adMap = await IcraciAdXeritesiAsync();

        return new MektubFiltrMenbeDto
        {
            Iller = hamisi.Where(x => x.Il.HasValue).Select(x => x.Il!.Value)
                          .Distinct().OrderByDescending(x => x).ToList(),

            // Yalnız REAL işlənən nömrələr — HR-ın hansı kodları təyin etməli olduğu da buradan görünür
            Icracilar = hamisi
                .Select(x => IcraciNoOxu(x.Icraci))
                .Where(n => n.HasValue)
                .GroupBy(n => n!.Value)
                .Select(g => new MektubIcraciDto
                {
                    No  = g.Key,
                    Ad  = adMap.TryGetValue(g.Key, out var ad) ? ad : null,
                    Say = g.Count()
                })
                .OrderBy(x => x.No)
                .ToList()
        };
    }

    // Oracle-dan gələn və FinNex-də yazılan dəyər rəqəmdir; parse olunmayan köhnə
    // (adla yazılmış) qeydlərdə null qayıdır.
    private static int? IcraciNoOxu(string? xam) =>
        int.TryParse(xam?.Trim(), out var n) ? n : (int?)null;

    private async Task<Dictionary<int, string>> IcraciAdXeritesiAsync()
    {
        var isciler = await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.IcraciNo != null, izlemeden: true);
        return isciler.Where(i => i.IcraciNo.HasValue)
            .GroupBy(i => i.IcraciNo!.Value)
            .ToDictionary(g => g.Key, g => g.First().TamAd);
    }

    public async Task<MektubSehifeDto<XaricMektubListDto>> HamisiniGetirAsync(MektubFiltrDto? filtr = null)
    {
        var f  = MektubFiltrDto.Normalla(filtr);
        var il = f.SorguIli;   // "bütün illər" seçilibsə null

        // İl və tarix DB tərəfində süzülür (32 min sətir yaddaşa çəkilməsin);
        // icraçı və mətn axtarışı sətir əməliyyatı olduğu üçün yaddaşda.
        var list = await _uow.Repository<XaricMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib
                         && (il == null || x.Il == il)
                         && (f.TarixFrom == null || (x.Tarix != null && x.Tarix >= f.TarixFrom))
                         && (f.TarixTo   == null || (x.Tarix != null && x.Tarix <= f.TarixTo)),
            izlemeden: true);

        if (f.IcraciNo.HasValue)
            list = list.Where(x => IcraciNoOxu(x.Icraci) == f.IcraciNo.Value).ToList();

        if (!string.IsNullOrWhiteSpace(f.Axtaris))
        {
            var q = f.Axtaris.Trim();
            list = list.Where(x =>
                    (x.GonYer  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.QisaMez ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.QeyNom  ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // İcraçı nömrəsi → işçi adı (Isci.IcraciNo) — Daxil məktub/Həvalə ilə eyni qayda.
        var adMap = await IcraciAdXeritesiAsync();

        // Səhifələmə süzgəclərdən SONRA — "Cəmi" filtrə uyğun sətir sayını göstərməlidir.
        var cemi = list.Count;
        var sehifeSetirleri = list
            .OrderByDescending(x => x.Il).ThenByDescending(x => ParseNum(x.QeyNom))
            .Skip((f.Sehife - 1) * f.SehifeOlcusu)
            .Take(f.SehifeOlcusu)
            .ToList();

        return new MektubSehifeDto<XaricMektubListDto>
        {
            CemiSay      = cemi,
            Sehife       = f.Sehife,
            SehifeOlcusu = f.SehifeOlcusu,
            Setirler     = sehifeSetirleri
            .Select(x =>
            {
                // Köhnə Oracle sətirlərində və yeni qeydlərdə dəyər nömrədir; nömrəyə
                // parse olunmayan (keçmiş adla yazılmış) qeydlər xam qalır.
                int? icNo = IcraciNoOxu(x.Icraci);
                return new XaricMektubListDto
                {
                    Id        = x.Id,
                    QeyNom    = x.QeyNom,
                    Tarix     = x.Tarix,
                    GonYer    = x.GonYer,
                    QisaMez   = x.QisaMez,
                    Icraci    = x.Icraci,
                    IcraciNo  = icNo,
                    IcraciAd  = (icNo.HasValue && adMap.TryGetValue(icNo.Value, out var ad)) ? ad : null,
                    Il        = x.Il,
                    YaradanId = x.YaradanIcraciId,
                    FaylYolu  = x.FaylYolu,
                    FaylVar   = !string.IsNullOrEmpty(x.FaylYolu)
                };
            })
            .ToList()
        };
    }

    public async Task<Result<int>> YaratAsync(XaricMektubCreateDto dto, int yaradanUserId, string? faylYolu = null)
    {
        // İCRAÇI — cari istifadəçinin işçisindən NÖMRƏ (Isci.AppUserId → IcraciNo).
        // Oracle `odb.xaric_mektub.ICRACI` sütununda BMI rəqəm saxlayır (68, 25, 48…);
        // əvvəl bura işçinin TAM ADI yazılırdı və eyni sütunda iki fərqli məna yaranırdı
        // (köhnə sətirlər "68", yeni sətirlər "Rafael Quliyev İsrafil"). İndi rəqəm yazılır,
        // ad isə göstərmə anında tapılır — Daxil məktub/Həvalə ilə eyni qayda.
        // İşçiyə hələ nömrə təyin edilməyibsə sahə BOŞ qalır (yalançı dəyər yazılmır);
        // nömrələr HR → İcraçı Nömrələri səhifəsindən verilir.
        var isci = (await _uow.Repository<Isci>().HamisiniGetirAsync(
            predicate: x => x.AppUserId == yaradanUserId && !x.Silinib, izlemeden: true)).FirstOrDefault();
        var icraciNoMetn = (isci?.IcraciNo is int no && no > 0)
            ? no.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;

        var il = dto.Tarix?.Year ?? DateTime.Now.Year;
        var novbeti = await NovbetiNomreAsync(il);

        var entity = new XaricMektub
        {
            QeyNom     = QeyNomYarat(il, novbeti),
            GonYer     = dto.GonYer?.Trim(),
            Tarix      = dto.Tarix,
            QisaMez    = dto.QisaMez?.Trim(),
            Icraci     = icraciNoMetn,
            MektubMetn = string.IsNullOrWhiteSpace(dto.MektubMetn) ? null : dto.MektubMetn.Trim(),
            Il         = il,
            FaylYolu   = string.IsNullOrWhiteSpace(faylYolu) ? null : faylYolu,
            YaradanIcraciId = yaradanUserId
        };

        await _uow.Repository<XaricMektub>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();

        return Result<int>.Ok(novbeti, $"Xaric məktub qeydə alındı — Qeydiyyat № {novbeti}/{il}.");
    }

    // İl üzrə növbəti Qeydiyyat № (yüklənən BMI datası + yeni qeydlərdən max+1).
    // YaratAsync da bunu çağırır — preview ilə real nömrə HƏMİŞƏ eyni düsturdan gəlir.
    //
    // DİQQƏT: nömrə FinNex bazasındakı sətirlərdən hesablanır. Həmin ilin BMI datası
    // hələ idxal edilməyibsə nömrə 1-dən başlayar və köhnə nömrələrlə toqquşar —
    // ona görə jurnaldan nömrə verməzdən əvvəl həmin il idxal edilməlidir
    // (SenedDovriyyesi → Məktublar → BMI-dən köçürmə).
    public async Task<int> NovbetiNomreAsync(int il)
    {
        var heminIl = await _uow.Repository<XaricMektub>().HamisiniGetirAsync(
            predicate: x => !x.Silinib && x.Il == il, izlemeden: true);

        return heminIl
            .Select(x => ParseNum(x.QeyNom))
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    public async Task<XaricMektubEditDto?> RedakteMelumatiAsync(int id)
    {
        var e = await _uow.Repository<XaricMektub>().GetirAsync(
            x => x.Id == id && !x.Silinib, izlemeden: true);
        if (e == null) return null;

        return new XaricMektubEditDto
        {
            Id             = e.Id,
            Tarix          = e.Tarix,
            GonYer         = e.GonYer,
            QisaMez        = e.QisaMez,
            MektubMetn     = e.MektubMetn,
            QeyNom         = e.QeyNom,
            Il             = e.Il,
            MovcudFaylYolu = e.FaylYolu,
            YaradanId      = e.YaradanIcraciId
        };
    }

    public async Task<Result> YenileAsync(XaricMektubEditDto dto, int userId, bool isAdmin, string? yeniFaylYolu = null)
    {
        if (string.IsNullOrWhiteSpace(dto.GonYer))
            return Result.Fail("Göndərilən yer (təyinat) boş ola bilməz.");

        var e = await _uow.Repository<XaricMektub>().GetirAsync(x => x.Id == dto.Id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin dəyişə bilər.");

        // Qeydiyyat № (QeyNom) və İl dəyişməz qalır — jurnal nömrəsi yaradılışda təyin olunur
        e.Tarix      = dto.Tarix;
        e.GonYer     = dto.GonYer?.Trim();
        e.QisaMez    = dto.QisaMez?.Trim();
        e.MektubMetn = string.IsNullOrWhiteSpace(dto.MektubMetn) ? null : dto.MektubMetn.Trim();
        if (!string.IsNullOrWhiteSpace(yeniFaylYolu))
            e.FaylYolu = yeniFaylYolu;
        e.YenileyenIcraciId = userId;
        e.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<XaricMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub yeniləndi.");
    }

    public async Task<Result> SilAsync(int id, int userId, bool isAdmin)
    {
        var e = await _uow.Repository<XaricMektub>().GetirAsync(x => x.Id == id && !x.Silinib);
        if (e == null) return Result.Fail("Məktub tapılmadı.");
        if (!isAdmin && e.YaradanIcraciId != userId)
            return Result.Fail("Yalnız öz qeydinizi və ya Admin silə bilər.");

        e.Silinib       = true;
        e.SilinmeTarixi = DateTime.Now;
        e.SilenIcraciId = userId;

        await _uow.Repository<XaricMektub>().YenileAsync(e);
        await _uow.YaddaSaxlaAsync();
        return Result.Ok("Məktub silindi.");
    }
}
