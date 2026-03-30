public class IsciMaliyeDto
{
    public int Id { get; set; }
    public int IsciId { get; set; }

    public decimal CariMaas { get; set; }
    public string? Iban { get; set; }
    public string? SosialSigortaNomresi { get; set; }

    public DateTime? SonMaasDeyismeTarixi { get; set; }
    public bool Aktivdir { get; set; }
}
public class CreateIsciMaliyeDto
{
    public int IsciId { get; set; }

    public decimal CariMaas { get; set; }
    public string? Iban { get; set; }
    public string? SosialSigortaNomresi { get; set; }
}
public class UpdateIsciMaliyeDto
{
    public int Id { get; set; }
    public int IsciId { get; set; }

    public decimal CariMaas { get; set; }
    public string? Iban { get; set; }
    public string? SosialSigortaNomresi { get; set; }

    public DateTime? SonMaasDeyismeTarixi { get; set; }
    public bool Aktivdir { get; set; }
}