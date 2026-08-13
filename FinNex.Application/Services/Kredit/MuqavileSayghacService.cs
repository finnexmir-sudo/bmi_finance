using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Domain.Entities.Kredit;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// Müqavilə nömrə sayğacları — FinNex bazasında.
/// Şablon layihədəki `EmrService.NovbetiNomreAsync` ilə eynidir (EmrSayghaci):
/// (Novu, Il) sətri tapılır, SonNomre artırılır, yeni dəyər qaytarılır.
///
/// KONKURENSİYA — AÇIQ QEYD: oxu-artır-yaz ardıcıllığında kilid YOXDUR (EmrSayghaci
/// də belədir). İki sorğu EYNİ ANDA gəlsə nəzəri olaraq eyni nömrəni ala bilər.
/// Müqavilə hazırlanması əl ilə, gündə bir neçə dəfə edilən əməliyyatdır və BMI artıq
/// nömrə vermir — praktiki risk yoxdur. Sərtləşdirmək lazım gələrsə yol: entity-yə
/// RowVersion əlavə edib DbUpdateConcurrencyException-da təkrar cəhd etmək.
/// </summary>
public class MuqavileSayghacService : IMuqavileSayghacService
{
    private readonly IUnitOfWork _uow;

    public MuqavileSayghacService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // Ekranda görünən ad + BMI sütun qarşılığı (bir yerdə saxlanılır ki, ad
    // dəyişəndə köçürmə və idarəetmə ekranı ayrılmasın).
    public static readonly IReadOnlyDictionary<MuqavileNomreNovu, (string Ad, string OracleSutun, bool OracleNovbetiSaxlayir)> Novler =
        new Dictionary<MuqavileNomreNovu, (string, string, bool)>
        {
            [MuqavileNomreNovu.KrZaminlik]  = ("Kredit müqaviləsi",      "KR_ZAMINLIK",  true),
            [MuqavileNomreNovu.KrMenzil]    = ("İpoteka müqaviləsi",     "KR_MENZIL",    true),
            // kr_zaminler BMI-də SONUNCU verilmiş nömrədir (kod `kr_zaminler + i` işlədir),
            // digərləri isə NÖVBƏTİ nömrəni saxlayır — köçürmədə fərq buradan gəlir.
            [MuqavileNomreNovu.KrZaminler]  = ("Zaminlik müqaviləsi",    "KR_ZAMINLER",  false),
            [MuqavileNomreNovu.KrSerencam]  = ("Sərəncam",               "KR_SERENCAM",  true),
            [MuqavileNomreNovu.KrAvtomobil] = ("Avtomobil girovu",       "KR_AVTOMOBIL", true),
            [MuqavileNomreNovu.Depozit]     = ("Depozit müqaviləsi",     "DEPOZIT",      true),
            [MuqavileNomreNovu.KrKart]      = ("Kart krediti",           "KR_KART",      true),
            [MuqavileNomreNovu.KartZamin]   = ("Kart krediti zaminliyi", "KART_ZAMIN",   true),
            [MuqavileNomreNovu.KrQizil]     = ("Qızıl girovu",           "KR_QIZIL",     true),
        };

    public static string Ad(MuqavileNomreNovu novu) =>
        Novler.TryGetValue(novu, out var m) ? m.Ad : novu.ToString();

    public async Task<int> NomreAyirAsync(MuqavileNomreNovu novu, int il, bool yaz,
        CancellationToken ct = default)
    {
        var nolar = await NomreAyirAsync(novu, il, 1, yaz, ct);
        return nolar.Count > 0 ? nolar[0] : 1;
    }

    public async Task<List<int>> NomreAyirAsync(MuqavileNomreNovu novu, int il, int adet, bool yaz,
        CancellationToken ct = default)
    {
        if (adet <= 0) return new List<int>();

        var repo = _uow.Repository<MuqavileSayghaci>();

        var saygac = (await repo.HamisiniGetirAsync(
                x => x.Novu == novu && x.Il == il && !x.Silinib))
            .FirstOrDefault();

        var son = saygac?.SonNomre ?? 0;
        if (son < 0) son = 0;

        var nolar = Enumerable.Range(1, adet).Select(i => son + i).ToList();

        // Preview — sayğaca toxunulmur
        if (!yaz) return nolar;

        if (saygac == null)
        {
            await repo.YaratAsync(new MuqavileSayghaci
            {
                Novu     = novu,
                Il       = il,
                SonNomre = son + adet
            });
        }
        else
        {
            saygac.SonNomre     = son + adet;
            saygac.YenilenmeTarixi = DateTime.Now;
            await repo.YenileAsync(saygac);
        }

        await _uow.YaddaSaxlaAsync();
        return nolar;
    }

    public async Task<int> SonNomreAsync(MuqavileNomreNovu novu, int il)
    {
        var saygac = (await _uow.Repository<MuqavileSayghaci>().HamisiniGetirAsync(
                x => x.Novu == novu && x.Il == il && !x.Silinib, izlemeden: true))
            .FirstOrDefault();

        return saygac?.SonNomre ?? 0;
    }

    public async Task SonNomreTeyinEtAsync(MuqavileNomreNovu novu, int il, int sonNomre,
        int? istifadeciId = null)
    {
        if (sonNomre < 0) sonNomre = 0;

        var repo = _uow.Repository<MuqavileSayghaci>();
        var saygac = (await repo.HamisiniGetirAsync(
                x => x.Novu == novu && x.Il == il && !x.Silinib))
            .FirstOrDefault();

        if (saygac == null)
        {
            await repo.YaratAsync(new MuqavileSayghaci
            {
                Novu            = novu,
                Il              = il,
                SonNomre        = sonNomre,
                YaradanIcraciId = istifadeciId
            });
        }
        else
        {
            saygac.SonNomre          = sonNomre;
            saygac.YenileyenIcraciId = istifadeciId;
            saygac.YenilenmeTarixi   = DateTime.Now;
            await repo.YenileAsync(saygac);
        }

        await _uow.YaddaSaxlaAsync();
    }

    public async Task<List<MuqavileSayghacDto>> HamisiniGetirAsync()
    {
        var hamisi = await _uow.Repository<MuqavileSayghaci>()
            .HamisiniGetirAsync(x => !x.Silinib, izlemeden: true);

        return hamisi
            .OrderByDescending(x => x.Il).ThenBy(x => x.Novu)
            .Select(x => new MuqavileSayghacDto
            {
                Novu        = x.Novu,
                NovuAdi     = Ad(x.Novu),
                OracleSutun = Novler.TryGetValue(x.Novu, out var m) ? m.OracleSutun : "",
                Il          = x.Il,
                SonNomre    = x.SonNomre
            })
            .ToList();
    }
}
