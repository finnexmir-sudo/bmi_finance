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
    public static byte[] Doldur(string templatePath, IReadOnlyDictionary<string, string?> tokenler)
    {
        var templateBytes = File.ReadAllBytes(templatePath);
        using var ms = new MemoryStream();
        ms.Write(templateBytes, 0, templateBytes.Length);

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var main = doc.MainDocumentPart!;
            DoldurElement(main.Document.Body!, tokenler);
            foreach (var h in main.HeaderParts) DoldurElement(h.Header, tokenler);
            foreach (var f in main.FooterParts) DoldurElement(f.Footer, tokenler);
            main.Document.Save();
        }

        return ms.ToArray();
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

    private static void DoldurElement(OpenXmlElement root, IReadOnlyDictionary<string, string?> tokenler)
    {
        foreach (var para in root.Descendants<Paragraph>())
            ParaqrafiEvezle(para, tokenler);
    }

    // Bir paraqrafda bütün tokenləri run formatını qoruyaraq əvəz edir
    private static void ParaqrafiEvezle(Paragraph para, IReadOnlyDictionary<string, string?> tokenler)
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
                DiapazonuEvezle(texts, idx, kv.Key.Length, kv.Value ?? "");
                tapildi = true;
                break; // yenidən tara (offsetlər dəyişdi)
            }
            if (!tapildi) break;
        }
    }

    // [start, start+length) diapazonunu (run-lara yayıla bilər) val ilə əvəz edir.
    // Dəyər başladığı run-un formatını alır; digər run-ların formatı toxunulmur.
    private static void DiapazonuEvezle(List<Text> texts, int start, int length, string val)
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

            t.Text = dolduruldu ? before + after : before + val + after;
            t.Space = SpaceProcessingModeValues.Preserve;
            dolduruldu = true;
        }
    }
}
