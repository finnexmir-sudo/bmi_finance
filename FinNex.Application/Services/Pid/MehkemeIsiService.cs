using FinNex.Application.DTOs.Oracle;
using FinNex.Application.DTOs.Pid;
using FinNex.Application.Interfaces;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Pid;
using FinNex.Application.Interfaces.Sorgular;
using FinNex.Domain.Entities.Pid;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FinNex.Application.Services.Pid;

public class MehkemeIsiService : IMehkemeIsiService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorguService;
    private readonly ISistemAyarService _sistemAyar;

    public MehkemeIsiService(
        IUnitOfWork uow,
        IOracleService oracle,
        IOracleSorguService sorguService,
        ISistemAyarService sistemAyar)
    {
        _uow = uow;
        _oracle = oracle;
        _sorguService = sorguService;
        _sistemAyar = sistemAyar;
    }

    public async Task<IList<MehkemeIsiListDto>> HamisiniGetirAsync()
    {
        var list = await _uow.Repository<MehkemeIsi>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Merheleler)
            .OrderByDescending(x => x.YaradilmaTarixi)
            .ToListAsync();

        return list.Select(x =>
        {
            // yalnız "Məhkəmə iclası" (görüş) tipli mərhələlər iclas sayılır
            var iclaslar = x.Merheleler.Where(m => !m.Silinib && m.MerheleTipi == MerheleTipi.MehkemeIclasi).ToList();
            var bugun = DateTime.Today;
            var novbeti = iclaslar.Where(m => m.Tarix.Date >= bugun).OrderBy(m => m.Tarix).FirstOrDefault()
                          ?? iclaslar.OrderByDescending(m => m.Tarix).FirstOrDefault();
            return new MehkemeIsiListDto
            {
                Id                = x.Id,
                QeydiyyatNomresi  = x.QeydiyyatNomresi,
                BorcluAd          = x.BorcluAd,
                EsasBorc          = x.EsasBorc,
                MehkemeXerci      = x.MehkemeXerci,
                Nov               = x.Nov,
                Status            = x.Status,
                BaslamaTarixi     = x.BaslamaTarixi,
                QetnameTarixi     = x.QetnameTarixi,
                NovbetiIclasTarix = novbeti?.Tarix,
                NovbetiIclasSaat  = novbeti?.Saat,
                Hakim             = x.Hakim,
                Teminat           = x.KreditNovuMetn,
                MerheleCount      = iclaslar.Count,
                YaradilmaTarixi   = x.YaradilmaTarixi
            };
        }).ToList();
    }

    // ── İcra işləri (Excel "İcrada olan işlər") ───────────────────────────
    // status: "aktiv" (default) = İcra; "baglanmis" = Tamamlandı+Bağlandı; "hamisi" = hamısı
    public async Task<IList<IcraIsiListDto>> IcraIsleriGetirAsync(string? status)
    {
        var q = _uow.Repository<MehkemeIsi>().Query().AsNoTracking().Where(x => !x.Silinib);

        if (status == "baglanmis")
            q = q.Where(x => x.Status == MehkemeIsiStatus.Tamamlandi || x.Status == MehkemeIsiStatus.Baghlandi);
        else if (status == "hamisi")
            q = q.Where(x => x.Status == MehkemeIsiStatus.Icra
                          || x.Status == MehkemeIsiStatus.Tamamlandi
                          || x.Status == MehkemeIsiStatus.Baghlandi);
        else // "aktiv" — yalnız icrada olanlar (default)
            q = q.Where(x => x.Status == MehkemeIsiStatus.Icra);

        var list = await q
            .OrderByDescending(x => x.YenilenmeTarixi ?? x.YaradilmaTarixi)
            .ToListAsync();

        // Zaminlər — borclunun altında alt sətir kimi göstərmək üçün tam yüklənir (AsNoTracking)
        var ids = list.Select(x => x.Id).ToList();
        var zaminlar = ids.Count == 0
            ? new List<MehkemeZamin>()
            : await _uow.Repository<MehkemeZamin>().Query().AsNoTracking()
                .Where(z => !z.Silinib && ids.Contains(z.MehkemeIsiId))
                .OrderBy(z => z.Id)
                .ToListAsync();
        // Eyni zamin həm Excel idxalından (icra sahələri, FİN yox), həm Oracle snapshot-dan
        // (FİN/ünvan/doğum) gələ bilər → ada görə birləşdir (dublikatları aradan qaldır).
        var zaminDict = zaminlar
            .GroupBy(z => z.MehkemeIsiId)
            .ToDictionary(g => g.Key, g => MergeZaminler(g));

        // "Son ödəniş tarixi" = Oracle "Son əməliyyat" (Detal səhifəsi ilə eyni mənbə).
        // Canlı siyahını bir dəfə çəkib hər işə tətbiq edirik; Oracle əlçatmazsa DB snapshot qalır.
        List<MehkemeKreditSatirDto>? canli = null;
        try
        {
            var siyahi = await SiyahiGetirAsync();
            if (siyahi.Satirlar.Count > 0) canli = siyahi.Satirlar;
        }
        catch { /* Oracle yoxdursa snapshot qalsın */ }

        return list.Select(x => new IcraIsiListDto
        {
            Id                = x.Id,
            BorcluAd          = x.BorcluAd,
            QeydiyyatNomresi  = x.QeydiyyatNomresi,
            Subkod            = string.IsNullOrWhiteSpace(x.Subkod) ? x.KreditSubHesab : x.Subkod,
            Status            = x.Status,
            MehkemeStatusMetn = x.MehkemeStatusMetn,
            QalanBorc         = x.QalanBorc,
            SonOdenisTarixi   = CanliSonOdenis(x, canli) ?? x.SonOdenisTarixi,
            Qeydiyyati        = x.Qeydiyyati,
            EmekHaqqiMelumati = x.EmekHaqqiMelumati,
            AdinaSorgu        = x.AdinaSorgu,
            DypSorguTarixi    = x.DypSorguTarixi,
            EmlakaHebs        = x.EmlakaHebs,
            Stop              = x.Stop,
            IcraMemuru        = x.IcraMemuru,
            IcraSonIsler      = x.IcraSonIsler,
            DogumTarixi       = CanliDogumTarixi(x, canli) ?? x.DogumTarixi,   // ana (Oracle) sorğudan, yoxdursa DB snapshot
            Zamin             = x.Zamin,
            ZaminSayi         = zaminDict.GetValueOrDefault(x.Id)?.Count ?? 0,
            Zaminler          = zaminDict.GetValueOrDefault(x.Id) ?? new List<IcraZaminSetirDto>(),
            QetnameTarixi     = x.QetnameTarixi,
            IsYeri            = x.IsYeri,
            IcraQeyd          = x.IcraQeyd
        }).ToList();
    }

    public async Task<IList<YaxinlasanGorusDto>> YaxinlasanGoruslerAsync()
    {
        var bugun = DateTime.Today;
        var merheleler = await _uow.Repository<MehkemeMerhelesi>().Query().AsNoTracking()
            .Where(m => !m.Silinib && m.Tarix >= bugun && !m.MehkemeIsi.Silinib
                        && m.MerheleTipi == MerheleTipi.MehkemeIclasi)
            .Include(m => m.MehkemeIsi)
            .OrderBy(m => m.Tarix).ThenBy(m => m.Saat)
            .ToListAsync();

        return merheleler.Select(m => new YaxinlasanGorusDto
        {
            MehkemeIsiId     = m.MehkemeIsiId,
            BorcluAd         = m.MehkemeIsi.BorcluAd,
            QeydiyyatNomresi = m.MehkemeIsi.QeydiyyatNomresi,
            Tarix            = m.Tarix,
            Saat             = m.Saat,
            MerheleTipi      = m.MerheleTipi,
            IsNomresi        = m.MehkemeIsi.IsNomresi,
            Hakim            = m.MehkemeIsi.Hakim,
            Qeyd             = m.Qeyd
        }).ToList();
    }

    public async Task<MehkemeMonitorDto> MonitorGetirAsync()
    {
        var bugun = DateTime.Today;
        var d7  = bugun.AddDays(7);
        var d30 = bugun.AddDays(30);
        var dto = new MehkemeMonitorDto();

        // Bütün işlər — yüngül proyeksiya (yalnız oxu; entity tracking yox)
        var isler = await _uow.Repository<MehkemeIsi>().Query().AsNoTracking()
            .Where(x => !x.Silinib)
            .Select(x => new
            {
                x.Id, x.BorcluAd, x.QeydiyyatNomresi, x.Status, x.Nov,
                x.QalanBorc, x.MehkemeXerci, x.Qeydiyyati,
                x.BaslamaTarixi, x.YaradilmaTarixi
            })
            .ToListAsync();

        dto.CemIs = isler.Count;
        foreach (var x in isler)
        {
            switch (x.Status)
            {
                case MehkemeIsiStatus.Hazirlanir: dto.Hazirlanir++; break;
                case MehkemeIsiStatus.Mehkemede:  dto.Mehkemede++;  break;
                case MehkemeIsiStatus.Icra:       dto.Icrada++;     break;
                case MehkemeIsiStatus.Tamamlandi: dto.Tamamlandi++; break;
                case MehkemeIsiStatus.Baghlandi:  dto.Baglandi++;   break;
            }
            dto.CemQalanBorc    += x.QalanBorc ?? 0m;
            dto.CemMehkemeXerci += x.MehkemeXerci ?? 0m;
        }
        dto.AktivIs = dto.Hazirlanir + dto.Mehkemede + dto.Icrada;

        static bool Aktivdir(MehkemeIsiStatus s) =>
            s == MehkemeIsiStatus.Hazirlanir || s == MehkemeIsiStatus.Mehkemede || s == MehkemeIsiStatus.Icra;

        dto.AktivQalanBorc = isler.Where(x => Aktivdir(x.Status)).Sum(x => x.QalanBorc ?? 0m);

        // Növ üzrə paylanma
        dto.NovUzre = isler
            .GroupBy(x => x.Nov)
            .Select(g => new MonitorAdSayDto { Ad = NovAdi(g.Key), Say = g.Count() })
            .OrderByDescending(a => a.Say)
            .ToList();

        // Rayon üzrə (aktiv işlər, top 8)
        dto.RayonUzre = isler
            .Where(x => Aktivdir(x.Status) && !string.IsNullOrWhiteSpace(x.Qeydiyyati))
            .GroupBy(x => x.Qeydiyyati!.Trim())
            .Select(g => new MonitorAdSayDto { Ad = g.Key, Say = g.Count() })
            .OrderByDescending(a => a.Say)
            .Take(8)
            .ToList();

        // Son 12 ay üzrə yeni işlər (məhkəməyə verilmə, yoxdursa yaradılma)
        var ayBasla = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-11);
        var aySaylari = isler
            .Select(x => x.BaslamaTarixi ?? x.YaradilmaTarixi)
            .Where(t => t >= ayBasla)
            .GroupBy(t => t.Year * 100 + t.Month)
            .ToDictionary(g => g.Key, g => g.Count());
        for (int i = 0; i < 12; i++)
        {
            var d = ayBasla.AddMonths(i);
            dto.AyUzre.Add(new MonitorAySayDto
            {
                Ay  = d.ToString("MM/yy", CultureInfo.InvariantCulture),
                Say = aySaylari.TryGetValue(d.Year * 100 + d.Month, out var c) ? c : 0
            });
        }

        // Ən böyük qalan borc (aktiv, top 10)
        dto.EnBoyukBorclu = isler
            .Where(x => Aktivdir(x.Status) && (x.QalanBorc ?? 0m) > 0m)
            .OrderByDescending(x => x.QalanBorc)
            .Take(10)
            .Select(x => new MonitorBorcluDto
            {
                Id = x.Id, BorcluAd = x.BorcluAd, QeydiyyatNomresi = x.QeydiyyatNomresi,
                QalanBorc = x.QalanBorc, Status = x.Status, Qeydiyyati = x.Qeydiyyati
            })
            .ToList();

        // Zaminli iş sayı (yalnız silinməmiş işlər)
        var idSet = isler.Select(x => x.Id).ToHashSet();
        var zaminliIdler = await _uow.Repository<MehkemeZamin>().Query().AsNoTracking()
            .Where(z => !z.Silinib)
            .Select(z => z.MehkemeIsiId)
            .Distinct()
            .ToListAsync();
        dto.ZaminliIs = zaminliIdler.Count(id => idSet.Contains(id));

        // Məhkəmə iclasları (proyeksiya — tracking/Include tələsi yox)
        var iclaslar = await _uow.Repository<MehkemeMerhelesi>().Query().AsNoTracking()
            .Where(m => !m.Silinib && !m.MehkemeIsi.Silinib
                        && m.MerheleTipi == MerheleTipi.MehkemeIclasi)
            .Select(m => new
            {
                m.MehkemeIsiId, m.Tarix, m.Saat, m.Qeyd,
                AdSoyad = m.MehkemeIsi.BorcluAd,
                m.MehkemeIsi.QeydiyyatNomresi,
                Hakim   = m.MehkemeIsi.Hakim,
                IsNo    = m.MehkemeIsi.IsNomresi,
                Status  = m.MehkemeIsi.Status
            })
            .ToListAsync();

        dto.YaxinlasanIclas7  = iclaslar.Count(m => m.Tarix.Date >= bugun && m.Tarix.Date <= d7);
        dto.YaxinlasanIclas30 = iclaslar.Count(m => m.Tarix.Date >= bugun && m.Tarix.Date <= d30);
        dto.GecikmisIclas     = iclaslar.Count(m => m.Tarix.Date < bugun
                                  && (m.Status == MehkemeIsiStatus.Mehkemede || m.Status == MehkemeIsiStatus.Hazirlanir));

        dto.YaxinlasanIclaslar = iclaslar
            .Where(m => m.Tarix.Date >= bugun && m.Tarix.Date <= d30)
            .OrderBy(m => m.Tarix).ThenBy(m => m.Saat)
            .Take(12)
            .Select(m => new YaxinlasanGorusDto
            {
                MehkemeIsiId = m.MehkemeIsiId, BorcluAd = m.AdSoyad,
                QeydiyyatNomresi = m.QeydiyyatNomresi, Tarix = m.Tarix, Saat = m.Saat,
                MerheleTipi = MerheleTipi.MehkemeIclasi, IsNomresi = m.IsNo, Hakim = m.Hakim, Qeyd = m.Qeyd
            })
            .ToList();

        return dto;
    }

    private static string NovAdi(MehkemeIsiNov n) => n switch
    {
        MehkemeIsiNov.Ipoteka    => "İpoteka",
        MehkemeIsiNov.Istehlak   => "İstehlak",
        MehkemeIsiNov.KartKredit => "Kart krediti",
        _                        => "Digər"
    };

    public async Task<MehkemeIsiDetailDto?> DetailGetirAsync(int id)
    {
        var x = await _uow.Repository<MehkemeIsi>()
            .Query()
            .Where(m => m.Id == id && !m.Silinib)
            .Include(m => m.Merheleler)
            .FirstOrDefaultAsync();

        if (x == null) return null;

        var zaminler = await _uow.Repository<MehkemeZamin>().Query()
            .Where(z => z.MehkemeIsiId == id && !z.Silinib)
            .OrderBy(z => z.Id).AsNoTracking().ToListAsync();

        var xercler = await _uow.Repository<MehkemeXerci>().Query()
            .Where(xc => xc.MehkemeIsiId == id && !xc.Silinib)
            .OrderBy(xc => xc.Tarix).AsNoTracking().ToListAsync();

        var senedler = await _uow.Repository<MehkemeSened>().Query()
            .Where(s => s.MehkemeIsiId == id && !s.Silinib)
            .OrderByDescending(s => s.Tarix).AsNoTracking().ToListAsync();

        var dto = new MehkemeIsiDetailDto
        {
            Id                = x.Id,
            QeydiyyatNomresi  = x.QeydiyyatNomresi,
            BorcluAd          = x.BorcluAd,
            EsasBorc          = x.EsasBorc,
            MehkemeXerci      = x.MehkemeXerci,
            Nov               = x.Nov,
            Status            = x.Status,
            BaslamaTarixi     = x.BaslamaTarixi,
            Qeyd              = x.Qeyd,
            Qerardad          = x.Qerardad,
            KreditHesabi      = x.KreditHesabi,
            Subkod            = x.Subkod,
            QalanBorc         = x.QalanBorc,
            SonOdenisTarixi   = x.SonOdenisTarixi,
            Qeydiyyati        = x.Qeydiyyati,
            EmekHaqqiMelumati = x.EmekHaqqiMelumati,
            DypSorguTarixi    = x.DypSorguTarixi,
            AdinaSorgu        = x.AdinaSorgu,
            EmlakaHebs        = x.EmlakaHebs,
            Stop              = x.Stop,
            IcraMemuru        = x.IcraMemuru,
            Hakim             = x.Hakim,
            IsNomresi         = x.IsNomresi,
            IcraSonIsler      = x.IcraSonIsler,
            DogumTarixi       = x.DogumTarixi,
            Zamin             = x.Zamin,
            QetnameTarixi     = x.QetnameTarixi,
            IsYeri            = x.IsYeri,
            IcraQeyd          = x.IcraQeyd,
            YaradilmaTarixi   = x.YaradilmaTarixi,
            Merheleler        = x.Merheleler
                .Where(m => !m.Silinib)
                .OrderByDescending(m => m.Tarix)
                .Select(m => new MehkemeMerheleDto
                {
                    Id           = m.Id,
                    MerheleTipi  = m.MerheleTipi,
                    Tarix        = m.Tarix,
                    Saat         = m.Saat,
                    IsNomresi    = m.IsNomresi,
                    Hakim        = m.Hakim,
                    IcraciMemur  = m.IcraciMemur,
                    Qeyd         = m.Qeyd,
                    SenedYolu    = m.SenedYolu
                }).ToList(),
            Zaminler          = zaminler.Select(MapZamin).ToList()
        };

        // Məhkəmə xərcləri (bir neçə ola bilər) — siyahı + cəm; cəm "Məhkəmə xərci" kartına gedir
        dto.Xercler = xercler.Select(xc => new MehkemeXerciDto
        {
            Id = xc.Id, Mebleg = xc.Mebleg, Tarix = xc.Tarix, Mehkeme = xc.Mehkeme
        }).ToList();
        if (xercler.Count > 0) dto.MehkemeXerci = xercler.Sum(xc => xc.Mebleg);

        dto.Senedler = senedler.Select(s => new MehkemeSenedDto
        {
            Id = s.Id, Ad = s.Ad, Novu = s.Novu, FaylYolu = s.FaylYolu, Tarix = s.Tarix
        }).ToList();

        // Maliyyəni Oracle canlısından çək (aktiv siyahıda olan kreditlər üçün);
        // siyahıda olmayanların məbləğləri əl ilə doldurulur (snapshot qalır).
        await CanliMaliyyeniTetbiqEtAsync(x, dto);

        return dto;
    }

    // ── Detal səhifəsi üçün Oracle canlı maliyyəsi (hesab + K.S. açarı) ──
    private async Task CanliMaliyyeniTetbiqEtAsync(MehkemeIsi ish, MehkemeIsiDetailDto dto)
    {
        var hesab = !string.IsNullOrWhiteSpace(ish.QeydiyyatNomresi) ? ish.QeydiyyatNomresi.Trim()
                  : (ish.KreditHesabi ?? "").Trim();
        var ks    = !string.IsNullOrWhiteSpace(ish.KreditSubHesab) ? ish.KreditSubHesab.Trim()
                  : (ish.Subkod ?? "").Trim();
        if (string.IsNullOrWhiteSpace(hesab)) return;

        MehkemeSiyahiResultDto siyahi;
        try { siyahi = await SiyahiGetirAsync(); }
        catch { return; }   // Oracle əlçatmazdırsa snapshot qalsın
        if (siyahi.Satirlar.Count == 0) return;

        var row = siyahi.Satirlar.FirstOrDefault(s =>
            s.KreditHesabi.Trim() == hesab &&
            (string.IsNullOrWhiteSpace(ks) || s.Ks.Trim() == ks));
        if (row == null) return;   // Oracle aktiv siyahısında yoxdur → əl ilə doldurulacaq

        // Əsas borc = qalıq + vaxtı keçmiş qalıq (sırf əsas borc, faizsiz)
        if (row.Qaliq.HasValue || row.VkQaliq.HasValue)
            dto.EsasBorc = (row.Qaliq ?? 0m) + (row.VkQaliq ?? 0m);

        // Qalan borc = tam qalıq (əsas + faiz + cərimə) = 1-ci səhifədəki "Tam Qalıq"
        dto.QalanBorc = row.TamQaliq;

        // Faiz borcu = hesablanmış faiz + vaxtı keçmiş faiz/cərimə
        if (row.FaizMeblegi.HasValue || row.VkFaizMeblegi.HasValue)
            dto.FaizBorcu = (row.FaizMeblegi ?? 0m) + (row.VkFaizMeblegi ?? 0m);

        // Təminat = girovun növü (Oracle: girovun_novu)
        if (!string.IsNullOrWhiteSpace(row.GirovunNovu)) dto.TeminatMetn = row.GirovunNovu;

        // Status = Oracle item_01 (canlı); Doğum tarixi = Oracle dogum_tarixi
        if (!string.IsNullOrWhiteSpace(row.Status)) dto.StatusMetn = row.Status;
        var dgt = ParseTarix(row.DogumTarixi);
        if (dgt.HasValue) dto.DogumTarixi = dgt;

        // Son ödəniş tarixi = son əməliyyat tarixi (Oracle "dd.MM.yyyy")
        var t = ParseTarix(row.SonEmeliyyatTarixi);
        if (t.HasValue) dto.SonOdenisTarixi = t;

        dto.MaliyyeCanli = true;
    }

    private static DateTime? ParseTarix(string? s)
        => DateTime.TryParseExact((s ?? "").Trim(), "dd.MM.yyyy",
               CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

    // İcra grid/export üçün: işin kreditini canlı Oracle siyahısında tapıb "Son əməliyyat"
    // tarixini qaytar (Detal ilə eyni hesab+K.S. açarı). Tapılmasa null → DB snapshot işlənir.
    private static DateTime? CanliSonOdenis(MehkemeIsi x, IReadOnlyList<MehkemeKreditSatirDto>? canli)
        => ParseTarix(CanliTap(x, canli)?.SonEmeliyyatTarixi);

    // İş üçün canlı Oracle siyahısındakı doğum tarixi (Aktiv Müştərilər "ana sorğu") — borclu sətrində göstərilir.
    private static DateTime? CanliDogumTarixi(MehkemeIsi x, IReadOnlyList<MehkemeKreditSatirDto>? canli)
        => ParseTarix(CanliTap(x, canli)?.DogumTarixi);

    // İşi canlı Oracle siyahısında hesab+K.S. açarı ilə tapır (Detal ilə eyni məntiq).
    private static MehkemeKreditSatirDto? CanliTap(MehkemeIsi x, IReadOnlyList<MehkemeKreditSatirDto>? canli)
    {
        if (canli == null || canli.Count == 0) return null;
        var hesab = !string.IsNullOrWhiteSpace(x.QeydiyyatNomresi) ? x.QeydiyyatNomresi.Trim()
                  : (x.KreditHesabi ?? "").Trim();
        var ks    = !string.IsNullOrWhiteSpace(x.KreditSubHesab) ? x.KreditSubHesab.Trim()
                  : (x.Subkod ?? "").Trim();
        if (string.IsNullOrWhiteSpace(hesab)) return null;
        return canli.FirstOrDefault(s =>
            s.KreditHesabi.Trim() == hesab &&
            (string.IsNullOrWhiteSpace(ks) || s.Ks.Trim() == ks));
    }

    // Bir işin zaminlərini ada görə birləşdirir (Excel idxalı + Oracle snapshot dublikatlarını
    // tək sətirdə yığır). Hər sahə üçün ilk boş olmayan dəyər götürülür; doğum tarixi
    // üstünlüklə FİN-i olan (Oracle) qeyddən, sonra Excel seriyası tarixə çevrilir.
    private static List<IcraZaminSetirDto> MergeZaminler(IEnumerable<MehkemeZamin> zlist)
    {
        var result = new List<IcraZaminSetirDto>();
        foreach (var grup in zlist.GroupBy(z => (z.Ad ?? "").Trim().ToLowerInvariant()))
        {
            if (string.IsNullOrWhiteSpace(grup.Key)) continue;
            string? Pick(Func<MehkemeZamin, string?> sel) =>
                grup.Select(sel).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

            var oracleZamin = grup.FirstOrDefault(z => !string.IsNullOrWhiteSpace(z.Fin));
            var dogumXam = (oracleZamin != null && !string.IsNullOrWhiteSpace(oracleZamin.DogumTarixi))
                ? oracleZamin.DogumTarixi
                : grup.Select(z => z.DogumTarixi).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            result.Add(new IcraZaminSetirDto
            {
                Id               = grup.Min(z => z.Id),
                Ad               = Pick(z => z.Ad) ?? "(naməlum)",
                Fin              = Pick(z => z.Fin),
                DogumTarixi      = ZaminDogum(dogumXam),
                Telefon          = Pick(z => z.Telefon),
                Unvan            = Pick(z => z.Unvan),
                EmekHaqqiTutulma = Pick(z => z.EmekHaqqiTutulma),
                AdinaSorgu       = Pick(z => z.AdinaSorgu),
                DypSorgu         = Pick(z => z.DypSorgu),
                EmlakaHebs       = Pick(z => z.EmlakaHebs),
                Stop             = Pick(z => z.Stop),
                IcraMemuru       = Pick(z => z.IcraMemuru),
                IcraSonIsler     = Pick(z => z.IcraSonIsler),
                IsYeri           = Pick(z => z.IsYeri),
                IcraQeyd         = Pick(z => z.IcraQeyd)
            });
        }
        return result;
    }

    // Excel doğum tarixi seriyasını (məs. "33105") oxunaqlı tarixə çevirir; mətn tarix olduğu kimi qalır.
    private static string? ZaminDogum(string? s)
    {
        s = (s ?? "").Trim();
        if (s.Length == 0) return null;
        if (!s.Contains('.') && !s.Contains('-') && !s.Contains('/')
            && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
            && serial >= 10000 && serial <= 60000)
        {
            try { return DateTime.FromOADate(serial).ToString("dd.MM.yyyy"); } catch { /* seriya deyil */ }
        }
        return s;
    }

    // ── Zamin icra qatı ──────────────────────────────────
    public async Task<int> ZaminElaveEtAsync(ZaminIcraCreateDto dto, int isciId)
    {
        var z = new MehkemeZamin
        {
            MehkemeIsiId     = dto.MehkemeIsiId,
            Ad               = (dto.Ad ?? "").Trim(),
            Fin              = dto.Fin?.Trim(),
            DogumTarixi      = dto.DogumTarixi?.Trim(),
            Telefon          = dto.Telefon?.Trim(),
            Unvan            = dto.Unvan?.Trim(),
            EmekHaqqiTutulma = dto.EmekHaqqiTutulma?.Trim(),
            IsYeri           = dto.IsYeri?.Trim(),
            EmlakaHebs       = dto.EmlakaHebs?.Trim(),
            Stop             = dto.Stop?.Trim(),
            IcraMemuru       = dto.IcraMemuru?.Trim(),
            IcraSonIsler     = dto.IcraSonIsler?.Trim(),
            DypSorgu         = dto.DypSorgu?.Trim(),
            AdinaSorgu       = dto.AdinaSorgu?.Trim(),
            IcraQeyd         = dto.IcraQeyd?.Trim(),
            YaradanIcraciId  = isciId,
            YaradilmaTarixi  = DateTime.Now
        };
        await _uow.Repository<MehkemeZamin>().YaratAsync(z);
        await _uow.YaddaSaxlaAsync();
        return z.Id;
    }

    public async Task<bool> ZaminYenileAsync(ZaminIcraUpdateDto dto, int isciId)
    {
        var z = await _uow.Repository<MehkemeZamin>().Query()
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.Silinib);
        if (z == null) return false;

        // Kimlik (Ad, Fin, ...) Oracle-dan gəlir — redaktə olunmur; yalnız icra qatı yenilənir
        z.EmekHaqqiTutulma = dto.EmekHaqqiTutulma?.Trim();
        z.IsYeri           = dto.IsYeri?.Trim();
        z.EmlakaHebs       = dto.EmlakaHebs?.Trim();
        z.Stop             = dto.Stop?.Trim();
        z.IcraMemuru       = dto.IcraMemuru?.Trim();
        z.IcraSonIsler     = dto.IcraSonIsler?.Trim();
        z.DypSorgu         = dto.DypSorgu?.Trim();
        z.AdinaSorgu       = dto.AdinaSorgu?.Trim();
        z.IcraQeyd         = dto.IcraQeyd?.Trim();
        z.YenileyenIcraciId = isciId;
        z.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeZamin>().YenileAsync(z);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> ZaminSilAsync(int zaminId, int isciId)
    {
        var z = await _uow.Repository<MehkemeZamin>().Query()
            .FirstOrDefaultAsync(x => x.Id == zaminId && !x.Silinib);
        if (z == null) return false;
        z.Silinib       = true;
        z.SilinmeTarixi = DateTime.Now;
        z.SilenIcraciId = isciId;
        await _uow.Repository<MehkemeZamin>().YenileAsync(z);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    // Bu kreditin zaminlərini Oracle-dan (Siyahı sorğusu) çəkib MehkemeZamin kimi yarat.
    // Mövcud zaminin (FİN/Ad açarına görə) icra məlumatına toxunmur.
    public async Task<int> ZaminleriOracledanYukleAsync(int mehkemeIsiId, int isciId)
    {
        var ish = await _uow.Repository<MehkemeIsi>().Query()
            .FirstOrDefaultAsync(x => x.Id == mehkemeIsiId && !x.Silinib);
        if (ish == null) return 0;

        var hesab = !string.IsNullOrWhiteSpace(ish.QeydiyyatNomresi) ? ish.QeydiyyatNomresi.Trim()
                  : (ish.KreditHesabi ?? "").Trim();
        var ks    = !string.IsNullOrWhiteSpace(ish.KreditSubHesab) ? ish.KreditSubHesab.Trim()
                  : (ish.Subkod ?? "").Trim();
        if (string.IsNullOrWhiteSpace(hesab)) return 0;

        var siyahi = await SiyahiGetirAsync();
        var row = siyahi.Satirlar.FirstOrDefault(s =>
            s.KreditHesabi.Trim() == hesab &&
            (string.IsNullOrWhiteSpace(ks) || s.Ks.Trim() == ks));
        if (row == null || row.Zaminler.Count == 0) return 0;

        var movcud = await _uow.Repository<MehkemeZamin>().Query()
            .Where(z => z.MehkemeIsiId == mehkemeIsiId && !z.Silinib).ToListAsync();

        int sayi = 0;
        foreach (var oz in row.Zaminler)
        {
            var key = (oz.Fin ?? oz.Ad ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key)) continue;
            bool varDiya = movcud.Any(m => ((m.Fin ?? m.Ad ?? "").Trim().ToLowerInvariant()) == key);
            if (varDiya) continue;   // artıq var — icra məlumatını qoru

            await _uow.Repository<MehkemeZamin>().YaratAsync(new MehkemeZamin
            {
                MehkemeIsiId    = mehkemeIsiId,
                Ad              = string.IsNullOrWhiteSpace(oz.Ad) ? "(naməlum)" : oz.Ad.Trim(),
                Fin             = oz.Fin?.Trim(),
                DogumTarixi     = oz.DogumTarixi?.Trim(),
                Telefon         = oz.Telefon?.Trim(),
                Unvan           = oz.Unvan?.Trim(),
                YaradanIcraciId = isciId,
                YaradilmaTarixi = DateTime.Now
            });
            sayi++;
        }
        if (sayi > 0) await _uow.YaddaSaxlaAsync();
        return sayi;
    }

    // Zaminləri Siyahı datasından (ana sorğu) snapshot et — Oracle-a yeni sorğu atmadan
    public async Task<int> ZaminleriSnapshotEtAsync(int mehkemeIsiId, List<MehkemeZaminDto> zaminler, int isciId)
    {
        if (zaminler == null || zaminler.Count == 0) return 0;

        var movcud = await _uow.Repository<MehkemeZamin>().Query()
            .Where(z => z.MehkemeIsiId == mehkemeIsiId && !z.Silinib).ToListAsync();

        int sayi = 0;
        foreach (var oz in zaminler)
        {
            var fin = (oz.Fin ?? "").Trim().ToLowerInvariant();
            var ad  = (oz.Ad ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fin) && string.IsNullOrWhiteSpace(ad)) continue;

            // Eyni zamin (FİN və ya ad üzrə) artıq varsa — məs. Excel idxalından gəlib —
            // dublikat yaratma, yalnız Oracle kimliyini (FİN/ünvan/telefon/doğum) əlavə et.
            var mz = movcud.FirstOrDefault(m =>
                (!string.IsNullOrWhiteSpace(fin) && (m.Fin ?? "").Trim().ToLowerInvariant() == fin)
             || (!string.IsNullOrWhiteSpace(ad)  && (m.Ad  ?? "").Trim().ToLowerInvariant() == ad));

            if (mz != null)
            {
                bool dey = false;
                if (string.IsNullOrWhiteSpace(mz.Fin)     && !string.IsNullOrWhiteSpace(oz.Fin))     { mz.Fin = oz.Fin.Trim();         dey = true; }
                if (string.IsNullOrWhiteSpace(mz.Unvan)   && !string.IsNullOrWhiteSpace(oz.Unvan))   { mz.Unvan = oz.Unvan.Trim();     dey = true; }
                if (string.IsNullOrWhiteSpace(mz.Telefon) && !string.IsNullOrWhiteSpace(oz.Telefon)) { mz.Telefon = oz.Telefon.Trim(); dey = true; }
                if (!string.IsNullOrWhiteSpace(oz.DogumTarixi)) { mz.DogumTarixi = oz.DogumTarixi.Trim(); dey = true; }  // Oracle doğum daha etibarlı
                if (dey)
                {
                    mz.YenileyenIcraciId = isciId;
                    mz.YenilenmeTarixi   = DateTime.Now;
                    await _uow.Repository<MehkemeZamin>().YenileAsync(mz);
                    sayi++;
                }
                continue;
            }

            await _uow.Repository<MehkemeZamin>().YaratAsync(new MehkemeZamin
            {
                MehkemeIsiId    = mehkemeIsiId,
                Ad              = string.IsNullOrWhiteSpace(oz.Ad) ? "(naməlum)" : oz.Ad.Trim(),
                Fin             = oz.Fin?.Trim(),
                DogumTarixi     = oz.DogumTarixi?.Trim(),
                Telefon         = oz.Telefon?.Trim(),
                Unvan           = oz.Unvan?.Trim(),
                YaradanIcraciId = isciId,
                YaradilmaTarixi = DateTime.Now
            });
            sayi++;
        }
        if (sayi > 0) await _uow.YaddaSaxlaAsync();
        return sayi;
    }

    private static ZaminIcraDto MapZamin(MehkemeZamin z) => new()
    {
        Id               = z.Id,
        MehkemeIsiId     = z.MehkemeIsiId,
        Ad               = z.Ad,
        Fin              = z.Fin,
        DogumTarixi      = z.DogumTarixi,
        Telefon          = z.Telefon,
        Unvan            = z.Unvan,
        EmekHaqqiTutulma = z.EmekHaqqiTutulma,
        IsYeri           = z.IsYeri,
        EmlakaHebs       = z.EmlakaHebs,
        Stop             = z.Stop,
        IcraMemuru       = z.IcraMemuru,
        IcraSonIsler     = z.IcraSonIsler,
        DypSorgu         = z.DypSorgu,
        AdinaSorgu       = z.AdinaSorgu,
        IcraQeyd         = z.IcraQeyd
    };

    public async Task<MehkemeIsi> YaratAsync(MehkemeIsiCreateDto dto, int yaradanIsciId)
    {
        var entity = new MehkemeIsi
        {
            QeydiyyatNomresi = dto.QeydiyyatNomresi.Trim(),
            BorcluAd         = dto.BorcluAd.Trim(),
            EsasBorc         = dto.EsasBorc,
            MehkemeXerci     = dto.MehkemeXerci,
            Nov              = dto.Nov,
            Status           = dto.Status,
            BaslamaTarixi    = dto.BaslamaTarixi,
            Qeyd             = string.IsNullOrWhiteSpace(dto.Qeyd) ? null : dto.Qeyd.Trim(),
            YaradanIcraciId  = yaradanIsciId,
            YaradilmaTarixi  = DateTime.Now
        };
        await _uow.Repository<MehkemeIsi>().YaratAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return entity;
    }

    public async Task<bool> YenileAsync(int id, MehkemeIsiUpdateDto dto, int yenileyenIsciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        // ── Məhkəmə mərhələsi (əl ilə izləmə — Oracle deyil) ──
        entity.Nov               = dto.Nov;      // İpoteka/İstehlak/Kart/Digər
        entity.Status            = dto.Status;   // iş axını: Hazırlanır→Məhkəmədə→İcra→Tamamlandı→Bağlandı
        entity.BaslamaTarixi     = dto.BaslamaTarixi;
        entity.Qeyd              = string.IsNullOrWhiteSpace(dto.Qeyd) ? null : dto.Qeyd.Trim();
        entity.Qerardad          = string.IsNullOrWhiteSpace(dto.Qerardad) ? null : dto.Qerardad.Trim();
        entity.Qeydiyyati        = dto.Qeydiyyati;
        entity.EmekHaqqiMelumati = dto.EmekHaqqiMelumati;
        entity.DypSorguTarixi    = dto.DypSorguTarixi;
        entity.AdinaSorgu        = dto.AdinaSorgu;
        entity.EmlakaHebs        = dto.EmlakaHebs;
        entity.Stop              = dto.Stop;
        entity.IcraMemuru        = dto.IcraMemuru;
        entity.Hakim             = dto.Hakim;        // iş səviyyəsində (bir dənə)
        entity.IsNomresi         = dto.IsNomresi;    // iş səviyyəsində (bir dənə)
        entity.IcraSonIsler      = dto.IcraSonIsler;
        entity.Zamin             = dto.Zamin;
        entity.QetnameTarixi     = dto.QetnameTarixi;
        entity.IsYeri            = dto.IsYeri;
        entity.IcraQeyd          = dto.IcraQeyd;
        entity.YenileyenIcraciId = yenileyenIsciId;
        entity.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    // ── Excel "Məhkəmə" sheet → MehkemeIsi (arxiv idxalı) ──────────────────
    // Təkrar idxalı atlayır (ad + məhkəməyə verilmə tarixi açarı ilə).
    // İclas tarixləri "Məhkəmə iclası" mərhələsi kimi yazılır. Oracle yazılmır.
    public async Task<(int isSayi, int merheleSayi)> ExcelImportAsync(IList<MehkemeCedvelImportDto> rows, int isciId, bool onizleme = false)
    {
        if (rows == null || rows.Count == 0) return (0, 0);

        var movcud = await _uow.Repository<MehkemeIsi>().Query().AsNoTracking()
            .Where(x => !x.Silinib)
            .Select(x => new { x.BorcluAd, x.MehkemeyeVerilmeTarixi })
            .ToListAsync();
        var acarlar = new HashSet<string>(movcud.Select(m =>
            (m.BorcluAd ?? "").Trim().ToLowerInvariant() + "|" +
            (m.MehkemeyeVerilmeTarixi?.ToString("yyyy-MM-dd") ?? "")));

        int isSayi = 0, merheleSayi = 0;
        var repo = _uow.Repository<MehkemeIsi>();

        foreach (var r in rows)
        {
            var ad = (r.BorcluAd ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ad)) continue;

            var acar = ad.ToLowerInvariant() + "|" + (r.MehkemeyeVerilmeTarixi?.ToString("yyyy-MM-dd") ?? "");
            if (!acarlar.Add(acar)) continue;   // artıq var (və ya bu idxalda təkrar) — atla

            isSayi++;

            // Etibarlı iclaslar (tarix və ya saat olanlar) — say önizləmədə və realda EYNİdir.
            var validIclaslar = r.Iclaslar
                .Where(ic => !(ic.Tarix == null && string.IsNullOrWhiteSpace(ic.Saat)))
                .ToList();
            merheleSayi += validIclaslar.Count;

            // ── ÖNİZLƏMƏ qapısı: yalnız say, DB-yə heç nə yazma ──
            if (onizleme) continue;

            var isNo = string.IsNullOrWhiteSpace(r.MehkemeIsNomresi) ? null : r.MehkemeIsNomresi.Trim();
            var hesab = (r.Hesab ?? "").Trim();
            var subkod = (r.Subkod ?? "").Trim();
            // "42" = PID arxiv kodu (Oracle-da aktiv müştəri yoxdur). Real hesab+subkod → AKTIV:
            // QeydiyyatNomresi+KreditSubHesab Oracle siyahısına (SiyahiGetir) linklənir →
            // Aktiv Müştərilər səhifəsində "iş açılıb" görünür. Köhnə (42/boş) → arxiv açarı.
            bool kohne = string.IsNullOrWhiteSpace(hesab) || hesab == "42" || subkod == "42";
            var entity = new MehkemeIsi
            {
                QeydiyyatNomresi       = kohne ? (r.Sira.HasValue ? $"ARX-{r.Sira}" : "ARX") : hesab,
                KreditSubHesab         = kohne ? null : subkod,
                KreditHesabi           = kohne ? null : hesab,
                Subkod                 = kohne ? null : subkod,
                BorcluAd               = ad,
                Sira                   = r.Sira,
                MehkemeStatusMetn      = string.IsNullOrWhiteSpace(r.StatusMetn) ? null : r.StatusMetn.Trim(),
                KreditNovuMetn         = string.IsNullOrWhiteSpace(r.GirovunNovu) ? null : r.GirovunNovu.Trim(),
                MehkemeSenedi          = isNo,
                IsNomresi              = isNo,
                BaslamaTarixi          = r.MehkemeyeVerilmeTarixi,
                MehkemeyeVerilmeTarixi = r.MehkemeyeVerilmeTarixi,
                Status                 = MehkemeIsiStatus.Mehkemede,   // Məhkəmə vərəqi → məhkəmə mərhələsi
                Nov                    = MehkemeIsiNov.Diger,
                YaradanIcraciId        = isciId,
                YaradilmaTarixi        = DateTime.Now
            };

            foreach (var ic in validIclaslar)
            {
                entity.Merheleler.Add(new MehkemeMerhelesi
                {
                    MerheleTipi     = MerheleTipi.MehkemeIclasi,
                    Tarix           = ic.Tarix ?? r.MehkemeyeVerilmeTarixi ?? DateTime.Today,
                    Saat            = string.IsNullOrWhiteSpace(ic.Saat) ? null : ic.Saat.Trim(),
                    YaradanIcraciId = isciId,
                    YaradilmaTarixi = DateTime.Now
                });
            }

            await repo.YaratAsync(entity);
        }

        if (!onizleme && isSayi > 0) await _uow.YaddaSaxlaAsync();
        return (isSayi, merheleSayi);
    }

    // ── Excel "İCRADA OLAN İŞLƏR" → MehkemeIsi (icra fazası) + MehkemeZamin ──
    // hesab+subkod ilə dedup (mövcud qeyd toxunulmur). MehkemeZamin-də MehkemeIsi-yə
    // naviqasiya yoxdur → iki fazalı: əvvəl işlər saxlanır (Id dolur), sonra zaminlər.
    public async Task<IcraImportNeticeDto> IcraCedvelImportAsync(IList<IcraCedvelImportDto> isler, int isciId, bool onizleme)
    {
        var netice = new IcraImportNeticeDto { Onizleme = onizleme, CemQrup = isler?.Count ?? 0 };
        if (isler == null || isler.Count == 0) return netice;

        // Mövcud işlər (TAM entity — yüksəltmə üçün lazımdır). Önizləmədə yazmadığımız üçün
        // AsNoTracking kifayətdir; real idxalda tracking lazımdır ki, status/sahə dəyişikliyi saxlanılsın.
        var bazaQ = _uow.Repository<MehkemeIsi>().Query().Where(x => !x.Silinib);
        var movcudList = onizleme ? await bazaQ.AsNoTracking().ToListAsync()
                                  : await bazaQ.ToListAsync();
        var movcudDict = new Dictionary<string, MehkemeIsi>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in movcudList)
        {
            var k = Acar(m.QeydiyyatNomresi, m.KreditSubHesab);
            if (!movcudDict.ContainsKey(k)) movcudDict[k] = m;
        }

        // Artıq zamini olan işlər — yüksəltmədə təkrar zamin əlavə etməmək üçün (idempotent).
        var zaminliIsler = onizleme
            ? new HashSet<int>()
            : new HashSet<int>(await _uow.Repository<MehkemeZamin>().Query().AsNoTracking()
                .Where(z => !z.Silinib).Select(z => z.MehkemeIsiId).ToListAsync());

        var repo = _uow.Repository<MehkemeIsi>();
        var pending = new List<(MehkemeIsi e, List<IcraZaminImportDto> z)>();

        foreach (var d in isler)
        {
            if (string.IsNullOrWhiteSpace(d.Hesab)) { netice.Xeta++; continue; }
            if (d.Arxiv) netice.Arxiv++; else netice.Aktiv++;

            var key = Acar(d.Hesab, d.Subkod);
            movcudDict.TryGetValue(key, out var mov);

            // Mövcud iş artıq icra/tamam/bağlı fazasındadırsa — toxunma (təkrar idxalda dəyişmir).
            if (mov != null && (mov.Status == MehkemeIsiStatus.Icra
                             || mov.Status == MehkemeIsiStatus.Tamamlandi
                             || mov.Status == MehkemeIsiStatus.Baghlandi))
            {
                netice.Atlanan++;
                continue;
            }

            // mov != null → Hazırlanır/Məhkəmədə fazasındadır → İCRAYA YÜKSƏLT (eyni iş, mərhələ irəliləyir).
            bool yukseltme = mov != null;
            if (yukseltme) netice.Yukseldilen++; else netice.YeniIs++;
            netice.Zamin += d.Zaminler.Count;
            if (netice.Numuneler.Count < 6)
                netice.Numuneler.Add(
                    $"{(string.IsNullOrWhiteSpace(d.BorcluAd) ? "(adsız)" : d.BorcluAd)} " +
                    $"[{d.Hesab}/{d.Subkod}] {(yukseltme ? "yüksəldildi" : (d.Arxiv ? "arxiv" : "yeni"))}, {d.Zaminler.Count} zamin");

            if (onizleme) continue;

            MehkemeIsi entity;
            if (yukseltme)
            {
                // Mövcud məhkəmə işini icra fazasına keçir; məhkəmə tarixçəsi (Merheleler) qalır.
                entity = mov!;
                entity.Status = d.Arxiv ? MehkemeIsiStatus.Baghlandi : MehkemeIsiStatus.Icra;
                if (string.IsNullOrWhiteSpace(entity.KreditHesabi))   entity.KreditHesabi   = d.Hesab;
                if (string.IsNullOrWhiteSpace(entity.Subkod))         entity.Subkod         = d.Subkod;
                if (string.IsNullOrWhiteSpace(entity.KreditSubHesab)) entity.KreditSubHesab = d.Subkod;
                if ((string.IsNullOrWhiteSpace(entity.BorcluAd) || entity.BorcluAd == "(naməlum)")
                    && !string.IsNullOrWhiteSpace(d.BorcluAd)) entity.BorcluAd = d.BorcluAd!.Trim();
                entity.MehkemeStatusMetn ??= d.StatusMetn;
                entity.QalanBorc         ??= d.QalanBorc;
                entity.SonOdenisTarixi   ??= d.SonOdenisTarixi;
                entity.Qeydiyyati        ??= d.Qeydiyyati;
                entity.EmekHaqqiMelumati ??= d.EmekHaqqiMelumati;
                entity.DypSorguTarixi    ??= d.DypSorguTarixi;
                entity.AdinaSorgu        ??= d.AdinaSorgu;
                entity.EmlakaHebs        ??= d.EmlakaHebs;
                entity.Stop              ??= d.Stop;
                entity.IcraMemuru        ??= d.IcraMemuru;
                entity.IcraSonIsler      ??= d.IcraSonIsler;
                entity.DogumTarixi       ??= d.DogumTarixi;
                entity.QetnameTarixi     ??= d.QetnameTarixi;
                entity.IsYeri            ??= d.IsYeri;
                entity.IcraQeyd          ??= d.IcraQeyd;
                entity.YenileyenIcraciId = isciId;
                entity.YenilenmeTarixi   = DateTime.Now;
                await repo.YenileAsync(entity);
            }
            else
            {
                entity = new MehkemeIsi
                {
                    QeydiyyatNomresi  = d.Hesab,
                    KreditSubHesab    = d.Subkod,
                    KreditHesabi      = d.Hesab,
                    Subkod            = d.Subkod,
                    BorcluAd          = string.IsNullOrWhiteSpace(d.BorcluAd) ? "(naməlum)" : d.BorcluAd!.Trim(),
                    Nov               = MehkemeIsiNov.Diger,
                    Status            = d.Arxiv ? MehkemeIsiStatus.Baghlandi : MehkemeIsiStatus.Icra,
                    MehkemeStatusMetn = d.StatusMetn,
                    QalanBorc         = d.QalanBorc,
                    SonOdenisTarixi   = d.SonOdenisTarixi,
                    Qeydiyyati        = d.Qeydiyyati,
                    EmekHaqqiMelumati = d.EmekHaqqiMelumati,
                    DypSorguTarixi    = d.DypSorguTarixi,
                    AdinaSorgu        = d.AdinaSorgu,
                    EmlakaHebs        = d.EmlakaHebs,
                    Stop              = d.Stop,
                    IcraMemuru        = d.IcraMemuru,
                    IcraSonIsler      = d.IcraSonIsler,
                    DogumTarixi       = d.DogumTarixi,
                    QetnameTarixi     = d.QetnameTarixi,
                    IsYeri            = d.IsYeri,
                    IcraQeyd          = d.IcraQeyd,
                    YaradanIcraciId   = isciId,
                    YaradilmaTarixi   = DateTime.Now
                };
                await repo.YaratAsync(entity);
                movcudDict[key] = entity;   // eyni faylda təkrar açar olarsa bir daha yaratma
            }
            pending.Add((entity, d.Zaminler));
        }

        if (!onizleme && (netice.YeniIs > 0 || netice.Yukseldilen > 0))
        {
            await _uow.YaddaSaxlaAsync();   // MehkemeIsi.Id-lər dolur

            var zrepo = _uow.Repository<MehkemeZamin>();
            int zCount = 0;
            foreach (var (e, zlist) in pending)
            {
                if (zaminliIsler.Contains(e.Id)) continue;   // bu işdə artıq zamin var — təkrar əlavə etmə
                foreach (var z in zlist)
                {
                    if (string.IsNullOrWhiteSpace(z.Ad)) continue;
                    await zrepo.YaratAsync(new MehkemeZamin
                    {
                        MehkemeIsiId     = e.Id,
                        Ad               = z.Ad.Trim(),
                        DogumTarixi      = z.DogumTarixi,
                        EmekHaqqiTutulma = z.EmekHaqqiTutulma,
                        DypSorgu         = z.DypSorgu,
                        AdinaSorgu       = z.AdinaSorgu,
                        EmlakaHebs       = z.EmlakaHebs,
                        Stop             = z.Stop,
                        IcraMemuru       = z.IcraMemuru,
                        IcraSonIsler     = z.IcraSonIsler,
                        IsYeri           = z.IsYeri,
                        IcraQeyd         = z.IcraQeyd,
                        YaradanIcraciId  = isciId,
                        YaradilmaTarixi  = DateTime.Now
                    });
                    zCount++;
                }
            }
            if (zCount > 0) await _uow.YaddaSaxlaAsync();
        }

        return netice;
    }

    // ── Məhkəmə fazası (yalnız litiqasiya sahələri) ──
    public async Task<bool> MehkemeFazaYenileAsync(int id, MehkemeIsiUpdateDto dto, int isciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        entity.Status        = dto.Status;
        entity.Nov           = dto.Nov;
        entity.BaslamaTarixi = dto.BaslamaTarixi;
        entity.QetnameTarixi = dto.QetnameTarixi;
        entity.Hakim         = dto.Hakim;
        entity.IsNomresi     = dto.IsNomresi;
        entity.Qerardad      = string.IsNullOrWhiteSpace(dto.Qerardad) ? null : dto.Qerardad.Trim();
        entity.Qeyd          = string.IsNullOrWhiteSpace(dto.Qeyd) ? null : dto.Qeyd.Trim();
        entity.YenileyenIcraciId = isciId;
        entity.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    // ── İcra fazası (yalnız icra/borclu sahələri) ──
    public async Task<bool> IcraFazaYenileAsync(int id, MehkemeIsiUpdateDto dto, int isciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        entity.IcraMemuru        = dto.IcraMemuru;
        entity.Qeydiyyati        = dto.Qeydiyyati;
        entity.EmekHaqqiMelumati = dto.EmekHaqqiMelumati;
        entity.IsYeri            = dto.IsYeri;
        entity.DypSorguTarixi    = dto.DypSorguTarixi;
        entity.AdinaSorgu        = dto.AdinaSorgu;
        entity.EmlakaHebs        = dto.EmlakaHebs;
        entity.Stop              = dto.Stop;
        entity.IcraSonIsler      = dto.IcraSonIsler;
        entity.IcraQeyd          = dto.IcraQeyd;
        entity.Zamin             = dto.Zamin;
        entity.QalanBorc         = dto.QalanBorc;
        entity.SonOdenisTarixi   = dto.SonOdenisTarixi;
        entity.DogumTarixi       = dto.DogumTarixi;
        entity.YenileyenIcraciId = isciId;
        entity.YenilenmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<bool> SilAsync(int id, int silenIsciId)
    {
        var entity = await _uow.Repository<MehkemeIsi>().IdIleGetirAsync(id);
        if (entity == null || entity.Silinib) return false;

        entity.Silinib         = true;
        entity.SilenIcraciId   = silenIsciId;
        entity.SilinmeTarixi   = DateTime.Now;

        await _uow.Repository<MehkemeIsi>().YenileAsync(entity);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<MehkemeMerhelesi> MerheleElavEtAsync(
        MehkemeMerheleCreateDto dto,
        IFormFile? fayl,
        string dmsRoot,
        int yaradanIsciId)
    {
        string? senedYolu = null;
        if (fayl != null && fayl.Length > 0)
        {
            var dir = Path.Combine(dmsRoot, "mehkeme");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(fayl.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            await using var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await fayl.CopyToAsync(fs);
            senedYolu = $"mehkeme/{fileName}";
        }

        var m = new MehkemeMerhelesi
        {
            MehkemeIsiId    = dto.MehkemeIsiId,
            MerheleTipi     = dto.MerheleTipi,
            Tarix           = dto.Tarix,
            Saat            = string.IsNullOrWhiteSpace(dto.Saat)        ? null : dto.Saat.Trim(),
            IsNomresi       = string.IsNullOrWhiteSpace(dto.IsNomresi)   ? null : dto.IsNomresi.Trim(),
            Hakim           = string.IsNullOrWhiteSpace(dto.Hakim)       ? null : dto.Hakim.Trim(),
            IcraciMemur     = string.IsNullOrWhiteSpace(dto.IcraciMemur) ? null : dto.IcraciMemur.Trim(),
            Qeyd            = string.IsNullOrWhiteSpace(dto.Qeyd)        ? null : dto.Qeyd.Trim(),
            SenedYolu       = senedYolu,
            YaradanIcraciId = yaradanIsciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<MehkemeMerhelesi>().YaratAsync(m);
        await _uow.YaddaSaxlaAsync();
        return m;
    }

    public async Task<bool> MerheleSilAsync(int merheleId, int silenIsciId)
    {
        var m = await _uow.Repository<MehkemeMerhelesi>().IdIleGetirAsync(merheleId);
        if (m == null || m.Silinib) return false;

        m.Silinib       = true;
        m.SilenIcraciId = silenIsciId;
        m.SilinmeTarixi = DateTime.Now;

        await _uow.Repository<MehkemeMerhelesi>().YenileAsync(m);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    // ── Məhkəmə xərci (bir işdə bir neçə ola bilər) ───────
    public async Task<int> XercElaveEtAsync(MehkemeXerciCreateDto dto, int isciId)
    {
        var x = new MehkemeXerci
        {
            MehkemeIsiId    = dto.MehkemeIsiId,
            Mebleg          = dto.Mebleg,
            Tarix           = dto.Tarix,
            Mehkeme         = string.IsNullOrWhiteSpace(dto.Mehkeme) ? null : dto.Mehkeme.Trim(),
            YaradanIcraciId = isciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<MehkemeXerci>().YaratAsync(x);
        await _uow.YaddaSaxlaAsync();
        return x.Id;
    }

    public async Task<bool> XercSilAsync(int xerciId, int isciId)
    {
        var x = await _uow.Repository<MehkemeXerci>().IdIleGetirAsync(xerciId);
        if (x == null || x.Silinib) return false;
        x.Silinib       = true;
        x.SilenIcraciId = isciId;
        x.SilinmeTarixi = DateTime.Now;
        await _uow.Repository<MehkemeXerci>().YenileAsync(x);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    // ── Sənədlər (işə birbaşa yüklənən) ───────────────────
    public async Task<int> SenedYukleAsync(MehkemeSenedCreateDto dto, IFormFile fayl, string dmsRoot, int isciId)
    {
        var dir = Path.Combine(dmsRoot, "mehkeme");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(fayl.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        await using (var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create))
            await fayl.CopyToAsync(fs);

        var s = new MehkemeSened
        {
            MehkemeIsiId    = dto.MehkemeIsiId,
            Ad              = string.IsNullOrWhiteSpace(dto.Ad)
                              ? Path.GetFileNameWithoutExtension(fayl.FileName)
                              : dto.Ad.Trim(),
            Novu            = dto.Novu,
            FaylYolu        = $"mehkeme/{fileName}",
            Tarix           = DateTime.Now,
            YaradanIcraciId = isciId,
            YaradilmaTarixi = DateTime.Now
        };
        await _uow.Repository<MehkemeSened>().YaratAsync(s);
        await _uow.YaddaSaxlaAsync();
        return s.Id;
    }

    public async Task<bool> SenedSilAsync(int senedId, int isciId)
    {
        var s = await _uow.Repository<MehkemeSened>().IdIleGetirAsync(senedId);
        if (s == null || s.Silinib) return false;
        s.Silinib       = true;
        s.SilenIcraciId = isciId;
        s.SilinmeTarixi = DateTime.Now;
        await _uow.Repository<MehkemeSened>().YenileAsync(s);
        await _uow.YaddaSaxlaAsync();
        return true;
    }

    public async Task<IList<Dictionary<string, string>>> OracleKreditlerGetirAsync(string qeydiyyatNomresi)
    {
        var ayar = await _sistemAyar.GetirAsync();
        if (ayar?.PidMehkemeSorguId == null)
            throw new InvalidOperationException("Sistem ayarlarında məhkəmə Oracle sorğusu seçilməyib.");

        var sorguResult = await _sorguService.IdIleGetirAsync(ayar.PidMehkemeSorguId.Value);
        if (!sorguResult.Success || sorguResult.Data is null || !sorguResult.Data.Aktiv)
            throw new InvalidOperationException("Oracle sorğusu tapılmadı və ya deaktivdir.");

        var sql = sorguResult.Data.SorguMetni.Replace("&nomre", qeydiyyatNomresi.Trim());
        var rows = await _oracle.SelectAsync(sql);
        return rows.Select(r => r.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "")).ToList();
    }

    // ── Yeni siyahı modeli ───────────────────────────────────────
    public async Task<MehkemeSiyahiResultDto> SiyahiGetirAsync()
    {
        var result = new MehkemeSiyahiResultDto();

        var ayar = await _sistemAyar.GetirAsync();
        if (ayar?.PidMehkemeSiyahiSorguId == null)
        {
            result.Konfiqurasiyali = false;
            result.Xeta = "Sistem ayarlarında məhkəmə siyahı sorğusu seçilməyib.";
            return result;
        }
        result.Konfiqurasiyali = true;

        var sorguResult = await _sorguService.IdIleGetirAsync(ayar.PidMehkemeSiyahiSorguId.Value);
        if (!sorguResult.Success || sorguResult.Data is null || !sorguResult.Data.Aktiv)
        {
            result.Xeta = "Oracle siyahı sorğusu tapılmadı və ya deaktivdir.";
            return result;
        }

        List<Dictionary<string, object?>> rows;
        try
        {
            rows = await _oracle.SelectAsync(sorguResult.Data.SorguMetni, maxRows: 5000);
        }
        catch (Exception ex)
        {
            result.Xeta = "Oracle sorğusu icra olunmadı: " + ex.Message;
            return result;
        }

        // SQL tracking qeydləri — kompozit açara görə (yalnız oxuma → AsNoTracking, fixup-dan qaçmaq üçün)
        var izlenenler = await _uow.Repository<MehkemeIsi>()
            .Query()
            .Where(x => !x.Silinib)
            .Include(x => x.Merheleler)
            .AsNoTracking()
            .ToListAsync();

        var map = new Dictionary<string, MehkemeIsi>(StringComparer.OrdinalIgnoreCase);
        foreach (var rec in izlenenler)
            map[Acar(rec.QeydiyyatNomresi, rec.KreditSubHesab)] = rec;

        foreach (var row in rows)
        {
            var satir = new MehkemeKreditSatirDto
            {
                KreditHesabi       = (GetStr(row, "kredit_hesabi", "licschkre", "hesab") ?? "").Trim(),
                Ks                 = (GetStr(row, "ks", "subschkre", "sub") ?? "").Trim(),
                Region             = GetStr(row, "region"),
                BorcluAd           = GetStr(row, "adi", "ad", "borclu_ad", "name_regnom"),
                TamQaliq           = GetDec(row, "tam_qaliq", "tamqaliq", "tam_qaliq_meblegi"),
                Qaliq              = GetDec(row, "qaliq", "summa"),
                VkQaliq            = GetDec(row, "vk_qaliq", "summa_19"),
                FaizMeblegi        = GetDec(row, "faiz_meblegi"),
                VkFaizMeblegi      = GetDec(row, "vk_faiz_meblegi"),
                SonEmeliyyatTarixi = GetStr(row, "son_emel_tarixi"),
                Status             = GetStr(row, "item_01"),
                SonFealiyyet       = GetStr(row, "item_10"),
                Faiz               = GetStr(row, "faiz"),
                Ehtiyat            = GetStr(row, "ehtiyat"),
                KreditinMeblegi    = GetDec(row, "kreditin_meblegi", "summakre"),
                VerilmeTarixi      = GetStr(row, "verilme_tarixi"),
                OverdueGun         = GetStr(row, "real_overdue_day"),
                Telefon            = GetStr(row, "phone_numbers"),
                Mobil              = GetStr(row, "mobile_numbers"),
                KreditinNovu       = GetStr(row, "kreditin_novu"),
                GirovunNovu        = GetStr(row, "girovun_novu"),
                Sektor             = GetStr(row, "sektor_name", "sektor"),
                Item02             = GetStr(row, "item_02"),
                DogumTarixi        = GetStr(row, "dogum_tarixi"),
                Zaminler           = ParseZaminler(row)
            };

            if (map.TryGetValue(Acar(satir.KreditHesabi, satir.Ks), out var rec))
            {
                satir.IsAcilib     = true;
                satir.MehkemeIsiId = rec.Id;
                satir.Qerardad     = rec.Qerardad;
                satir.MerheleSayi  = rec.Merheleler.Count(m => !m.Silinib);
            }

            result.Satirlar.Add(satir);
        }

        return result;
    }

    // ── Excel export üçün: siyahı sorğusunun TAM xam nəticəsi (bütün sütunlar/sətirlər) ──
    public async Task<OracleNetice> SiyahiXamGetirAsync()
    {
        var ayar = await _sistemAyar.GetirAsync();
        if (ayar?.PidMehkemeSiyahiSorguId == null)
            throw new InvalidOperationException("Sistem ayarlarında məhkəmə siyahı sorğusu seçilməyib.");

        var sorguResult = await _sorguService.IdIleGetirAsync(ayar.PidMehkemeSiyahiSorguId.Value);
        if (!sorguResult.Success || sorguResult.Data is null || !sorguResult.Data.Aktiv)
            throw new InvalidOperationException("Oracle siyahı sorğusu tapılmadı və ya deaktivdir.");

        // Tam dump — bütün sətirlər çıxsın deyə yüksək limit (grid 5000 ilə məhdudlaşır)
        return await _oracle.SelectXamAsync(sorguResult.Data.SorguMetni, maxRows: 100000);
    }

    public async Task<int> QerardadYazAsync(MehkemeKreditAcarDto acar, string? qerardad, int isciId)
    {
        var rec = await GetOrCreateAsync(acar, isciId);
        rec.Qerardad          = string.IsNullOrWhiteSpace(qerardad) ? null : qerardad.Trim();
        rec.YenileyenIcraciId = isciId;
        rec.YenilenmeTarixi   = DateTime.Now;
        await _uow.Repository<MehkemeIsi>().YenileAsync(rec);
        await _uow.YaddaSaxlaAsync();
        return rec.Id;
    }

    public async Task<MehkemeIsi> IsAchAsync(MehkemeKreditAcarDto acar, int isciId)
        => await GetOrCreateAsync(acar, isciId);

    private async Task<MehkemeIsi> GetOrCreateAsync(MehkemeKreditAcarDto acar, int isciId)
    {
        var hesab = (acar.KreditHesabi ?? "").Trim();
        var ks    = (acar.Ks ?? "").Trim();

        var rec = await _uow.Repository<MehkemeIsi>()
            .Query()
            .FirstOrDefaultAsync(x => !x.Silinib
                && x.QeydiyyatNomresi == hesab
                && (x.KreditSubHesab ?? "") == ks);

        if (rec != null) return rec;

        rec = new MehkemeIsi
        {
            QeydiyyatNomresi = hesab,
            KreditSubHesab   = ks,
            BorcluAd         = string.IsNullOrWhiteSpace(acar.BorcluAd) ? "(naməlum)" : acar.BorcluAd.Trim(),
            EsasBorc         = acar.EsasBorc,
            Nov              = MehkemeIsiNov.Diger,
            Status           = MehkemeIsiStatus.Hazirlanir,
            YaradanIcraciId  = isciId,
            YaradilmaTarixi  = DateTime.Now
        };
        await _uow.Repository<MehkemeIsi>().YaratAsync(rec);
        await _uow.YaddaSaxlaAsync();
        return rec;
    }

    private static string Acar(string? hesab, string? ks)
        => $"{(hesab ?? "").Trim()}|{(ks ?? "").Trim()}";

    private static List<MehkemeZaminDto> ParseZaminler(Dictionary<string, object?> row)
    {
        var list = new List<MehkemeZaminDto>();
        for (int i = 1; i <= 5; i++)
        {
            var ad = GetStr(row, $"zamin_{i}_ad");
            if (string.IsNullOrWhiteSpace(ad)) continue;
            list.Add(new MehkemeZaminDto
            {
                Ad          = ad,
                Fin         = GetStr(row, $"zamin_{i}_fin"),
                DogumTarixi = GetStr(row, $"zamin_{i}_dogum_tarixi"),
                DogumYeri   = GetStr(row, $"zamin_{i}_dogum_yeri"),
                Olke        = GetStr(row, $"zamin_{i}_olke"),
                Telefon     = GetStr(row, $"zamin_{i}_telefon"),
                Unvan       = GetStr(row, $"zamin_{i}_unvan"),
                Gelir       = GetStr(row, $"zamin_{i}_gelir"),
                BorcYuku    = GetStr(row, $"zamin_{i}_borc_yuku")
            });
        }
        return list;
    }

    private static string? GetStr(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var kv in row)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    var s = kv.Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s)) return s;
                }
        return null;
    }

    private static decimal? GetDec(Dictionary<string, object?> row, params string[] keys)
    {
        foreach (var key in keys)
            foreach (var kv in row)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                {
                    switch (kv.Value)
                    {
                        case decimal dm: return dm;
                        case double db:  return (decimal)db;
                        case float fl:   return (decimal)fl;
                        case int it:     return it;
                        case long lg:    return lg;
                    }
                    var s = kv.Value.ToString()?.Trim();
                    if (string.IsNullOrEmpty(s)) return null;
                    var d = ParseFlexible(s.Replace(" ", "").Replace("₼", ""));
                    if (d.HasValue) return d;
                }
        return null;
    }

    // Həm "16253.05", həm "16253,05", həm "16.253,05" formatını düzgün oxuyur
    private static decimal? ParseFlexible(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        bool hasDot = s.Contains('.');
        bool hasComma = s.Contains(',');
        if (hasDot && hasComma)
        {
            if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                s = s.Replace(".", "").Replace(",", ".");   // vergül onluqdur
            else
                s = s.Replace(",", "");                       // nöqtə onluqdur
        }
        else if (hasComma)
        {
            s = s.Replace(",", ".");
        }
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
