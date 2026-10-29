namespace FinNex.Application.DTOs.Dsmf;

// DSMF Vahid Hesabat Forması şablonunun "combo" vərəqindəki 12 səbəb — mətn
// HƏRFİ olaraq orijinal Excel şablonundan (UNIFIED_APPENDIX_1_UNEMPLOYED_DAYS.xlsx,
// "combo" vərəqi) köçürülüb. Dəyişdirmə — DSMF bu mətni dəqiq gözləyə bilər.
public static class DsmfIslemeyenGunSebebi
{
    public static readonly Dictionary<int, string> Metinler = new()
    {
        [1] = "1-İşçinin ödənişli əsas məzuniyyətdən və müvafiq hallarda əlavə məzuniyyətlərindən istifadə etməsi",
        [2] = "2-İşçinin sosial məzuniyyətlərindən istifadə etməsi",
        [3] = "3-İşçinin ödənişsiz məzuniyyətlərindən istifadə etməsi",
        [4] = "4-İşçinin təhsil məzuniyyətlərindən istifadə etməsi",
        [5] = "5-İşçinin xəstələnməsi",
        [6] = "6-Əmək müqaviləsinə xitam verilməsi",
        [7] = "7-Növbəli iş vaxtı",
        [8] = "8-Ayın ortası işə qəbul olma",
        [9] = "9-Qeyd olunan ay üzrə işəgötürənlə əmək müqaviləsi bağlanılmamışdır.",
        [10] = "10-İşçinin işəmuzd işləməsi",
        [11] = "11-İşçinin qanunvericiliklə nəzərdə tutulmuş hallarda və qaydada dövlət və ictimai vəzifələrin yerinə yetirilməsinə cəlb edildiyi müddət",
        [12] = "12-İşçi barəsində məhkəmə hökmünün qanuni qüvvəyə minməsinədək müəyyən müddətə azadlıqdan məhrum edilməsi",
    };

    /// <summary>
    /// Sistemdə qarşılığı olmayan kodlar (29.10.2026) — sosial məzuniyyət (hamiləlik/
    /// doğum, qismən ödənişli), təhsil məzuniyyəti, növbəli iş, işəmuzd, məhkəmə hökmü.
    /// Mühasib bu halları özü əl ilə əlavə etməlidir, sistem səssizcə buraxır demir —
    /// UI-da açıq xəbərdarlıq kimi göstərilir.
    /// </summary>
    public static readonly int[] AvtomatikAskarlanmayanKodlar = { 2, 4, 7, 10, 12 };
}
