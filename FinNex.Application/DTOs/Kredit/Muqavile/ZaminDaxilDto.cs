namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>Hazırlama formasında əl ilə daxil edilən zamin (guarantor).</summary>
public class ZaminDaxilDto
{
    public string? Ad { get; set; }
    public string? Pasport { get; set; }     // seriya/nömrə
    public string? Fin { get; set; }
    public string? Telefon { get; set; }
    public string? Unvan { get; set; }
    public string? Olke { get; set; }
    public string? PasportTarixi { get; set; }

    // Zamin hüquqi şəxsdirmi? (formada "Hüquqi şəxs" checkbox). Hüquqi olduqda
    // borcalan kimi VÖEN + direktor məlumatı əl ilə doldurulur; müqavilədə:
    // "hüquqi şəxs {ad} (VÖEN: X), direktoru {ölkə} vətəndaşı {direktor} (vəsiqə: ...)".
    public bool Huquqi { get; set; }
    public string? Voen { get; set; }
    public string? DirektorAd { get; set; }
    public string? DirektorVesiqe { get; set; }
    public string? DirektorOlke { get; set; }      // hüquqi şəxsdə "vətəndaşı" ölkəsi direktora aiddir
}
