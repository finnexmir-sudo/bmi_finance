using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Pid;

/// <summary>
/// PİD-dən göndərilən SMS-lərin loqu. Göyərçin API ilə inteqrasiya edilib.
/// </summary>
public class PidSmsLog : BaseEntity
{
    public string Telefon { get; set; } = null!;          // 994551234567 formatında
    public string Metn { get; set; } = null!;             // göndərilən son mətn (parametrlər tətbiq edildikdən sonra)

    public int? SablonId { get; set; }                    // hansı şablondan istifadə edildi (yoxsa null = sərbəst mətn)
    public PidSmsSablon? Sablon { get; set; }

    public PidSmsStatus Status { get; set; } = PidSmsStatus.Gozleyir;
    public DateTime? GonderilmeTarixi { get; set; }

    public string? GoyercinId { get; set; }               // Göyərçin tərəfindən qaytarılan ReceiverAccepted.id — status polling üçün
    public string? GatewayCavabi { get; set; }            // API-nin tam cavabı (debug üçün, max 2000 simvol)
    public string? Xeta { get; set; }                     // səhv mesajı

    public int GonderenIsciId { get; set; }
    public Isci GonderenIsci { get; set; } = null!;
}

public enum PidSmsStatus
{
    Gozleyir = 0,         // göndərilməyi gözləyir / API-ya verildi, hələ status yoxdur
    Gonderildi = 1,       // Göyərçin operatora ötürdü (300)
    Catdirildi = 2,       // Müştəriyə çatdı (400)
    Catmadi = 3,          // Çatmadı (402+, mənfi kodlar)
    Xeta = 4              // API səhvi (RejectedReceivers və ya HTTP error)
}
