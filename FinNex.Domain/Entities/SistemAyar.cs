namespace FinNex.Domain.Entities;

public class SistemAyar
{
    public int Id { get; set; }

    // Kredit IMAP ayarları
    public string KreditImapServer   { get; set; } = "imap.titan.email";
    public int    KreditImapPort     { get; set; } = 993;
    public string KreditImapEmail    { get; set; } = "";
    public string KreditImapPassword { get; set; } = "";

    // PİD toplu SMS
    public int? PidTopluSmsOracleSorguId  { get; set; }
    public int? PidOdenisGunuSorguId      { get; set; }
    public int? PidZaminlerSorguId        { get; set; }

    // PİD Məhkəmə İşləri — Oracle sorğusu (qeydiyyat № ilə tək kredit axtarışı, &nomre)
    public int? PidMehkemeSorguId         { get; set; }

    // PİD Məhkəmə İşləri — tam siyahı sorğusu (parametrsiz, bütün problemli kreditlər + zaminlər)
    public int? PidMehkemeSiyahiSorguId   { get; set; }
}
