namespace FinNex.Domain.Entities.Pid;

// Məhkəmə işində zamin — öz icra qatı ilə (icra subyekti).
// Zamin də əmək haqqına tutulma, həbs, stop və s. icra işlərinə məruz qalır.
public class MehkemeZamin : BaseEntity
{
    public int MehkemeIsiId { get; set; }   // hansı məhkəmə işinə aiddir (FK)

    // ── Kimlik (Oracle-dan və ya əl ilə) ──
    public string  Ad          { get; set; } = null!;
    public string? Fin         { get; set; }
    public string? DogumTarixi { get; set; }   // mətn (məs. "18.01.1990")
    public string? Telefon     { get; set; }
    public string? Unvan       { get; set; }

    // ── İcra qatı (zaminin öz icra işləri) ──
    public string? EmekHaqqiTutulma { get; set; }   // əmək haqqına tutulma
    public string? IsYeri           { get; set; }
    public string? EmlakaHebs       { get; set; }
    public string? Stop             { get; set; }
    public string? IcraMemuru       { get; set; }
    public string? IcraSonIsler     { get; set; }
    public string? DypSorgu         { get; set; }
    public string? AdinaSorgu       { get; set; }
    public string? IcraQeyd         { get; set; }
}
