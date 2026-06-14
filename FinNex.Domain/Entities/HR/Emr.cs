using System.ComponentModel.DataAnnotations.Schema;

namespace FinNex.Domain.Entities.HR
{
    // Əmr reyestri — verilmiş hər rəsmi əmr bir sətirdir (məzuniyyət, maaş dəyişikliyi, ...).
    // Nömrə mərkəzi EmrSayghaci-dən atomik alınır; hər növ üzrə ayrı il-nömrələnir.
    public class Emr : BaseEntity
    {
        public EmrNovu  Nov     { get; set; }
        public int      Nomre   { get; set; }      // rəqəm hissəsi
        public string?  Suffiks { get; set; }      // "a", "b" — adətən null
        public int      Il      { get; set; }
        public DateTime Tarix   { get; set; }

        // Hansı qeydə aiddir (Nov ilə birlikdə cədvəli müəyyən edir, məs. MezuniyyetId)
        public int?     ElaqeliEntityId { get; set; }
        public string?  Metn      { get; set; }
        public string?  SenedYolu { get; set; }
        public string?  SenedAd   { get; set; }
        public int?     VerenIsciId { get; set; }   // əmri verən (HR)

        // Tam nömrə formatı — növə görə (məs. "K/M 12", "MD-2026/001")
        [NotMapped]
        public string TamNomre => Nov switch
        {
            EmrNovu.Mezuniyyet      => $"K/M {Nomre}{Suffiks}",
            EmrNovu.MaasDeyisikliyi => $"MD-{Il}/{Nomre:D3}",
            EmrNovu.IseQebul        => $"İQ-{Il}/{Nomre:D3}",
            EmrNovu.Xitam           => $"X-{Il}/{Nomre:D3}",
            EmrNovu.Ezamiyyet       => $"E-{Il}/{Nomre:D3}",
            _                       => $"{Nomre}{Suffiks}"
        };
    }
}
