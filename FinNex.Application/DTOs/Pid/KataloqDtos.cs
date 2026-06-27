using FinNex.Application.DTOs.Oracle;

namespace FinNex.Application.DTOs.Pid;

// "Ümumi cari Kataloq" comboboxunda bir seçim (PID departamentinin Oracle sorğusu)
public class KataloqSecimDto
{
    public int    Id { get; set; }
    public string Ad { get; set; } = "";
}

// Kataloq səhifəsinin tam modeli: combobox siyahısı + seçilmiş sorğunun xam nəticəsi
public class KataloqViewDto
{
    public List<KataloqSecimDto> Kataloglar { get; set; } = new();
    public int?    SeciliId  { get; set; }
    public string? SeciliAd  { get; set; }
    public OracleNetice? Netice { get; set; }   // Sutunlar + Setirler (xam)
    public int     Limit     { get; set; }
    public bool    Kesildi   { get; set; }      // sətir sayı limitə çatdısa (daha çox ola bilər)
    public string? Xeta      { get; set; }
}
