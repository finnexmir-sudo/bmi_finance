namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Bir IsciHYS qeydinin KONKRET bir ayda "dondurulması" — həmin ay üçün
    /// HYS məbləği maaş/vergi bazasından ÇIXILMIR, sanki o ay üçün HYS qeydi
    /// mövcud deyil. İşçi növbəti ay HYS-i avtomatik davam edir.
    ///
    /// IsciHYS-in BaslamaTarixi/BitmeTarixi-nə TOXUNULMUR — bu, HYS-i
    /// dayandırıb-sonra-yenidən-yaratmaq (iki ayrı tarix aralığı) üsulunun
    /// əvəzinədir. Bir qeyd = bir ay. Eyni HYS-in bir neçə ayı ayrı-ayrı
    /// dondurula bilər (hər biri öz sətri).
    ///
    /// Silinmir, deaktiv edilir (Silinib=true) — MasinMuddetService/IsciGuzest
    /// ilə eyni konvensiya, tarixçə qalsın.
    /// </summary>
    public class IsciHYSDondurma : BaseEntity
    {
        public int IsciHYSId { get; set; }
        public IsciHYS IsciHYS { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public string? Sebeb { get; set; }
    }
}
