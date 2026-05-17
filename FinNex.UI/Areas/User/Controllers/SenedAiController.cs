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

    // ─── Risk Analizi ──────────────────────────────────────────────────────────

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
            catch { }

            return Json(new { ok = true, riskLevel = result.RiskLevel.ToString(), clauses = result.RiskyClauslar });
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
                var record = new SenedKonstruktor
                {
                    AppUserId = userId,
                    SenedNovu = senedNovu ?? senedMovzusu ?? "Sərbəst",
                    ParametrlerJson = JsonSerializer.Serialize(new { senedNovu, musteriAd, gecikmeGun, meble, elaveMelumat, senedMovzusu, tercumeEdilsinmi, hedafDil }),
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

    // ─── Word (.docx) Export ───────────────────────────────────────────────────

    [HttpPost]
    public IActionResult ExportToWord(string content, bool isHtml, string? docTitle,
        bool istifadeEdilsinmiBankBlank = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest();

        var title = string.IsNullOrWhiteSpace(docTitle)
            ? $"FinNex_Sened_{DateTime.Now:yyyyMMdd_HHmm}"
            : docTitle;

        try
        {
            byte[] bytes;
            if (istifadeEdilsinmiBankBlank)
            {
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "bank_blank.docx");
                bytes = BuildWordFromTemplate(content, isHtml, templatePath);
            }
            else
            {
                bytes = BuildWordDocument(content, isHtml);
            }

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                SanitizeFileName(title) + ".docx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ─── Word generation ───────────────────────────────────────────────────────

    // ─── Template-based Word generation ───────────────────────────────────────

    private static byte[] BuildWordFromTemplate(string content, bool isHtml, string templatePath)
    {
        try
        {
            var templateBytes = System.IO.File.ReadAllBytes(templatePath);
            using var ms = new MemoryStream();
            ms.Write(templateBytes, 0, templateBytes.Length);

            using (var wordDoc = WordprocessingDocument.Open(ms, true))
            {
                var body = wordDoc.MainDocumentPart!.Document.Body!;
                var lines = isHtml ? HtmlToLines(content) : PlainToLines(content);

                // Find signature area: first direct-child paragraph containing signature keywords
                OpenXmlElement? insertBefore = body.ChildElements
                    .OfType<Paragraph>()
                    .FirstOrDefault(p =>
                    {
                        var t = p.InnerText;
                        return t.Contains("müdiri") || t.StartsWith("«") || t.Contains("filialının müdiri");
                    });

                // Fallback: insert before SectionProperties (always last child)
                insertBefore ??= (OpenXmlElement?)body.ChildElements
                    .OfType<SectionProperties>().LastOrDefault();

                void Insert(OpenXmlElement el)
                {
                    if (insertBefore != null) body.InsertBefore(el, insertBefore);
                    else body.AppendChild(el);
                }

                // Current date right-aligned (Azerbaijani format), no first-line indent
                var datePara = MakePara(AzDate(DateTime.Now), "Body", noIndent: true);
                datePara.ParagraphProperties!.Justification =
                    new Justification { Val = JustificationValues.Right };
                Insert(datePara);
                Insert(SpacerPara());

                // AI-generated content
                foreach (var (lineText, style) in lines)
                    Insert(string.IsNullOrWhiteSpace(lineText) ? SpacerPara() : MakePara(lineText, style));

                Insert(SpacerPara());
                Insert(SpacerPara());

                wordDoc.MainDocumentPart.Document.Save();
            }

            return ms.ToArray();
        }
        catch
        {
            // Fallback to standard clean document if template unreadable
            return BuildWordDocument(content, isHtml);
        }
    }

    private static string AzDate(DateTime d)
    {
        string[] months = ["Yanvar","Fevral","Mart","Aprel","May","İyun","İyul","Avqust","Sentyabr","Oktyabr","Noyabr","Dekabr"];
        // Ordinal suffix via vowel harmony on last digit of year
        string[] suffixes = ["cı","ci","ci","cü","cü","ci","cı","ci","ci","cu"];
        return $"{d.Day} {months[d.Month - 1]} {d.Year}-{suffixes[d.Year % 10]} il";
    }

    private static byte[] BuildWordDocument(string content, bool isHtml)
    {
        using var ms = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            var body = mainPart.Document.Body!;

            var lines = isHtml ? HtmlToLines(content) : PlainToLines(content);
            bool started = false;

            foreach (var (lineText, style) in lines)
            {
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    if (started) body.AppendChild(SpacerPara());
                    continue;
                }
                started = true;
                body.AppendChild(MakePara(lineText, style));
            }

            // Page setup: A4
            var sectPr = new SectionProperties();
            var ps = new PageSize();
            ps.Width = (UInt32Value)11906U;
            ps.Height = (UInt32Value)16838U;
            sectPr.AppendChild(ps);

            var pm = new PageMargin();
            pm.Top = 1134;
            pm.Bottom = 1134;
            pm.Left = (UInt32Value)1418U;
            pm.Right = (UInt32Value)1134U;
            sectPr.AppendChild(pm);
            body.AppendChild(sectPr);

            mainPart.Document.Save();
        }
        return ms.ToArray();
    }

    private static Paragraph MakePara(string text, string style, bool noIndent = false)
    {
        bool isHeading = style is "H1" or "H2" or "H3";
        bool bold = style is "H1" or "H2";
        string halfPt = style switch { "H1" => "28", "H2" => "26", "H3" => "24", _ => "24" };

        var para = new Paragraph();
        var pPr = new ParagraphProperties();

        var spacing = new SpacingBetweenLines();
        spacing.LineRule = LineSpacingRuleValues.Auto;
        if (isHeading)
        {
            spacing.Line = "276";
            spacing.Before = "240";
            spacing.After = "120";
            pPr.Justification = new Justification { Val = JustificationValues.Center };
        }
        else
        {
            spacing.Line = "360"; // 1.5x sətir aralığı
            spacing.Before = "0";
            spacing.After = "160";
            pPr.Justification = new Justification { Val = JustificationValues.Both };
            if (!noIndent)
                pPr.Indentation = new Indentation { FirstLine = "720" }; // 0.5" abzas girintisi
        }
        pPr.SpacingBetweenLines = spacing;
        para.ParagraphProperties = pPr;

        var run = new Run();
        var rPr = new RunProperties();
        rPr.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
        rPr.AppendChild(new FontSize { Val = halfPt });
        if (bold) rPr.AppendChild(new Bold());
        run.RunProperties = rPr;
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);
        return para;
    }

    private static Paragraph SpacerPara()
    {
        var para = new Paragraph();
        var pPr = new ParagraphProperties();
        var spacing = new SpacingBetweenLines();
        spacing.Before = "0";
        spacing.After = "60";
        pPr.SpacingBetweenLines = spacing;
        para.ParagraphProperties = pPr;
        return para;
    }

    private static List<(string text, string style)> HtmlToLines(string html)
    {
        var result = new List<(string, string)>();
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", m => "%%H1%%" + Strip(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", m => "%%H2%%" + Strip(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", m => "%%H3%%" + Strip(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, @"<p[^>]*>(.*?)</p>", m => Strip(m.Groups[1].Value) + "\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Strip(html);

        foreach (var line in html.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("%%H1%%"))      result.Add((t.Substring(6), "H1"));
            else if (t.StartsWith("%%H2%%")) result.Add((t.Substring(6), "H2"));
            else if (t.StartsWith("%%H3%%")) result.Add((t.Substring(6), "H3"));
            else                             result.Add((t, "Body"));
        }
        return result;
    }

    private static List<(string text, string style)> PlainToLines(string text)
    {
        var result = new List<(string, string)>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            var isHeading = line.Length is > 0 and <= 80
                && line == line.ToUpper()
                && line.Any(char.IsLetter)
                && (i == 0 || string.IsNullOrWhiteSpace(lines[i - 1]));
            result.Add((line, isHeading ? "H2" : "Body"));
        }
        return result;
    }

    private static string Strip(string html) =>
        Regex.Replace(html, @"<[^>]+>", "")
             .Replace("&nbsp;", " ").Replace("&amp;", "&")
             .Replace("&lt;", "<").Replace("&gt;", ">").Trim();

    private static string SanitizeFileName(string name) =>
        Regex.Replace(name, @"[\\/:*?""<>|]", "_").Trim().TrimEnd('.');
}
