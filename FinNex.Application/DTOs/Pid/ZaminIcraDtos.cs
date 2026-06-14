namespace FinNex.Application.DTOs.Pid;

// Məhkəmə işində zaminin icra qatı (oxuma)
public class ZaminIcraDto
{
    public int Id            { get; set; }
    public int MehkemeIsiId  { get; set; }
    public string Ad         { get; set; } = "";
    public string? Fin       { get; set; }
    public string? DogumTarixi { get; set; }
    public string? Telefon   { get; set; }
    public string? Unvan     { get; set; }

    public string? EmekHaqqiTutulma { get; set; }
    public string? IsYeri           { get; set; }
    public string? EmlakaHebs       { get; set; }
    public string? Stop             { get; set; }
    public string? IcraMemuru       { get; set; }
    public string? IcraSonIsler     { get; set; }
    public string? DypSorgu         { get; set; }
    public string? AdinaSorgu       { get; set; }
    public string? IcraQeyd         { get; set; }
}

public class ZaminIcraCreateDto
{
    public int MehkemeIsiId  { get; set; }
    public string Ad         { get; set; } = "";
    public string? Fin       { get; set; }
    public string? DogumTarixi { get; set; }
    public string? Telefon   { get; set; }
    public string? Unvan     { get; set; }

    public string? EmekHaqqiTutulma { get; set; }
    public string? IsYeri           { get; set; }
    public string? EmlakaHebs       { get; set; }
    public string? Stop             { get; set; }
    public string? IcraMemuru       { get; set; }
    public string? IcraSonIsler     { get; set; }
    public string? DypSorgu         { get; set; }
    public string? AdinaSorgu       { get; set; }
    public string? IcraQeyd         { get; set; }
}

public class ZaminIcraUpdateDto : ZaminIcraCreateDto
{
    public int Id { get; set; }
}
