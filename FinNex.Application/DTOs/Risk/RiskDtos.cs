using FinNex.Application.DTOs.Oracle;

namespace FinNex.Application.DTOs.Risk;

// Risk departamentinə təyin olunmuş bir hesabat (OracleSorgular-dan)
public class RiskHesabatDto
{
    public int     Id       { get; set; }
    public string  Ad       { get; set; } = "";
    public string? Mahiyyet { get; set; }
}

// Bir hesabatın icra nəticəsi — dinamik sütunlar + sətirlər
public class RiskNeticeDto
{
    public int          Id       { get; set; }
    public string       Ad       { get; set; } = "";
    public string?      Mahiyyet { get; set; }
    public OracleNetice Netice   { get; set; } = new();
    public int          Say      { get; set; }
}
