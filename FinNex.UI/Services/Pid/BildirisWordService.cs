using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FinNex.Application.DTOs.Pid;

namespace FinNex.UI.Services.Pid;

// PID bildiriş məktublarını (.docx) şablondan doldurur.
// Şablon placeholder-ləri "sssssN" formatındadır (Word mail-merge deyil, sadə token).
// İki şablon: borcalana (Bildirish_LATIN.docx) və zaminə (Bildir_zam_LATIN.docx).
public static class BildirisWordService
{
    private const string Valyuta = "AZN";

    // Məbləğ formatı: "606.472,20" (nöqtə minlik, vergül onluq) — Monitorinq ilə eyni.
    private static readonly CultureInfo Fmt = YaratFmt();
    private static CultureInfo YaratFmt()
    {
        var c = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NumberGroupSeparator = ".";
        c.NumberFormat.NumberDecimalSeparator = ",";
        return c;
    }
    private static string M(decimal? v) => (v ?? 0m).ToString("#,##0.00", Fmt);
    private static string Rate(decimal? v) => v.HasValue ? v.Value.ToString("0.##", Fmt) : "____";

    private static readonly string[] Aylar =
        { "", "yanvar", "fevral", "mart", "aprel", "may", "iyun",
          "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };

    // Tarixi Azərbaycan sözü ilə: "01 iyul 2026-cı" (il şəkilçisiz — şablonda "il" qalır).
    private static string DAz(DateTime? v)
    {
        if (!v.HasValue) return "";
        var d = v.Value;
        return $"{d.Day:00} {Aylar[d.Month]} {d.Year}-{IlSonluq(d.Year)}";
    }

    // İlin sıra sonluğu (vurğu harmoniyası ilə): son rəqəmin sözünə görə.
    private static string IlSonluq(int il)
    {
        int tek = il % 10, on = (il / 10) % 10;
        //                    0     1     2     3     4     5     6     7     8     9
        string[] tekS = { "cı", "ci", "ci", "cü", "cü", "ci", "cı", "ci", "ci", "cu" };
        string[] onS  = { "ci", "cu", "ci", "cu", "cı", "ci", "cı", "ci", "ci", "cı" };
        return tek != 0 ? tekS[tek] : onS[on];
    }

    // Borcalana bildiriş
    public static byte[] Borclu(string templatePath, BildirisSetirDto s, DateTime tarix)
    {
        var map = OrtakSaheler(s, tarix);
        map["sssss2"] = s.Ad ?? "";            // Kimə (borcalan adı)
        map["sssss3"] = s.BorcUnvan ?? "";     // borcalan ünvanı
        return Doldur(templatePath, map);
    }

    // Zaminə bildiriş — konkret zamin
    public static byte[] Zamin(string templatePath, BildirisSetirDto s, BildirisZaminDto z, DateTime tarix)
    {
        var map = OrtakSaheler(s, tarix);
        map["zmn"]    = z.Ad ?? "";            // Kimə (zamin adı)
        map["sssss3"] = z.Unvan ?? "";         // zamin ünvanı
        map["sssss2"] = s.Ad ?? "";            // borcalan adı (mətn içində referans)
        return Doldur(templatePath, map);
    }

    private static Dictionary<string, string> OrtakSaheler(BildirisSetirDto s, DateTime tarix) => new()
    {
        ["sssss1"]  = DAz(tarix),              // bildiriş tarixi (söz ilə, il şəkilçili)
        ["sssssac"] = DAz(s.VTar),             // müqavilə tarixi (date_open)
        ["sssssay"] = s.MuddedAy?.ToString() ?? "",
        ["sssss9"]  = Rate(s.FaizDerecesi),    // illik faiz dərəcəsi (%) — yoxdursa "____"
        ["sssss4"]  = M(s.VerKr),              // verilmiş kredit
        ["sssss8"]  = " " + Valyuta,           // valyuta (öndə boşluq — şablonda məbləğə bitişikdir)
        ["ssssscm"] = M(s.UmumiBorc),          // ümumi borc
        ["sssss5"]  = M(s.Esas),               // əsas borc
        ["sssss6"]  = M(s.Vk),                 // vaxtı keçmiş əsas
        ["sssss7"]  = M(s.Faiz),               // faiz borcu
        ["sssssv7"] = M(s.VkFaiz),             // vaxtı keçmiş faiz
        ["sssssvk"] = M(s.ToplamVkBorc),       // gecikdirilmiş kredit borcu
    };

    private static byte[] Doldur(string templatePath, Dictionary<string, string> map)
    {
        var bytes = File.ReadAllBytes(templatePath);
        using var ms = new MemoryStream();
        ms.Write(bytes, 0, bytes.Length);
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            MergeRuns(body);
            // Şablonda tarix placeholder-indən sonra SABİT yazılmış "ci/cu il" şəkilçisini sil —
            // DAz onsuz da ilə düzgün sıra şəkilçisi verir (qoşa şəkilçi olmasın).
            // "sssss1 il tarixə" kimi şəkilçisiz hallar toxunulmur.
            foreach (var t in new[] { "sssss1", "sssssac" })
                foreach (var suf in new[] { "cı", "ci", "cu", "cü", "c ı", "c i", "c u", "c ü" })
                    Replace(body, $"{t} {suf} il", $"{t} il");
            // uzun tokenlər əvvəl (prefiks toqquşması yoxdur, amma zəmanət üçün)
            foreach (var kv in map.OrderByDescending(k => k.Key.Length))
                Replace(body, kv.Key, kv.Value);
            doc.MainDocumentPart.Document.Save();
        }
        return ms.ToArray();
    }

    // Yalnız token olan paraqraflarda run-ları birləşdir ki, "sssssN" bölünməsin,
    // amma başlıq/blank-ın formatı korlanmasın.
    private static void MergeRuns(Body body)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count < 2) continue;

            var combined = string.Concat(runs.Select(r =>
                string.Concat(r.Elements<Text>().Select(t => t.Text))));
            if (!combined.Contains("sssss") && !combined.Contains("zmn")) continue;

            var firstText = runs[0].GetFirstChild<Text>();
            if (firstText == null) continue;

            firstText.Text = combined;
            firstText.Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
            for (int i = 1; i < runs.Count; i++) runs[i].Remove();
        }
    }

    private static void Replace(Body body, string key, string value)
    {
        foreach (var text in body.Descendants<Text>())
            if (text.Text.Contains(key))
                text.Text = text.Text.Replace(key, value);
    }
}
