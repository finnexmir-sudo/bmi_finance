using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication;

public class GelenMail : BaseEntity
{
    public string MessageId { get; set; } = null!;   // IMAP Message-ID header
    public string KimdenAd { get; set; } = "";
    public string KimdenEmail { get; set; } = null!;
    public string Movzu { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public string MetinDuz { get; set; } = "";
    public DateTime AlinmaTarixi { get; set; }
    public bool Oxunub { get; set; } = false;
    public DateTime? OxunmaTarixi { get; set; }

    // AI təhlili
    public string? AIXulase { get; set; }
    public DateTime? AITahlilTarixi { get; set; }

    // Cavab durumu
    public bool CavabVerildi { get; set; } = false;

    // Sənəd dövriyyəsinə bağlantı (əgər əlavə edilibsə)
    public int? SenedId { get; set; }

    // Tapşırılan işçi (əsas — geriyəuyğunluq üçün)
    public int? TapalanIsciId { get; set; }
    public int? TapalanIsciTarafindan { get; set; }
    public DateTime? TapalanTarix { get; set; }
    public string? TapalanQeyd { get; set; }

    // Deadline (AI tərəfindən aşkar edilir)
    public DateTime? DedlaynTarix { get; set; }
    public string? DedlaynNov { get; set; }   // Gorus / Hesabat / Muqavile / SonTarix / Diger
    public string? DedlaynQeyd { get; set; }
    public bool DedlaynXatirlatmaYaradildi { get; set; } = false;

    public virtual ICollection<GelenMailQosma> Qosmalar { get; set; } = new List<GelenMailQosma>();
    public virtual ICollection<GelenMailIsci> TapalanIsciler { get; set; } = new List<GelenMailIsci>();
    public virtual Isci? TapalanIsci { get; set; }
}
