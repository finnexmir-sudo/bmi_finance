using FinNex.Application.DTOs.HR.MaasNovuDtos;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Maas
{
    public class MaasNovuListDto : BaseDto { public string Ad { get; set; } = null!; public bool Gelirdir { get; set; } public bool Aktivdir { get; set; } }
    public class MaasNovuCreateDto { public string Ad { get; set; } = null!; public bool Gelirdir { get; set; } }
    public class MaasNovuUpdateDto : MaasNovuCreateDto { public int Id { get; set; } public bool Aktivdir { get; set; } }

    public class MaasListDto : BaseDto
    {
        public string IsciTamAd { get; set; } = null!;
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }
    }

    // Maaş əllə yaradılmır, avtomatik hesablanır, amma Generic Service üçün lazımdırsa:
    public class MaasCreateDto
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; } = MaasStatus.Layihe;
    }

    public class MaasUpdateDto : MaasCreateDto { public int Id { get; set; } }
    public class MaasDetayListDto : BaseDto
    {
        public string MaasNovuAdi { get; set; } = null!;
        public decimal Mebleg { get; set; }
        public string? Acıqlama { get; set; }
        public bool Gelirdir { get; set; }
    }

    public class MaasDetayCreateDto
    {
        public int MaasId { get; set; }
        public int MaasNovuId { get; set; }
        public decimal Mebleg { get; set; }
        public string? Acıqlama { get; set; }
    }
    public class MaasDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }

        public string? IsciAdi { get; set; }
        public string? IsciSoyadi { get; set; }

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }

        public DateTime HesablanmaTarixi { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public DateTime? OdenisTarixi { get; set; }

        public string? Qeyd { get; set; }

        public List<MaasDetayDto> Detallar { get; set; } = new();
    }
    public class UpdateMaasDto
    {
        public int Id { get; set; }

        public MaasStatus Status { get; set; }
        public string? Qeyd { get; set; }
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
