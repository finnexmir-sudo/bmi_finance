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

        // HYS (Həyat Yığım Sığortası) parametrləri
        HysIsegotürenFaizi = 10,     // İşəgötürənin HYS payı (default 15%)
        HysMaxMaasFaizi = 11         // İşçinin HYS ödənişi maaşın max neçə %-i ola bilər (default 50%)
    }
}
