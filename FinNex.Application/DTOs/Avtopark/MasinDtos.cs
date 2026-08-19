using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.Avtopark;

namespace FinNex.Application.DTOs.Avtopark
{
    /// <summary>Maşın kartı — oxumaq üçün.</summary>
    public class MasinDto
    {
        public int Id { get; set; }
        public string DovletNomresi { get; set; } = "";
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public int? BuraxilisIli { get; set; }
        public string? Reng { get; set; }
        public string? Ban { get; set; }
        public string? Vin { get; set; }
        public string? Novu { get; set; }

        public int? DepartamentId { get; set; }
        public string? DepartamentAdi { get; set; }

        public int? TehkimSurucuId { get; set; }
        public string? TehkimSurucuAdi { get; set; }

        public MasinStatus Status { get; set; }
        public string? Qeyd { get; set; }

        /// <summary>Marka + model + nömrə — siyahılarda tək sətir kimi göstərmək üçün.</summary>
        public string TamAd
        {
            get
            {
                var ad = $"{Marka} {Model}".Trim();
                return ad.Length > 0 ? $"{ad} — {DovletNomresi}" : DovletNomresi;
            }
        }

        public string StatusMetni => Status switch
        {
            MasinStatus.Aktiv => "Aktiv",
            MasinStatus.Temirde => "Təmirdə",
            MasinStatus.IstifadedenCixib => "İstifadədən çıxıb",
            _ => Status.ToString()
        };

        // ── Cari vəziyyət (siyahıda göstərilir) ──────────────────────────────
        /// <summary>Maşın hazırda çöldədirmi (açıq çıxış var).</summary>
        public bool IndiColdedir { get; set; }
        /// <summary>Çöldədirsə — kimdədir.</summary>
        public string? IndiKimde { get; set; }
        /// <summary>Bu gündən sonra bitən aktiv müddət qeydlərinin ən yaxını.</summary>
        public DateTime? EnYaxinMuddetTarixi { get; set; }
        public string? EnYaxinMuddetAdi { get; set; }
    }

    /// <summary>Maşın kartı — yaratmaq/redaktə üçün.</summary>
    public class MasinCreateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Dövlət nömrəsi mütləqdir.")]
        [StringLength(20, ErrorMessage = "Dövlət nömrəsi 20 simvoldan uzun ola bilməz.")]
        [Display(Name = "Dövlət nömrəsi")]
        public string DovletNomresi { get; set; } = "";

        [StringLength(50)] [Display(Name = "Marka")]      public string? Marka { get; set; }
        [StringLength(50)] [Display(Name = "Model")]      public string? Model { get; set; }
        [Range(1900, 2100, ErrorMessage = "Buraxılış ili düzgün deyil.")]
        [Display(Name = "Buraxılış ili")]                 public int? BuraxilisIli { get; set; }
        [StringLength(30)] [Display(Name = "Rəng")]       public string? Reng { get; set; }
        [StringLength(50)] [Display(Name = "Ban / şassi")] public string? Ban { get; set; }
        [StringLength(50)] [Display(Name = "VIN")]        public string? Vin { get; set; }
        [StringLength(50)] [Display(Name = "Növü")]       public string? Novu { get; set; }

        [Display(Name = "Departament")]      public int? DepartamentId { get; set; }
        [Display(Name = "Təhkim olunmuş sürücü")] public int? TehkimSurucuId { get; set; }
        [Display(Name = "Status")]           public MasinStatus Status { get; set; } = MasinStatus.Aktiv;
        [StringLength(500)] [Display(Name = "Qeyd")] public string? Qeyd { get; set; }
    }
}
