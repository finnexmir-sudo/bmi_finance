using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.DTOs.Pid;

public class OdenisNezaretiDto
{
    public int Id { get; set; }
    public BalansNovu BalansNovu { get; set; }
    public string BalansNovuAd   { get; set; } = "";
    public string MusteriAdi     { get; set; } = "";
    public string? HesabNomresi  { get; set; }
    public string? Teyinat       { get; set; }
    public DateTime? SonOdenisTarixi { get; set; }
    public string? OdenisVeziyyeti   { get; set; }
    public string? Qeyd          { get; set; }
}

public class OdenisNezaretiCreateDto
{
    public BalansNovu BalansNovu { get; set; }
    public string MusteriAdi     { get; set; } = "";
    public string? HesabNomresi  { get; set; }
    public string? Teyinat       { get; set; }
    // input type=date "yyyy-MM-dd" göndərir — string alıb servisdə invariant parse edirik
    // (app az-Latn-AZ mədəniyyətindədir, DateTime birbaşa bind olmur)
    public string? SonOdenisTarixi { get; set; }
    public string? OdenisVeziyyeti { get; set; }
    public string? Qeyd          { get; set; }
}

public class OdenisNezaretiUpdateDto : OdenisNezaretiCreateDto
{
    public int Id { get; set; }
}
