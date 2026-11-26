namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Isci avans muracietleri.
    /// Isci her ay 1 defe avans iste biler (maasin max 30%-i).
    /// Muhasib tesdiq edir, ay sonu maas hesablananda NET-den cixilir.
    /// </summary>
    public class Avans : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal Mebleg { get; set; }
        public string? Sebeb { get; set; }

        public AvansStatus Status { get; set; } = AvansStatus.Gozlemede;

        public int? MuhasibId { get; set; }
        public Isci? Muhasib { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public string? ImtinaSebebi { get; set; }

        /// <summary>
        /// Mühasib pravodkanı BMI-də vurub, "ulduzları" götürüb, pul HƏQİQƏTƏN
        /// çıxarıla bilən olanda bu tarix yazılır (26.11.2026). `TesdiqTarixi`
        /// yalnız MÜRACİƏTİN təsdiqləndiyini göstərir — pulun hazır olduğunu
        /// YOX. Bu sahə dolu olanda `Status == Odenilib`-dir.
        /// </summary>
        public DateTime? OdenisTarixi { get; set; }

        /// <summary>
        /// Qabaqcadan ödənilmiş məzuniyyətin ikinci (və sonrakı) ayına görə
        /// SİSTEM tərəfindən avtomatik yaradılan ƏVƏZLƏŞMƏ qeydi (26.11.2026).
        ///
        /// NİYƏ VAR: qabaqcadan ödənilmiş məzuniyyət bir neçə təqvim ayına
        /// düşəndə, mühasib indiyədək HƏR sonrakı ay üçün əl ilə bir "Avans"
        /// sətri yazırdı ki, artıq birinci ayda ödənilmiş pul maaş hesablamasında
        /// İKİNCİ dəfə (NET-dən) çıxılmasın. Bu sahə həmin əl işini
        /// MezuniyyetAvansService vasitəsilə avtomatlaşdırır.
        ///
        /// Null = işçinin öz adi (ixtiyari) avans müraciəti.
        /// Dolu = bu konkret Mezuniyyet-in ödənişinin ƏVƏZLƏŞMƏ payıdır —
        ///   işçinin özü BU qeydi görmür/ləğv edə bilmir (User/AvansController
        ///   "bu ay artıq avans götürülüb" yoxlaması bunu xaric edir), Mühasib
        ///   PravodkaExport-da bunu AYRI bir mühasibat hesabına (
        ///   "MezQabaqcadanAvansDebet") yazır — adi avansla QARIŞDIRILMIR.
        /// </summary>
        public int? MezuniyyetId { get; set; }
        public Mezuniyyet? Mezuniyyet { get; set; }
    }
}
