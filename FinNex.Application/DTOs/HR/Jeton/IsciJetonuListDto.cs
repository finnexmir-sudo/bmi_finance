using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Jeton
{
    public class IsciJetonuListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public int JetonTeyinatiId { get; set; }
        public string JetonAd { get; set; } = null!;
        public JetonNovu JetonNovu { get; set; }
        public JetonRengi JetonRengi { get; set; }
        public string JetonIkon { get; set; } = null!;
        public string JetonRengKodu { get; set; } = null!;
        public decimal JetonSaatDeyeri { get; set; }
        public JetonVahid JetonVahid { get; set; }
        public decimal? QalanSaat { get; set; }
        public DateTime QazanmaTarixi { get; set; }
        public string Sebeb { get; set; } = null!;
        public IsciJetonuStatus Status { get; set; }
        public int? RedimTelebiId { get; set; }
        // Redim sorğusunun təsdiq tarixi (XerclenmeTarixi null olduqda fallback)
        public DateTime? RedimNetice { get; set; }

        // İcazəyə bağlı xərcləmə izlənməsi
        public int? IcazeId { get; set; }
        public DateTime? IcazeTarixi { get; set; }
        public string? IcazeBaslamaSaati { get; set; }
        public string? IcazeBitisSaati { get; set; }

        // Xərclənmə tarixi
        public DateTime? XerclenmeTarixi { get; set; }

        // Yalnız Qara (Menfi) jetonlar üçün doldurulur — bu jetonun həqiqətən
        // kəsdiyi saat (JetonSaatDeyeri − hələ "Gözləyir"/bağışlanmış qalıq).
        // Null = Menfi deyil / hələ hesablanmayıb.
        public decimal? QaraJetonKesilenSaat { get; set; }

        // "36 Saat Hüququ {il}" kimi Sistemli jeton növüdürmü (JetonTeyinati.Sistemli).
        // 15.10.2026: bu bayraq olmadan HR/Jeton ("Jeton İdarəetməsi") səhifəsi bu
        // jetonun QALAN-ını da adi mükafat kimi (QalanSaat, Qara Jeton kəsintisi
        // xaric dəyişməz) göstərirdi — İşçi Jeton Saatları səhifəsinin "Qalıb"
        // sütunu isə icazə istifadəsini çıxıb fərqli (düzgün) rəqəm göstərirdi.
        public bool Sistemli { get; set; }

        // YALNIZ Sistemli=true olan (məs. "36 Saat Hüququ") sətirlərdə doldurulur —
        // JetonController.GetEmeliyyatlar, İşçi Jeton Saatları ilə EYNİ düsturla
        // (Hüquq − cari ilin təsdiqlənmiş icazə saatı). Digər jeton növlərində
        // həmişə null — QalanSaat-ın özü istifadə olunur.
        public decimal? EffektivQalanSaat { get; set; }
    }
}
