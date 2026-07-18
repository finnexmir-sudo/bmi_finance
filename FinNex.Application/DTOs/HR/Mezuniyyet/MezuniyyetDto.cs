using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    // Ümumi baxış üçün (Siyahılama)
    public class MezuniyyetDto
    {
        public int Id { get; set; }

        // İşçi məlumatları
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!; // Şöbə məlumatı vacibdir
        public string VezifeAdi { get; set; } = null!; // Vəzifə məlumatı vacibdir

        // Ɗvəzedici şəxs
        public int? EvezEdenIsciId { get; set; }
        public string? EvezEdenIsciAdSoyad { get; set; }

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }

        // Tarixlər
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }

        public string? Qeyd { get; set; }
        public string? ImtinaSebebi { get; set; }

        // Workflow Tarixçəsi
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // Ödəniş (ay-sonu ↔ qabaqcadan) — HR düzəliş üçün
        public MezuniyyetOdenisTipi OdenisTipi { get; set; } = MezuniyyetOdenisTipi.AySonuOdenis;
        public MezuniyyetOdenisStatus OdenisStatus { get; set; } = MezuniyyetOdenisStatus.TetbiqEdilmir;
        public decimal? OdenenMebleg { get; set; }
        public DateTime? PlanliOdenisTarixi { get; set; }

        // Ɗmr nömrəsi (K/M) — HR təsdiqindən sonra avtomatik verilir
        public int? EmrRegem { get; set; }
        public string? EmrSuffiks { get; set; }
        public int? EmrIl { get; set; }
        public string? EmrNomresi => EmrRegem.HasValue ? $"K/M {EmrRegem}{EmrSuffiks ?? string.Empty}" : null;

        // Dövlət vəzifəsi korreksiyası üçün sənəd məlumatları
        // SenedYolu: "|" ilə ayrılmış fayl yolları (çoxlu sənəd dəstəyi)
        public string? SenedYolu { get; set; }
        public string? KorreksiyaSebebi { get; set; }

        // İşçinin ləğv müraciəti (HR yalnız bu müraciət əsasında ləğv edir)
        public bool      LegvTelebEdilib { get; set; }
        public string?   LegvTelebSebebi { get; set; }
        public DateTime? LegvTelebTarixi { get; set; }

        /// <summary>
        /// SenedYolu-nu ayrı-ayrı yollara parçalayır ("|" ayırıcı ilə).
        /// </summary>
        public IReadOnlyList<string> SenedYollari =>
            string.IsNullOrWhiteSpace(SenedYolu)
                ? []
                : SenedYolu.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }
}
