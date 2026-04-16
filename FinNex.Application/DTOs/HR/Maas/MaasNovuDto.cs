using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.MaasNovuDtos
{
    public class MaasNovuDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public MaasDetayTipi Tip { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class CreateMaasNovuDto
    {
        public string Ad { get; set; } = null!;
        public MaasDetayTipi Tip { get; set; }
    }

    public class UpdateMaasNovuDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public MaasDetayTipi Tip { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class MaasParametriDto
    {
        public int Id { get; set; }
        public MaasParametrNovu Nov { get; set; }
        public string NovAd => Nov switch
        {
            MaasParametrNovu.GelirVergisiFaizi => "Gəlir Vergisi Faizi",
            MaasParametrNovu.DsmfFaizi => "DSMF Faizi (İşçi)",
            MaasParametrNovu.IssizlikSigortasiFaizi => "İşsizlik Sığortası Faizi (İşçi)",
            MaasParametrNovu.IcbariTibbiSigortaFaizi => "İTSS Faizi",
            MaasParametrNovu.MinimumEmekHaqqi => "Minimum Əmək Haqqı",
            MaasParametrNovu.VergiGuzestiMeblegi => "Vergi Güzəşti Məbləği",
            MaasParametrNovu.DsmfIsegoturenFaizi => "DSMF Faizi (İşəgötürən)",
            MaasParametrNovu.IssizlikIsegoturenFaizi => "İşsizlik Sığortası Faizi (İşəgötürən)",
            MaasParametrNovu.IcbariTibbiSigortaIsegoturenFaizi => "İTSS Faizi (İşəgötürən)",
            MaasParametrNovu.HysIsegoturenFaizi => "HYS İşəgötürən Faizi",
            MaasParametrNovu.HysMaxMaasFaizi => "HYS Max Maaş Faizi",
            _ => Nov.ToString()
        };
        public MaasParametrTipi Tip { get; set; }
        public decimal Deyer { get; set; }
        public string? Aciqlama { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class CreateMaasParametriDto
    {
        public MaasParametrNovu Nov { get; set; }
        public MaasParametrTipi Tip { get; set; }
        public decimal Deyer { get; set; }
        public string? Aciqlama { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
    }

    public class UpdateMaasParametriDto
    {
        public int Id { get; set; }
        public MaasParametrNovu Nov { get; set; }
        public MaasParametrTipi Tip { get; set; }
        public decimal Deyer { get; set; }
        public string? Aciqlama { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class MaasDetayDto
    {
        public int Id { get; set; }
        public int MaasId { get; set; }
        public int MaasNovuId { get; set; }
        public string MaasNovuAd { get; set; } = null!;
        public decimal Mebleg { get; set; }
        public string? Aciqlama { get; set; }
    }

    public class CreateMaasDetayDto
    {
        public int MaasId { get; set; }
        public int MaasNovuId { get; set; }
        public decimal Mebleg { get; set; }
        public string? Aciqlama { get; set; }
    }

    public class UpdateMaasDetayDto
    {
        public int Id { get; set; }
        public int MaasNovuId { get; set; }
        public decimal Mebleg { get; set; }
        public string? Aciqlama { get; set; }
    }

    public class IsciMaasTarixcesiDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrNomresi { get; set; }
        public string? Sebeb { get; set; }
    }

    public class CreateIsciMaasTarixcesiDto
    {
        public int IsciId { get; set; }
        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrNomresi { get; set; }
        public string? Sebeb { get; set; }
    }

    public class UpdateIsciMaasTarixcesiDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrNomresi { get; set; }
        public string? Sebeb { get; set; }
    }
}