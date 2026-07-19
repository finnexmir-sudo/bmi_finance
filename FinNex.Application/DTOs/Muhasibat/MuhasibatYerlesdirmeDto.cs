namespace FinNex.Application.DTOs.Muhasibat;

// Yerləşdirilmiş vəsaitlər — bankın başqa banklara / AMB-yə qoyduğu vəsaitlər
// (aktivin o biri tərəfi, kredit portfelinin güzgüsü). Mənbə: arh_licsch_rs.
// Qalıq = summa × valyuta kursu (AZN); faiz = procstav_rs; müddət = date_planclose.
public class MuhasibatYerlesdirmeDto
{
    public DateTime Tarix  { get; set; }
    public bool     Ugurlu { get; set; }
    public string?  Xeta   { get; set; }

    public decimal UmumiPortfel { get; set; }   // bütün yerləşdirmələr, AZN
    public int     Say          { get; set; }   // açıq yerləşdirmə sayı
    public decimal OrtaFaiz     { get; set; }   // AZN-qalıqla ölçülü orta faiz %
    public decimal IllikGelir   { get; set; }   // gözlənilən illik faiz gəliri = Σ(qalıq×faiz/100)

    // AMB overnight (hesab 11xxx) ayrıca — portfelin böyük hissəsi, likvidlik idarəçiliyi.
    public decimal AmbMebleg { get; set; }
    public int     AmbSay    { get; set; }
    // Banklararası (AMB xaric, hesab 15xxx) — kontragent-bank riski.
    public decimal BanklararasiMebleg { get; set; }
    public int     BanklararasiSay    { get; set; }

    public List<YerlesdirmeKatDto> Kontragentler { get; set; } = new();  // bank üzrə (say/məbləğ/faiz)
    public List<BalansMaddeDto>    ValyutaBolgusu { get; set; } = new();  // AZN / USD / ...
    public List<BalansMaddeDto>    MuddetBolgusu  { get; set; } = new();  // qalıq müddət qutuları
}

public class YerlesdirmeKatDto
{
    public string  Ad    { get; set; } = "";   // kontragent bank adı
    public int     Say   { get; set; }
    public decimal Qaliq { get; set; }          // AZN
    public decimal Faiz  { get; set; }           // orta faiz % (qalıqla ölçülü)
    public decimal Pay   { get; set; }           // portfeldə payı %
    public string  Reng  { get; set; } = "";
}
