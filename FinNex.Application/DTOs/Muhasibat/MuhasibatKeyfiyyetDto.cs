namespace FinNex.Application.DTOs.Muhasibat;

// Kredit Keyfiyyəti & Ehtiyat — kreditlərin təsnifatı (ehtiyat dərəcəsi üzrə),
// ehtiyat örtüyü, girov/LTV, restrukturizasiya. Mənbə: arh_licschkre
// (procstavrez/procstavrez_19 = ehtiyat %), girovun_bazar_deyeri (girov).
public class MuhasibatKeyfiyyetDto
{
    public DateTime Tarix   { get; set; }
    public bool     Ugurlu  { get; set; }
    public string?  Xeta    { get; set; }

    public decimal Portfel        { get; set; }   // əsas + VK, AZN
    public decimal Ehtiyat         { get; set; }   // ümumi ehtiyat (provision)
    public decimal EhtiyatFaiz     { get; set; }   // ehtiyat / portfel, %
    public decimal ProblemliQaliq  { get; set; }   // qeyri-standart+şübhəli+ümidsiz (≥20% ehtiyat)
    public decimal Ortuyu          { get; set; }   // ehtiyat / problemli qalıq, % (coverage)
    public int     MuqavileSayi    { get; set; }

    public List<KeyfiyyetKatDto> Kateqoriyalar { get; set; } = new();  // təsnifat qrupları

    // Restrukturizasiya
    public int     RestruktSay   { get; set; }
    public decimal RestruktQaliq { get; set; }

    // Girov / LTV
    public int     GirovluSay    { get; set; }
    public decimal GirovluQaliq  { get; set; }
    public int     GirovsuzSay   { get; set; }
    public decimal GirovsuzQaliq { get; set; }
    public decimal GirovCem      { get; set; }
    public decimal OrtaLtv       { get; set; }   // təminatlı qalıq / girov, %
}

public class KeyfiyyetKatDto
{
    public string  Ad      { get; set; } = "";   // Standart / Nəzarət altında / ...
    public int     Say     { get; set; }
    public decimal Qaliq   { get; set; }
    public decimal Ehtiyat { get; set; }
    public decimal Faiz    { get; set; }          // qalığın portfeldə payı %
    public string  Reng    { get; set; } = "";    // ciddiliyə görə rəng (yaşıl→qırmızı)
}
