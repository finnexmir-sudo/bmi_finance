using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
using System.Text.RegularExpressions;

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
    public async Task<IActionResult> SenedYaratPost(
        string senedNovu, string musteriAd, int gecikmeGun, decimal meble, string? elaveMelumat,
        string? senedMovzusu, bool tercumeEdilsinmi, string? hedafDil)
    {
        var userIdStr = _userManager.GetUserId(User);
        if (userIdStr == null)
            return Json(new { ok = false, xeta = "İstifadəçi müəyyən edilmədi." });

        var hasParametrik = !string.IsNullOrWhiteSpace(senedNovu) && !string.IsNullOrWhiteSpace(musteriAd);
        var hasSerbest = !string.IsNullOrWhiteSpace(senedMovzusu);

        if (!hasParametrik && !hasSerbest)
            return Json(new { ok = false, xeta = "Sənəd parametrləri və ya sərbəst mövzu daxil edilməyib." });

        var userId = int.Parse(userIdStr);

        try
        {
            var result = await _ai.ConstructDocumentAsync(
                senedNovu ?? "", musteriAd ?? "", gecikmeGun, meble, elaveMelumat,
                senedMovzusu, tercumeEdilsinmi, hedafDil);

            if (result.Xeta != null)
                return Json(new { ok = false, xeta = result.Xeta });

            try
            {
                var parametrler = new { senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat, senedMovzusu, tercumeEdilsinmi, hedafDil };
                var record = new SenedKonstruktor
                {
                    AppUserId = userId,
                    SenedNovu = senedNovu ?? senedMovzusu ?? "Sərbəst",
                    ParametrlerJson = JsonSerializer.Serialize(parametrler),
                    GeneratedContent = result.GeneratedContent
                };
                _db.SenedKonstruktorlar.Add(record);
                await _db.SaveChangesAsync();
            }
            catch { }

            return Json(new { ok = true, content = result.GeneratedContent, isHtml = result.IsHtml });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, xeta = "Server xətası: " + ex.Message });
        }
    }

    // ─── Word (.docx) Export ──────────────────────────────────────────────────

    [HttpPost]
    public IActionResult ExportToWord(string content, bool isHtml, string? docTitle)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest();

        var title = docTitle ?? $"FinNex_Sened_{DateTime.Now:yyyyMMdd_HHmm}";
        var bytes = BuildWordDocument(content, isHtml, title);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            $"{SanitizeFileName(title)}.docx");
    }

    // ─── Word Generation ──────────────────────────────────────────────────────

    private static byte[] BuildWordDocument(string content, bool isHtml, string docTitle)
    {
        using var ms = new MemoryStream();
        using var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true);

        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Styles
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = BuildStyles();

        // Page setup: A4, 2.5cm left/right margins
        var sectPr = new SectionProperties(
            new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait },
            new PageMargin { Top = 1134, Bottom = 1134, Left = 1418, Right = 1134, Header = 709, Footer = 709 }
        );

        // Content
        var lines = isHtml ? ParseHtmlToLines(content) : ParsePlainToLines(content);
        bool firstLine = true;

        foreach (var (text, style) in lines)
        {
            if (string.IsNullOrWhiteSpace(text) && !firstLine)
            {
                body.AppendChild(EmptyParagraph());
                continue;
            }
            if (string.IsNullOrWhiteSpace(text)) continue;

            body.AppendChild(MakeParagraph(text, style));
            firstLine = false;
        }

        body.AppendChild(sectPr);
        mainPart.Document.Save();
        return ms.ToArray();
    }

    private static List<(string text, string style)> ParseHtmlToLines(string html)
    {
        var result = new List<(string, string)>();
        // h1/h2/h3 → headings, p → body, strip other tags
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", m => "HEADING1" + StripTags(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", m => "HEADING2" + StripTags(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", m => "HEADING3" + StripTags(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<p[^>]*>(.*?)</p>", m => StripTags(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = StripTags(html);

        foreach (var line in html.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("HEADING1"))
                result.Add((trimmed.Substring(9), "Heading1"));
            else if (trimmed.StartsWith("HEADING2"))
                result.Add((trimmed.Substring(9), "Heading2"));
            else if (trimmed.StartsWith("HEADING3"))
                result.Add((trimmed.Substring(9), "Heading3"));
            else
                result.Add((trimmed, "BodyText"));
        }
        return result;
    }

    private static List<(string text, string style)> ParsePlainToLines(string text)
    {
        var result = new List<(string, string)>();
        var lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            // Heuristic: SHORT ALL-CAPS line surrounded by blank lines → heading
            var isHeading = line.Length > 0 && line.Length <= 80
                && line == line.ToUpper()
                && line.Any(char.IsLetter)
                && (i == 0 || string.IsNullOrWhiteSpace(lines[i - 1]));

            result.Add((line, isHeading ? "Heading2" : "BodyText"));
        }
        return result;
    }

    private static Paragraph MakeParagraph(string text, string style)
    {
        var para = new Paragraph();
        var props = new ParagraphProperties(new ParagraphStyleId { Val = style });

        if (style == "BodyText")
        {
            props.AppendChild(new SpacingBetweenLines { Line = "276", LineRule = LineSpacingRuleValues.Auto, Before = "0", After = "120" });
        }
        para.AppendChild(props);

        var run = new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        var rPr = new RunProperties();
        if (style == "BodyText")
            rPr.AppendChild(new FontSize { Val = "24" }); // 12pt
        run.PrependChild(rPr);
        para.AppendChild(run);
        return para;
    }

    private static Paragraph EmptyParagraph()
    {
        var para = new Paragraph();
        var props = new ParagraphProperties();
        props.AppendChild(new SpacingBetweenLines { Before = "0", After = "0" });
        para.AppendChild(props);
        return para;
    }

    private static Styles BuildStyles()
    {
        var styles = new Styles();

        styles.AppendChild(new Style
        {
            Type = StyleValues.Paragraph, StyleId = "BodyText", Default = false,
            StyleName = new StyleName { Val = "BodyText" },
            StyleRunProperties = new StyleRunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "24" })
        });

        styles.AppendChild(MakeHeadingStyle("Heading1", "14", "2E4057", true));
        styles.AppendChild(MakeHeadingStyle("Heading2", "26", "1971C2", true));
        styles.AppendChild(MakeHeadingStyle("Heading3", "24", "495057", false));

        return styles;
    }

    private static Style MakeHeadingStyle(string id, string halfPt, string color, bool bold)
    {
        var s = new Style { Type = StyleValues.Paragraph, StyleId = id, StyleName = new StyleName { Val = id } };
        var rpr = new StyleRunProperties();
        rpr.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
        rpr.AppendChild(new FontSize { Val = halfPt });
        rpr.AppendChild(new Color { Val = color });
        if (bold) rpr.AppendChild(new Bold());
        s.StyleRunProperties = rpr;

        var ppr = new StyleParagraphProperties();
        ppr.AppendChild(new SpacingBetweenLines { Before = "240", After = "120" });
        s.StyleParagraphProperties = ppr;
        return s;
    }

    private static string StripTags(string html) =>
        Regex.Replace(html, @"<[^>]+>", "").Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Trim();

    private static string SanitizeFileName(string name) =>
        Regex.Replace(name, @"[\\/:*?""<>|]", "_").Trim().TrimEnd('.');
}
