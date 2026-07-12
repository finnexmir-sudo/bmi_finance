using FinNex.Application.DTOs.Oracle;

namespace FinNex.Application.DTOs.Risk;

// Risk departamentinə təyin olunmuş bir hesabat (OracleSorgular-dan)
public class RiskHesabatDto
{
    public int     Id       { get; set; }
    public string  Ad       { get; set; } = "";
    public string? Mahiyyet { get; set; }
}

// Hesabatın SQL-ində olan parametr token-ləri
public class RiskParametrler
{
    public bool BasTarix { get; set; }   // {BASTARIX}
    public bool SonTarix { get; set; }   // {SONTARIX}
    public bool Tarix    { get; set; }   // {TARIX}
    public bool Hedd     { get; set; }   // {HEDD}
    public bool Il       { get; set; }   // {IL}

    public bool VarMi => BasTarix || SonTarix || Tarix || Hedd || Il;
}

// İstifadəçinin daxil etdiyi parametr dəyərləri (xam)
public class RiskParametrDeyer
{
    public string? BasTarix { get; set; }   // yyyy-MM-dd
    public string? SonTarix { get; set; }
    public string? Tarix    { get; set; }
    public string? Hedd     { get; set; }
    public string? Il       { get; set; }
}

// Dashboard — tək rəqəmli göstərici kartı ([KPI] tag-li sorğudan)
public class RiskKpiDto
{
    public int     Id     { get; set; }
    public string  Ad     { get; set; } = "";
    public string? Alt    { get; set; }              // Mahiyyət-in tag-dan sonrakı hissəsi
    public string  Deyer  { get; set; } = "0";       // formatlanmış rəqəm və ya mətn
    public bool    Reqem  { get; set; } = true;      // dəyər rəqəmdir?
    public string? Xeta   { get; set; }
}

// Dashboard — qrafik ([PIE]/[BAR]/[LINE] tag-li sorğudan: sütun0=etiket, sonuncu=dəyər)
public class RiskChartDto
{
    public int           Id         { get; set; }
    public string        Ad         { get; set; } = "";
    public string        Tip        { get; set; } = "bar";   // bar|pie|line
    public List<string>  Etiketler  { get; set; } = new();
    public List<decimal> Deyerler   { get; set; } = new();
    public string?       Xeta       { get; set; }
    public bool          Bosdur => Etiketler.Count == 0 && string.IsNullOrEmpty(Xeta);
}

// Dashboard — bütün widget-lər + adi hesabat kartları
public class RiskPanelDto
{
    public List<RiskKpiDto>      Kpiler      { get; set; } = new();
    public List<RiskChartDto>    Qrafikler   { get; set; } = new();
    public List<RiskHesabatDto>  Hesabatlar  { get; set; } = new();   // tag-siz (klikləyəndə cədvəl)
    public bool Bosdur => Kpiler.Count == 0 && Qrafikler.Count == 0 && Hesabatlar.Count == 0;
}

// Bir hesabatın icra nəticəsi — dinamik sütunlar + sətirlər
public class RiskNeticeDto
{
    public int             Id           { get; set; }
    public string          Ad           { get; set; } = "";
    public string?         Mahiyyet     { get; set; }
    public OracleNetice    Netice       { get; set; } = new();
    public int             Say          { get; set; }
    public RiskParametrler Parametrler  { get; set; } = new();
    public RiskParametrDeyer Deyerler   { get; set; } = new();
    public bool            IcraOlundu   { get; set; }   // parametrlər tam olub icra edildimi
    public string?         Xeta         { get; set; }
}
