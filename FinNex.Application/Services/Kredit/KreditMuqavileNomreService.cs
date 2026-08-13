using System.Globalization;
using FinNex.Application.DTOs.Kredit.Muqavile;
using FinNex.Application.DTOs.Mektub;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Mektub;
using FinNex.Domain.Entities.Kredit;
using Microsoft.Extensions.Configuration;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// Müqavilə nömrələri və girova düşmə (BTİ) məktubunun qeydiyyatı.
///
/// ✅ ORACLE-A ARTIQ HEÇ NƏ YAZILMIR. Əvvəl bu servis iki Oracle cədvəlinə yazırdı
/// (`odb.muqavile_nomreleri` UPDATE, `odb.xaric_mektub` INSERT) — hər ikisi FinNex-ə
/// köçürüldü:
///   • sayğaclar → `MuqavileSayghaci` (IMuqavileSayghacService)
///   • məktub    → `XaricMektub`      (IXaricMektubService)
/// Beləliklə CLAUDE.md-dəki "Oracle YALNIZ SELECT" qaydasının istisnası qalmadı.
///
/// KreditMuqavile:NomreYaz = false (default) → preview, HEÇ NƏ yazılmır (nə sayğac,
/// nə məktub). true → nömrələr real ayrılır və məktub jurnala düşür.
///
/// Nömrə semantikası BMI-nin Menzil.cs (muq_no_at) məntiqindən götürülüb:
///   KrZaminlik → kredit müqaviləsi nömrəsi ({k_mno})
///   KrMenzil   → ipoteka müqaviləsi nömrəsi ({i_mno})
///   KrZaminler → zaminlik running nömrəsi ({zmno1})
///
/// İL: nömrə CARİ TƏQVİM İLİNİN sayğacından verilir (BMI-də də SYSDATE idi).
/// Yeni ildə həmin il üçün sayğac sətri olmadığı üçün nömrələmə 1-dən başlayır.
/// </summary>
public class KreditMuqavileNomreService : IKreditMuqavileNomreService
{
    private readonly bool _nomreYaz;
    private readonly IMuqavileSayghacService _saygac;
    private readonly IXaricMektubService _xaricMektub;

    public KreditMuqavileNomreService(
        IConfiguration config,
        IMuqavileSayghacService saygac,
        IXaricMektubService xaricMektub)
    {
        _nomreYaz    = config.GetValue("KreditMuqavile:NomreYaz", false);
        _saygac      = saygac;
        _xaricMektub = xaricMektub;
    }

    public async Task<MenzilNomreleriDto> MenzilNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default)
    {
        var il = DateTime.Now.Year;

        var kreditNo  = await _saygac.NomreAyirAsync(MuqavileNomreNovu.KrZaminlik, il, _nomreYaz, ct);
        var ipotekaNo = await _saygac.NomreAyirAsync(MuqavileNomreNovu.KrMenzil,   il, _nomreYaz, ct);
        var zaminNolar = await _saygac.NomreAyirAsync(
            MuqavileNomreNovu.KrZaminler, il, zaminSayi, _nomreYaz, ct);

        return new MenzilNomreleriDto
        {
            KreditNo   = kreditNo,
            IpotekaNo  = ipotekaNo,
            ZaminNolar = zaminNolar,
            Yazildi    = _nomreYaz
        };
    }

    public async Task<MenzilNomreleriDto> ZaminlikNomreleriAyirAsync(int zaminSayi, CancellationToken ct = default)
    {
        var il = DateTime.Now.Year;

        // Yalnız zaminlik kreditinin sayğacları — ipoteka (KrMenzil) TOXUNULMUR,
        // çünki zaminlik kreditində ipoteka yoxdur.
        var kreditNo = await _saygac.NomreAyirAsync(MuqavileNomreNovu.KrZaminlik, il, _nomreYaz, ct);
        var zaminNolar = await _saygac.NomreAyirAsync(
            MuqavileNomreNovu.KrZaminler, il, zaminSayi, _nomreYaz, ct);

        return new MenzilNomreleriDto
        {
            KreditNo   = kreditNo,
            IpotekaNo  = 0,
            ZaminNolar = zaminNolar,
            Yazildi    = _nomreYaz
        };
    }

    // Girova düşmə (BTİ) məktubu — FinNex məktub jurnalına yazılır.
    // Qeyd də, nömrə də jurnal səhifəsindəki ilə eyni yoldan gəlir
    // (XaricMektubService), yəni nömrələmə TƏK yerdən idarə olunur.
    public async Task<string> MektubQeydiyyatiAsync(DateTime tarix, int yaradanUserId, CancellationToken ct = default)
    {
        var il = tarix.Year;

        if (!_nomreYaz)
        {
            // Preview — heç nə yazılmır, yalnız növbəti nömrə göstərilir
            var novbeti = await _xaricMektub.NovbetiNomreAsync(il);
            return $"{il}-{novbeti.ToString(CultureInfo.InvariantCulture)}";
        }

        var netice = await _xaricMektub.YaratAsync(new XaricMektubCreateDto
        {
            Tarix   = tarix,
            GonYer  = "Mənzil",
            QisaMez = "mənzil gir sal"
        }, yaradanUserId);

        if (!netice.Success)
            throw new InvalidOperationException($"Məktub qeydiyyatı alınmadı: {netice.Message}");

        return $"{il}-{netice.Data.ToString(CultureInfo.InvariantCulture)}";
    }
}
