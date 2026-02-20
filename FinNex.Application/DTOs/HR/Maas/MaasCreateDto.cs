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
}
