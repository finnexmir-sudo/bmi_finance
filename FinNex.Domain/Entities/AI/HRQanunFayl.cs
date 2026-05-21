namespace FinNex.Domain.Entities.AI;

public class HRQanunFayl : BaseEntity
{
    /// <summary>Məs: "Əmək Məcəlləsi 2026"</summary>
    public string Ad { get; set; } = "";

    /// <summary>"emek" | "vergi" | "dsmf" | "itss"</summary>
    public string Kateqoriya { get; set; } = "";

    public string FaylYolu { get; set; } = "";

    public string MetnContent { get; set; } = "";

    public DateTime YaradilmaTarixi { get; set; } = DateTime.Now;
}
