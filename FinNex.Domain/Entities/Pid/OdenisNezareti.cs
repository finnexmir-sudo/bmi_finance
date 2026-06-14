namespace FinNex.Domain.Entities.Pid;

// Balans / balansdan kənar (b-k) ayrımı
public enum BalansNovu
{
    Balans = 0,
    BalansdanKenar = 1
}

// PİD — Əmək haqqı və ödənişə nəzarət (Excel: "ƏMƏK HAQQI VƏ ÖDƏNİŞƏ NƏZARƏT")
public class OdenisNezareti : BaseEntity
{
    public BalansNovu BalansNovu      { get; set; } = BalansNovu.Balans;
    public string  MusteriAdi         { get; set; } = null!;   // müştərinin adı və soyadı
    public string? HesabNomresi       { get; set; }            // hesab №
    public string? Teyinat            { get; set; }            // təyinat
    public DateTime? SonOdenisTarixi  { get; set; }            // gələn pulun son tarixi
    public string? OdenisVeziyyeti    { get; set; }            // ödənişin vəziyyəti
    public string? Qeyd               { get; set; }
}
