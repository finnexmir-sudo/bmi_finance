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

    // Zamin hüquqi şəxsdirmi? (formada "Şəxs tipi" seçimi). Hüquqi olduqda
    // vəsiqə/FİN yerinə VÖEN yazılır ({zves1}).
    public bool Huquqi { get; set; }
    public string? Voen { get; set; }
}
