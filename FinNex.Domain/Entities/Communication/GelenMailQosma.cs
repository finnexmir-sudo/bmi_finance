namespace FinNex.Domain.Entities.Communication;

public class GelenMailQosma : BaseEntity
{
    public int GelenMailId { get; set; }
    public string FaylAdi { get; set; } = null!;
    public string ContentType { get; set; } = "";
    public long OlcuBayt { get; set; }
    public string FaylYolu { get; set; } = "";      // disk path
    public string? CixarilmisMetin { get; set; }    // PDF/DOCX/XLSX extracted text

    public virtual GelenMail GelenMail { get; set; } = null!;
}
