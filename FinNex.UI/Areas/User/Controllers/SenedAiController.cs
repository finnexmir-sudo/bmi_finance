using FinNex.Application.DTOs.AI;
using FinNex.Application.Interfaces.AI;
using FinNex.Application.Interfaces.Communication;
using FinNex.DataAccess.Contexts;
using FinNex.Domain;
using FinNex.Domain.Entities.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class SenedAiController : Controller
{
    private readonly ISenedAiService _ai;
    private readonly IAttachmentTextExtractor _extractor;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public SenedAiController(ISenedAiService ai, IAttachmentTextExtractor extractor,
        AppDbContext db, UserManager<AppUser> userManager, IWebHostEnvironment env)
    {
        _ai = ai;
        _extractor = extractor;
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // ─── Risk Analizi ─────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult RiskAnaliz() => View();

    [HttpPost]
    public async Task<IActionResult> RiskAnalizEt(IFormFile? fayl, string? metin)
    {
        var userIdStr = _userManager.GetUserId(User);
        if (userIdStr == null)
            return Json(new { ok = false, xeta = "İstifadəçi müəyyən edilmədi." });

        var userId = int.Parse(userIdStr);
        string text = "";
        string fileName = "manual_input";

        try
        {
            if (fayl != null && fayl.Length > 0)
            {
                var ext = Path.GetExtension(fayl.FileName).ToLowerInvariant();
                var tempDir = Path.Combine(_env.ContentRootPath, "Temp");
                Directory.CreateDirectory(tempDir);
                var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}{ext}");

                await using (var fs = System.IO.File.Create(tempPath))
                    await fayl.CopyToAsync(fs);

                text = _extractor.Extract(tempPath, fayl.ContentType) ?? "";
                fileName = fayl.FileName;
                System.IO.File.Delete(tempPath);
            }
            else if (!string.IsNullOrWhiteSpace(metin))
            {
                text = metin;
            }

            if (string.IsNullOrWhiteSpace(text))
                return Json(new { ok = false, xeta = "Fayl və ya mətn daxil edilməyib." });

            var result = await _ai.AnalyzeRiskAsync(text, fileName);

            if (result.Xeta != null)
                return Json(new { ok = false, xeta = result.Xeta });

            // DB-yə saxla (uğursuz olsa nəticəni yenə qaytarırıq)
            try
            {
                var record = new SenedAnaliz
                {
                    AppUserId = userId,
                    OriginalFileName = fileName,
                    OriginalText = text.Length > 10000 ? text.Substring(0, 10000) : text,
                    RiskLevel = result.RiskLevel,
                    RiskyClausesJson = JsonSerializer.Serialize(result.RiskyClauslar)
                };
                _db.SenedAnalizler.Add(record);
                await _db.SaveChangesAsync();
            }
            catch { /* migration hələ run edilməyibsə nəticəni yenə qaytarırıq */ }

            return Json(new
            {
                ok = true,
                riskLevel = result.RiskLevel.ToString(),
                clauses = result.RiskyClauslar
            });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, xeta = "Server xətası: " + ex.Message });
        }
    }

    // ─── Sənəd Konstruktoru ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult SenedYarat() => View();

    [HttpPost]
    public async Task<IActionResult> SenedYaratPost(string senedNovu, string musteriAd,
        int gecikmeGun, decimal meble, string? elaveMelumat)
    {
        if (string.IsNullOrWhiteSpace(senedNovu) || string.IsNullOrWhiteSpace(musteriAd))
            return Json(new { ok = false, xeta = "Sənəd növü və müştəri adı mütləqdir." });

        var userIdStr = _userManager.GetUserId(User);
        if (userIdStr == null)
            return Json(new { ok = false, xeta = "İstifadəçi müəyyən edilmədi." });

        var userId = int.Parse(userIdStr);

        try
        {
            var result = await _ai.ConstructDocumentAsync(senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat);

            if (result.Xeta != null)
                return Json(new { ok = false, xeta = result.Xeta });

            try
            {
                var parametrler = new { senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat };
                var record = new SenedKonstruktor
                {
                    AppUserId = userId,
                    SenedNovu = senedNovu,
                    ParametrlerJson = JsonSerializer.Serialize(parametrler),
                    GeneratedContent = result.GeneratedContent
                };
                _db.SenedKonstruktorlar.Add(record);
                await _db.SaveChangesAsync();
            }
            catch { /* migration hələ run edilməyibsə nəticəni yenə qaytarırıq */ }

            return Json(new { ok = true, content = result.GeneratedContent });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, xeta = "Server xətası: " + ex.Message });
        }
    }
}
