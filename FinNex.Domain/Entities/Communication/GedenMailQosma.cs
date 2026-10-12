namespace FinNex.Domain.Entities.Communication;

public class GedenMailQosma : BaseEntity
{
    public int GedenMailId { get; set; }
    public string FaylAdi { get; set; } = null!;
    public string ContentType { get; set; } = "";
    public long OlcuBayt { get; set; }
    public string FaylYolu { get; set; } = "";      // disk path
    public string? CixarilmisMetin { get; set; }    // PDF/DOCX/XLSX extracted text

    public virtual GedenMail GedenMail { get; set; } = null!;
}
