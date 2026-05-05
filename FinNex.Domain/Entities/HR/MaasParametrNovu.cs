namespace FinNex.Domain.Entities.HR
{
    public enum MaasParametrNovu
    {
        // Isciden tutulanlar
        GelirVergisiFaizi = 1,
        DsmfFaizi = 2,
        IssizlikSigortasiFaizi = 3,
        IcbariTibbiSigortaFaizi = 4,

        // Umumi
        MinimumEmekHaqqi = 5,
        VergiGuzestiMeblegi = 6,

        // Sirket odeyir (isciden tutulmur) -- evvel hardcode idi, indi parametrden
        DsmfIsegoturenFaizi = 7,
        IssizlikIsegoturenFaizi = 8,
        IcbariTibbiSigortaIsegoturenFaizi = 9,

        // HYS (Heyat Yigim Sigortasi)
        HysIsegoturenFaizi = 10,    // isegoturen payi: 15%
        HysMaxMaasFaizi = 11,       // maasin max nece %-i HYS ola biler: 50%

        // Avans
        AvansMaxFaizi = 12,         // maasin max nece %-i avans ola biler: 30%
        AvansGunu = 13,             // ayin nece-ci gunu avans verilir: 15

        // Xestelik odenisi staj faizleri (qanun: <8 il→60%, 8–12 il→80%, ≥12 il→100%)
        // Mühasib lazım gələndə bu faizləri dəyişə bilər (məs. hamı 100%).
        XestelikStaj8YasagidaFaizi = 14,   // staj < 8 il → default 60%
        XestelikStaj8_12IlFaizi = 15,      // 8 ≤ staj < 12 il → default 80%
        XestelikStaj12YuxariFaizi = 16,    // staj ≥ 12 il → default 100%

        // Xəstəlik ödənişinin yeni DSMF-əsaslı düsturu (May 2026+):
        // GünlükPul = (son 12 ay (DSMF işçi + DSMF şirkət) × Əmsal) ÷ (son 12 ay təqvim günü − xəstəlik təqvim günü)
        XestelikDsmfEmsali = 17            // default 4
    }
}
