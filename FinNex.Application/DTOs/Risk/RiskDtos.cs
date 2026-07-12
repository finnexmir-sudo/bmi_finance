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
