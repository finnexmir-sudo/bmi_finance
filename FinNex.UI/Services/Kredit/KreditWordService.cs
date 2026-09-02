using System.IO.Compression;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FinNex.UI.Services.Kredit;

/// <summary>
/// Kredit müqavilə şablonlarını (.docx) token əvəzləməklə doldurur.
/// {token} bir neçə run-a bölünə bilər — run-ları birləşdirmədən, HƏR RUN-un
/// öz formatını (bold, şrift) qoruyaraq əvəz edir. Token dəyəri başladığı
/// run-un formatını alır (yəni şablonda token qalın idisə, dəyər də qalın olur).
/// </summary>
public static class KreditWordService
{
    /// <param name="unicodeSrift">
    /// Dəyər yazılan run-un şrifti bununla əvəz olunur — AMMA yalnız dəyərdə
    /// ASCII-dən kənar hərf (Ə, Ü, İ, Ğ, Ş, Ç, Ö) varsa. `null` = toxunma.
    ///
    /// NİYƏ LAZIMDIR: köhnə BMI şablonlarında mətn `Times Latin` / `Ora Times`
    /// kimi ƏVVƏLKİ NƏSİL Azəri şriftlərindədir — orada müasir Unicode hərfləri
    /// YOXDUR. Belə run-a «HÜSEYNOV SAMİR OĞLU» yazsan Word hər xüsusi hərf üçün
    /// başqa şriftə keçir və aralarda boşluq qalır: «HÜ SEYNOV SAMİ R OĞ LU».
    /// Şablonun özü də bu problemi bilir — «ilə» sözündəki `ə` ayrıca
    /// `Times New Roman` run-una qoyulub (02.09.2026 yoxlanıldı).
    ///
    /// ⚠️ Defolt `null` — mövcud MÜQAVİLƏ şablonlarının görünüşü dəyişməsin.
    /// </param>
    public static byte[] Doldur(string templatePath, IReadOnlyDictionary<string, string?> tokenler,
                                string? unicodeSrift = null)
    {
        var templateBytes = File.ReadAllBytes(templatePath);
        using var ms = new MemoryStream();
        ms.Write(templateBytes, 0, templateBytes.Length);

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var main = doc.MainDocumentPart!;
            DoldurElement(main.Document.Body!, tokenler, unicodeSrift);
            foreach (var h in main.HeaderParts) DoldurElement(h.Header, tokenler, unicodeSrift);
            foreach (var f in main.FooterParts) DoldurElement(f.Footer, tokenler, unicodeSrift);
            main.Document.Save();
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Şablonda verilmiş yer tutucunun (məs. "{a_mno}") olub-olmadığını yoxlayır.
    ///
    /// NİYƏ LAZIMDIR: kod bir tokeni doldurur, şablonda o token yoxdursa Doldur()
    /// heç bir xəta vermir — dəyər sadəcə itir. Nömrə ilə bağlı tokenlərdə bu
    /// SƏSSİZ və TƏHLÜKƏLİDİR: məsələn avtomobil müqaviləsinin başlığında
    /// {a_mno} əvəzinə {k_mno} qalarsa, sənədin üstündə girov nömrəsi yerinə
    /// kredit müqaviləsinin nömrəsi çıxar və heç kim bunu görməz.
    ///
    /// Yoxlama Doldur() ilə eyni mətn görüntüsündən gedir: paraqrafın bütün
    /// run-ları birləşdirilir, çünki Word tokeni bir neçə run-a bölə bilər.
    /// </summary>
    public static bool TokenVarmi(string templatePath, string token)
    {
        using var doc = WordprocessingDocument.Open(templatePath, false);
        var main = doc.MainDocumentPart!;

        bool Var(OpenXmlElement? root) =>
            root != null && root.Descendants<Paragraph>().Any(p =>
                string.Concat(p.Descendants<Text>().Select(t => t.Text))
                      .Contains(token, StringComparison.Ordinal));

        return Var(main.Document.Body)
            || main.HeaderParts.Any(h => Var(h.Header))
            || main.FooterParts.Any(f => Var(f.Footer));
    }

    public static byte[] ZipYarat(IEnumerable<(string ad, byte[] data)> senedler)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var sayac = new Dictionary<string, int>();
            foreach (var (ad, data) in senedler)
            {
                var faylAdi = ad;
                if (sayac.TryGetValue(ad, out var n))
                {
                    sayac[ad] = n + 1;
                    var uzanti = Path.GetExtension(ad);
                    faylAdi = Path.GetFileNameWithoutExtension(ad) + $" ({n + 1})" + uzanti;
                }
                else sayac[ad] = 1;

                var entry = zip.CreateEntry(faylAdi, CompressionLevel.Optimal);
                using var es = entry.Open();
                es.Write(data, 0, data.Length);
            }
        }
        return ms.ToArray();
    }

    private static void DoldurElement(OpenXmlElement root, IReadOnlyDictionary<string, string?> tokenler,
                                      string? unicodeSrift)
    {
        foreach (var para in root.Descendants<Paragraph>())
            ParaqrafiEvezle(para, tokenler, unicodeSrift);
    }

    // Bir paraqrafda bütün tokenləri run formatını qoruyaraq əvəz edir
    private static void ParaqrafiEvezle(Paragraph para, IReadOnlyDictionary<string, string?> tokenler,
                                        string? unicodeSrift)
    {
        // Hər run-u tək Text-ə normallaşdır (yalnız RUN daxilində — format dəyişmir)
        foreach (var run in para.Elements<Run>())
        {
            var runTexts = run.Elements<Text>().ToList();
            if (runTexts.Count <= 1) continue;
            var birlesmis = string.Concat(runTexts.Select(t => t.Text));
            runTexts[0].Text = birlesmis;
            runTexts[0].Space = SpaceProcessingModeValues.Preserve;
            for (var i = 1; i < runTexts.Count; i++) runTexts[i].Remove();
        }

        var guard = 0;
        while (guard++ < 2000)
        {
            var texts = para.Elements<Run>()
                            .Select(r => r.GetFirstChild<Text>())
                            .Where(t => t != null)
                            .Cast<Text>()
                            .ToList();
            if (texts.Count == 0) break;

            var full = string.Concat(texts.Select(t => t.Text));
            var tapildi = false;
            foreach (var kv in tokenler)
            {
                var idx = full.IndexOf(kv.Key, StringComparison.Ordinal);
                if (idx < 0) continue;
                DiapazonuEvezle(texts, idx, kv.Key.Length, kv.Value ?? "", unicodeSrift);
                tapildi = true;
                break; // yenidən tara (offsetlər dəyişdi)
            }
            if (!tapildi) break;
        }
    }

    // [start, start+length) diapazonunu (run-lara yayıla bilər) val ilə əvəz edir.
    // Dəyər başladığı run-un formatını alır; digər run-ların formatı toxunulmur.
    private static void DiapazonuEvezle(List<Text> texts, int start, int length, string val,
                                        string? unicodeSrift = null)
    {
        var end = start + length;
        var pos = 0;
        var dolduruldu = false;
        foreach (var t in texts)
        {
            var len = t.Text.Length;
            int rs = pos, re = pos + len;
            pos = re;
            if (re <= start || rs >= end) continue; // kəsişmə yoxdur

            var localStart = Math.Max(0, start - rs);
            var localEnd = Math.Min(len, end - rs);
            var before = t.Text.Substring(0, localStart);
            var after = t.Text.Substring(localEnd);

            var buRunaYazilir = !dolduruldu;
            t.Text = dolduruldu ? before + after : before + val + after;
            t.Space = SpaceProcessingModeValues.Preserve;

            // Dəyər məhz BU run-a yazıldısa və içində ASCII-dən kənar hərf varsa,
            // run-un şriftini Unicode oxuyan şriftlə əvəz et (yuxarıdakı izah).
            // Ölçü/qalın/maili/altxətt TOXUNULMUR — yalnız şrift adı dəyişir.
            if (buRunaYazilir && unicodeSrift != null && val.Any(c => c > 127))
                SriftiDeyis(t, unicodeSrift);

            dolduruldu = true;
        }
    }

    /// <summary>
    /// Text-in aid olduğu run-un `w:ascii` / `w:hAnsi` şriftini dəyişir.
    /// `w:cs` (complex script) TOXUNULMUR — o, ərəb/fars mətni üçündür və
    /// şablonlarda ayrıca təyin olunub.
    /// </summary>
    private static void SriftiDeyis(Text t, string srift)
    {
        if (t.Parent is not Run run) return;

        var rPr = run.RunProperties ??= new RunProperties();
        var fonts = rPr.GetFirstChild<RunFonts>();
        if (fonts == null)
        {
            fonts = new RunFonts();
            // RunFonts sxemə görə RunProperties-in İLK elementi olmalıdır —
            // sonda əlavə etsək Word faylı «zədəlidir» sayır və açmır.
            rPr.InsertAt(fonts, 0);
        }
        fonts.Ascii = srift;
        fonts.HighAnsi = srift;
    }
}
