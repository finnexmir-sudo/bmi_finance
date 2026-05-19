namespace FinNex.Domain.Entities;

public class SistemAyar
{
    public int Id { get; set; }

    // Kredit IMAP ayarları
    public string KreditImapServer   { get; set; } = "imap.titan.email";
    public int    KreditImapPort     { get; set; } = 993;
    public string KreditImapEmail    { get; set; } = "";
    public string KreditImapPassword { get; set; } = "";
}
