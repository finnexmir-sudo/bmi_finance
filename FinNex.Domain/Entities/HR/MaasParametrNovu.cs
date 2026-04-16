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
        AvansGunu = 13              // ayin nece-ci gunu avans verilir: 15
    }
}
