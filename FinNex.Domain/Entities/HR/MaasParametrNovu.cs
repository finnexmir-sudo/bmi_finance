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
        IssizlikIsegoturenFaizi = 8
    }
}
