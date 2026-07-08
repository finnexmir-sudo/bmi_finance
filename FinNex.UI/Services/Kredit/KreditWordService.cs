using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FinNex.UI.Services.Kredit;

/// <summary>
/// Kredit müqavilə şablonlarını (.docx) token əvəzləməklə doldurur.
/// OdenisTapsirigiWordService ilə eyni mexanizm: əvvəl run-lar birləşdirilir
/// (placeholder bölünməsin), sonra {token} → dəyər əvəzlənir.
/// </summary>
public static class KreditWordService
{
    /// <summary>Bir şablonu doldurub .docx bayt massivi qaytarır.</summary>
    public static byte[] Doldur(string templatePath, IReadOnlyDictionary<string, string?> tokenler)
    {
        var templateBytes = File.ReadAllBytes(templatePath);
        using var ms = new MemoryStream();
        ms.Write(templateBytes, 0, templateBytes.Length);

        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            MergeRuns(body);
            foreach (var kv in tokenler)
                Replace(body, kv.Key, kv.Value);
            doc.MainDocumentPart.Document.Save();
        }

        return ms.ToArray();
    }

    /// <summary>Bir neçə sənədi tək .zip arxivinə yığır.</summary>
    public static byte[] ZipYarat(IEnumerable<(string ad, byte[] data)> senedler)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var sayac = new Dictionary<string, int>();
            foreach (var (ad, data) in senedler)
            {
                // Eyni adlı fayllar üçün suffiks
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

    // Hər paragrafın run-larını birləşdirir ki, {token} bölünməsin
    private static void MergeRuns(Body body)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count < 2) continue;

            var firstText = runs[0].GetFirstChild<Text>();
            if (firstText == null) continue;

            var combined = string.Concat(runs.Select(r =>
                string.Concat(r.Elements<Text>().Select(t => t.Text))));

            firstText.Text = combined;
            firstText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;

            for (var i = 1; i < runs.Count; i++)
                runs[i].Remove();
        }
    }

    private static void Replace(Body body, string key, string? value)
    {
        foreach (var text in body.Descendants<Text>())
            if (text.Text.Contains(key))
                text.Text = text.Text.Replace(key, value ?? "");
    }
}
