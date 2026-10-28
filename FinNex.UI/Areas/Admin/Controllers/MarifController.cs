using FinNex.Application.DTOs.Marif;
using FinNex.Application.Interfaces.AI;
using FinNex.Application.Interfaces.Communication;
using FinNex.Application.Interfaces.Marif;
using FinNex.Domain;
using FinNex.Domain.Entities.AI;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Marif;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.Admin.Controllers;

/// <summary>
/// Daxili Maarifləndirmə — pilot (27.10.2026). İdarəetmə tam Admin əlindədir:
/// qayda seç/yapışdır → AI sual hazırlayır → Admin redaktə/təsdiq edir →
/// işçi seçib göndərir. Avtomatik cədvəl/təkrar bu mərhələdə YOXDUR.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class MarifController : Controller
{
    private readonly IMarifService _marif;
    private readonly IMarifAiService _ai;
    private readonly IUnitOfWork _uow;
    private readonly IAttachmentTextExtractor _extractor;

    private static readonly string[] IcazaliFaylTipleri = { ".pdf", ".doc", ".docx" };
    private const long MaxFaylOlcusu = 20 * 1024 * 1024; // 20 MB

    public MarifController(
        IMarifService marif,
        IMarifAiService ai,
        IUnitOfWork uow,
        IAttachmentTextExtractor extractor)
    {
        _marif = marif;
        _ai = ai;
        _uow = uow;
        _extractor = extractor;
    }

    // ── GET /Admin/Marif ─────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Daxili Maarifləndirmə";
        var setler = await _marif.HamisiniGetirAsync();
        return View(setler);
    }

    // ── GET /Admin/Marif/Yarat (yeni) və ya /Admin/Marif/Yarat?id=5 (qaralamanı bərpa et) ──
    // 28.10.2026 — "Suallar hazırla" artıq generasiya ANINDA Qaralama kimi
    // avtomatik yadda saxlanır (bax aşağı SuallarHazirla) ki, admin naviqasiya
    // etsə (yaxud pəncərəni bağlasa) pullu AI çağırışının nəticəsi İTMƏSİN —
    // bu action həmin qaralamanı yenidən açıb redaktəyə davam etməyə imkan verir.
    public async Task<IActionResult> Yarat(int? id)
    {
        ViewData["Title"] = "Yeni Sual Dəsti";
        await FillMenbeSiyahilariAsync();

        if (id is > 0)
        {
            var set = await _marif.DetailGetirAsync(id.Value);
            if (set == null)
            {
                TempData["Error"] = "Sual dəsti tapılmadı.";
                return RedirectToAction(nameof(Index));
            }
            if (set.Status != MarifSetStatusu.Qaralama)
            {
                TempData["Error"] = "Göndərilmiş sual dəsti redaktə edilə bilməz — nəticələr artıq işçiyə bağlıdır.";
                return RedirectToAction(nameof(Detail), new { id = id.Value });
            }

            ViewData["Title"] = $"Qaralamanı redaktə et — {set.Basliq}";
            ViewBag.MovcudSet = set;
        }

        return View();
    }

    private async Task FillMenbeSiyahilariAsync()
    {
        var daxili = await _uow.Repository<HRDaxiliQayda>().Query().AsNoTracking()
            .Where(x => x.Aktiv)
            .OrderBy(x => x.Kateqoriya).ThenBy(x => x.Ad)
            .ToListAsync();
        ViewBag.DaxiliQaydalar = daxili
            .Select(x => new MarifMenbeSecimDto { Id = x.Id, Etiket = $"[{x.Kod}] {x.Ad} ({x.Kateqoriya})" })
            .ToList();

        var qanun = await _uow.Repository<HRQanunFayl>().Query().AsNoTracking()
            .OrderBy(x => x.Kateqoriya).ThenBy(x => x.Ad)
            .ToListAsync();
        ViewBag.QanunFayllar = qanun
            .Select(x => new MarifMenbeSecimDto { Id = x.Id, Etiket = $"{x.Ad} ({x.Kateqoriya})" })
            .ToList();
    }

    // ── POST /Admin/Marif/SuallarHazirla (AJAX) ───────────────────────
    // Mənbəni üç yerdən birindən tapır: Daxili Qayda / Qanun Faylı / birbaşa mətn-yükləmə.
    //
    // 28.10.2026, KRİTİK — real hadisə: admin "Suallar hazırla"ya basdı, AI
    // çağırışı (PULLU) uğurla bitdi, admin cavab gəlməmiş başqa səhifəyə
    // keçdi, qayıdanda "Yeni Sual Dəsti" forması boş/ilkin vəziyyətdə idi —
    // generasiyanın NƏTİCƏSİ heç yerə yazılmamışdı, yalnız brauzerin JS
    // yaddaşında (`state.suallar`) var idi, səhifədən çıxanda o da öldü.
    // Pul artıq xərclənmişdi, nəticə isə bərpa oluna bilməzdi.
    //
    // Həll: AI uğurla qayıdan kimi (brauzerə göndərmədən ƏVVƏL) nəticə dərhal
    // Qaralama statusunda `MarifSualSeti` kimi YAZILIR. `existingSetId`
    // veriləndə (admin artıq bir dəfə hazırlayıb indi "Yenidən hazırla"
    // etmişdi) yeni sətir yaratmaq əvəzinə HƏMİN qaralama YENİLƏNİR — hər
    // "Suallar hazırla" klikinin öz-özlüyündə ayrıca qaralama qoyması
    // qarşısı alınır. Admin "Yadda saxla"ya keçmədən naviqasiya etsə belə,
    // qaralama artıq bazadadır — "Daxili Maarifləndirmə" siyahısında görünür,
    // `Yarat?id=` ilə bərpa edilə bilər.
    [HttpPost]
    public async Task<IActionResult> SuallarHazirla(int? hrDaxiliQaydaId, int? hrQanunFaylId,
        string? menbeMetn, int sualSayi, IFormFile? fayl, string? sualNovu,
        int? existingSetId, string? basliq)
    {
        // 27.10.2026 — generasiya anında tip seçimi: "coxsecimli"/"aciq" tək tipi
        // sabitləyir, hər hansı başqa dəyər (boş, "qarisiq") qarışıq (default) deməkdir.
        MarifSualTipi? yalnizTip = sualNovu?.Trim().ToLowerInvariant() switch
        {
            "coxsecimli" => MarifSualTipi.CoxSecimli,
            "aciq" => MarifSualTipi.Acig,
            _ => null
        };

        string? metn = null;
        string? tempPath = null;

        try
        {
            if (hrDaxiliQaydaId is > 0)
            {
                var q = await _uow.Repository<HRDaxiliQayda>().IdIleGetirAsync(hrDaxiliQaydaId.Value);
                if (q == null) return Json(new { ok = false, xeta = "Daxili qayda tapılmadı." });
                metn = q.Mezmun;
            }
            else if (hrQanunFaylId is > 0)
            {
                var f = await _uow.Repository<HRQanunFayl>().IdIleGetirAsync(hrQanunFaylId.Value);
                if (f == null) return Json(new { ok = false, xeta = "Qanun faylı tapılmadı." });
                metn = f.MetnContent;
            }
            else if (fayl != null && fayl.Length > 0)
            {
                var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
                if (!IcazaliFaylTipleri.Contains(ext))
                    return Json(new { ok = false, xeta = "Yalnız PDF, DOC, DOCX fayllar qəbul olunur." });
                if (fayl.Length > MaxFaylOlcusu)
                    return Json(new { ok = false, xeta = "Fayl ölçüsü 20 MB-dan çox ola bilməz." });

                tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{ext}");
                await using (var fs = System.IO.File.Create(tempPath))
                    await fayl.CopyToAsync(fs);

                metn = _extractor.Extract(tempPath, fayl.ContentType);
                if (string.IsNullOrWhiteSpace(metn))
                    return Json(new { ok = false, xeta = "Fayldan mətn çıxarıla bilmədi — skan edilmiş PDF-lər dəstəklənmir." });
            }
            else if (!string.IsNullOrWhiteSpace(menbeMetn))
            {
                metn = menbeMetn;
            }
            else
            {
                return Json(new { ok = false, xeta = "Mənbə seçilməyib — qayda seçin, fayl yükləyin və ya mətn yapışdırın." });
            }

            var netice = await _ai.SuallarHazirlaAsync(metn ?? "", sualSayi <= 0 ? 5 : sualSayi, yalnizTip);
            if (!netice.Success)
                return Json(new { ok = false, xeta = netice.Message });

            // ── Pullu nəticəni DƏRHAL Qaralama kimi yaz ──────────────────
            var saxlaDto = new MarifSualSetiYaratDto
            {
                Basliq = !string.IsNullOrWhiteSpace(basliq)
                    ? basliq!.Trim()
                    : $"(Qaralama — {DateTime.Now:dd.MM.yyyy HH:mm}, başlıq əlavə edin)",
                HRDaxiliQaydaId = hrDaxiliQaydaId,
                HRQanunFaylId = hrQanunFaylId,
                MenbeMetn = metn,
                Suallar = netice.Data!
            };

            int? setId = null;
            string? saxlamaXeberdarligi = null;
            if (existingSetId is > 0)
            {
                var yenileNetice = await _marif.SetYenileAsync(existingSetId.Value, saxlaDto);
                if (yenileNetice.Success) setId = existingSetId.Value;
                else saxlamaXeberdarligi = yenileNetice.Message;
            }
            else
            {
                var yaratNetice = await _marif.SetYaratAsync(saxlaDto);
                if (yaratNetice.Success) setId = yaratNetice.Data;
                else saxlamaXeberdarligi = yaratNetice.Message;
            }

            // saxlamaXeberdarligi doluysa (çox nadir — məs. AI-nin özü validasiyadan
            // keçməyən sahə qaytarıb) suallar YENƏ DƏ brauzerə göndərilir ki, admin
            // onları itirməsin — amma JS açıq xəbərdarlıq göstərməlidir ki, bu dəfə
            // avtomatik qaralama YAZILMAYIB, səhifədən çıxmadan özü yadda saxlasın.
            return Json(new { ok = true, suallar = netice.Data, menbeMetn = metn, setId, saxlamaXeberdarligi });
        }
        finally
        {
            if (tempPath != null && System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    // ── POST /Admin/Marif/Yarat — redaktə olunmuş sualları AJAX (JSON) ilə yadda saxla ─
    // JSON body — dinamik (əlavə/sil) sual siyahısını adi form-array indeksləməsi ilə
    // göndərmək kövrək olardı, ona görə fetch+JSON işlədilir (RehberHrLegv ilə eyni
    // konvensiya — JS fetch-ə CSRF token header əlavə edilmir, Admin rolu qoruyur).
    [HttpPost]
    public async Task<IActionResult> Yarat([FromBody] MarifSualSetiYaratDto dto)
    {
        var netice = await _marif.SetYaratAsync(dto);
        if (!netice.Success)
            return Json(new { ok = false, xeta = netice.Message });

        return Json(new { ok = true, redirectUrl = Url.Action(nameof(Detail), new { id = netice.Data }) });
    }

    // ── POST /Admin/Marif/SetYenile/5 — avtomatik yadda saxlanmış qaralamanı
    //    yekun (admin-in redaktə etdiyi) halı ilə yeniləyir ──────────────────
    // 28.10.2026 — SuallarHazirla indi hər generasiyada bir Qaralama yazır;
    // "Yadda saxla" düyməsi artıq YENİ sətir yaratmır (bu, "Yarat" action-ının
    // işi idi), mövcud qaralamanın ÜZƏRİNƏ yazır — yoxsa hər "Suallar hazırla"
    // + "Yadda saxla" cütü iki ayrı sətir qoyardı.
    [HttpPost]
    public async Task<IActionResult> SetYenile(int id, [FromBody] MarifSualSetiYaratDto dto)
    {
        var netice = await _marif.SetYenileAsync(id, dto);
        if (!netice.Success)
            return Json(new { ok = false, xeta = netice.Message });

        return Json(new { ok = true, redirectUrl = Url.Action(nameof(Detail), new { id }) });
    }

    // ── GET /Admin/Marif/Detail/5 ──────────────────────────────────────
    public async Task<IActionResult> Detail(int id)
    {
        var set = await _marif.DetailGetirAsync(id);
        if (set == null)
        {
            TempData["Error"] = "Sual dəsti tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = set.Basliq;
        return View(set);
    }

    // ── POST /Admin/Marif/SetSil/5 ─────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetSil(int id)
    {
        var netice = await _marif.SetSilAsync(id);
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── GET /Admin/Marif/IsciSec/5 ──────────────────────────────────────
    public async Task<IActionResult> IsciSec(int id, string? axtaris)
    {
        var set = await _marif.DetailGetirAsync(id);
        if (set == null)
        {
            TempData["Error"] = "Sual dəsti tapılmadı.";
            return RedirectToAction(nameof(Index));
        }

        var sorgu = _uow.Repository<Isci>().Query()
            .Where(x => x.Status == IsciStatus.Aktiv);

        if (!string.IsNullOrWhiteSpace(axtaris))
        {
            var a = axtaris.Trim();
            sorgu = sorgu.Where(x => x.Ad.Contains(a) || x.Soyad.Contains(a));
        }

        var isciler = await sorgu
            .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
            .Select(x => new MarifIsciSecimDto { Id = x.Id, Ad = x.Ad, Soyad = x.Soyad })
            .ToListAsync();

        var artiqGonderilen = set.Tesyinatlar.Select(t => t.IsciId).ToHashSet();

        ViewBag.SetId = id;
        ViewBag.SetBasliq = set.Basliq;
        ViewBag.Axtaris = axtaris;
        ViewBag.ArtiqGonderilen = artiqGonderilen;

        return View(isciler);
    }

    // ── POST /Admin/Marif/Gonder ───────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Gonder(int setId, List<int> isciIdler)
    {
        var netice = await _marif.GonderAsync(new MarifGonderDto { SualSetiId = setId, IsciIdler = isciIdler ?? new List<int>() });
        TempData[netice.Success ? "Success" : "Error"] = netice.Message;
        return RedirectToAction(nameof(Detail), new { id = setId });
    }
}
