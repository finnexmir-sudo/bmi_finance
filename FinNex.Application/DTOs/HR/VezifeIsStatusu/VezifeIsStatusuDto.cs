using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.VezifeIsStatusu
{
    /// <summary>Siyahıda bir sətir — Vəzifə cədvəlindəki TƏKRARSIZ ad + cari status.</summary>
    public class VezifeIsStatusuDto
    {
        public string VezifeAdi { get; set; } = null!;
        public VezifeIsStatusuTipi Status { get; set; } = VezifeIsStatusuTipi.Icrachi;

        /// <summary>Bu ad üçün əvvəllər status qurulub-qurulmadığı (default-la fərqləndirmək üçün).</summary>
        public bool Qurulub { get; set; }
    }

    /// <summary>Saxlama sorğusunda bir sətir.</summary>
    public class VezifeIsStatusuSetDto
    {
        public string VezifeAdi { get; set; } = null!;
        public VezifeIsStatusuTipi Status { get; set; }
    }
}
