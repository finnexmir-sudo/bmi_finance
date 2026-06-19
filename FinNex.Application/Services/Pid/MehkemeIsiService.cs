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

        return list.Select(x => new MehkemeIsiListDto
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
            Hakim             = x.Merheleler.Where(m => !m.Silinib && !string.IsNullOrWhiteSpace(m.Hakim))
                                            .OrderByDescending(m => m.Tarix).Select(m => m.Hakim).FirstOrDefault(),
            Teminat           = x.KreditNovuMetn,
            MerheleCount      = x.Merheleler.Count(m => !m.Silinib),
            YaradilmaTarixi   = x.YaradilmaTarixi
        }).ToList();
    }

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
            var key = (oz.Fin ?? oz.Ad ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (movcud.Any(m => ((m.Fin ?? m.Ad ?? "").Trim().ToLowerInvariant()) == key)) continue;

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
    public async Task<(int isSayi, int merheleSayi)> ExcelImportAsync(IList<MehkemeCedvelImportDto> rows, int isciId)
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

            var isNo = string.IsNullOrWhiteSpace(r.MehkemeIsNomresi) ? null : r.MehkemeIsNomresi.Trim();
            // QeydiyyatNomresi NVARCHAR(20) NOT NULL — qısa marker saxla (truncate riski yox);
            // tam iş № onsuz da MehkemeSenedi-də qalır (nvarchar max).
            var qeydNo = (isNo != null && isNo.Length <= 20) ? isNo
                       : (r.Sira.HasValue ? $"ARX-{r.Sira}" : "ARX");
            var entity = new MehkemeIsi
            {
                QeydiyyatNomresi       = qeydNo,
                BorcluAd               = ad,
                Sira                   = r.Sira,
                KreditNovuMetn         = string.IsNullOrWhiteSpace(r.GirovunNovu) ? null : r.GirovunNovu.Trim(),
                MehkemeSenedi          = isNo,
                BaslamaTarixi          = r.MehkemeyeVerilmeTarixi,
                MehkemeyeVerilmeTarixi = r.MehkemeyeVerilmeTarixi,
                Status                 = MehkemeIsiStatus.Mehkemede,   // Excel arxivi → məhkəmə mərhələsində
                Nov                    = MehkemeIsiNov.Diger,
                YaradanIcraciId        = isciId,
                YaradilmaTarixi        = DateTime.Now
            };

            foreach (var ic in r.Iclaslar)
            {
                if (ic.Tarix == null && string.IsNullOrWhiteSpace(ic.Saat)) continue;
                entity.Merheleler.Add(new MehkemeMerhelesi
                {
                    MerheleTipi     = MerheleTipi.MehkemeIclasi,
                    Tarix           = ic.Tarix ?? r.MehkemeyeVerilmeTarixi ?? DateTime.Today,
                    Qeyd            = string.IsNullOrWhiteSpace(ic.Saat) ? null : "Saat: " + ic.Saat.Trim(),
                    YaradanIcraciId = isciId,
                    YaradilmaTarixi = DateTime.Now
                });
                merheleSayi++;
            }

            await repo.YaratAsync(entity);
            isSayi++;
        }

        if (isSayi > 0) await _uow.YaddaSaxlaAsync();
        return (isSayi, merheleSayi);
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
