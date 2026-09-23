namespace FinNex.Domain.Entities.HR
{
    public class IcazeCixisGiris : BaseEntity
    {
        public int IcazeId { get; set; }
        public Icaze Icaze { get; set; } = null!;

        // Fiziki çıxış/qayıdış vaxtları (cihazdan gəlir)
        public DateTime? CixisVaxt { get; set; }
        public DateTime? QayidisVaxt { get; set; }

        // Birdefelik çıxış: qayıdış gözlənilmir (HR təsdiq zamanı işarələyir)
        public bool Birdefelik { get; set; } = false;

        // Gün bağlananda cihaz oxuması olmayan icazənin vaxtları müraciətdəki
        // PLAN üzrə avtomatik yazılıbsa true (gecə servisi) — audit üçün.
        public bool PlanUzreAvtomatik { get; set; } = false;

        // Faktiki müddət = QayidisVaxt - CixisVaxt (Birdefelik isə hesablanmır).
        //
        // ⚠️ 23.09.2026, KRİTİK: əvvəl `QayidisVaxt.Value > CixisVaxt.Value` şərti
        // YOX idi — cihaz punch-ları qarışıq bağlanarsa (məs. icazə pəncərəsindən
        // KƏNAR bir çıxış səhvən bu qeydə bağlanıb, gecə xidməti isə çatışmayan
        // qayıdışı PLAN üzrə dolduranda) fərq MƏNFİ çıxa bilirdi (məs. çıxış 17:03,
        // qayıdış 12:45 → −4,3 saat). Bu mənfi ədəd sonra `Math.Max(0, ...)` nahar
        // düzəlişinə düşəndə TƏSADÜFƏN 0-a sıxılırdı — «0 saat» göstərilirdi, halbuki
        // bu, real ölçmə DEYİL, məntiqsiz məlumatın gizli məhsuludur.
        // İndi bu cüt mənasız olanda `null` qaytarılır — çağıran tərəf (GetDovriyyeAsync)
        // bunu «ölçülə bilmir» sayıb PLAN üzrə sayır (istifadəçi qərarı, 23.09.2026:
        // «işçi icazə yazıb getməyibsə, bu onun problemidir, sistem plan qədər hesablasın»).
        public double? FaktikiSaat =>
            (!Birdefelik && CixisVaxt.HasValue && QayidisVaxt.HasValue && QayidisVaxt.Value > CixisVaxt.Value)
                ? (QayidisVaxt.Value - CixisVaxt.Value).TotalHours
                : null;

        public IcazeCixisGirisStatus Status { get; set; } = IcazeCixisGirisStatus.Gozlenir;

        // ── Plan üzrə sayımın HR tərəfindən ləğvi (23.09.2026) ────────────
        // Qeyd bağlanıb, amma FaktikiSaat ölçülə bilmirsə (yuxarıdakı `FaktikiSaat`
        // null qayıdır), balans tərəfi bunu PLAN qədər hesablayır (istifadəçi qərarı:
        // «işçi icazə yazıb getməyibsə, bu onun problemidir»). Amma işçinin ÜZÜRLÜ
        // səbəbi ola bilər (məs. təcili çağırılıb, rəhbər saxlayıb) — HR bu KONKRET
        // qeydin plan-sayımını ləğv edə bilsin deyə bu sahələr əlavə olunub.
        //
        // ⚠️ İcazənin ÖZÜNƏ (Icaze.Status) TOXUNMUR — icazə təsdiqlənmiş və
        // tarixçədə qalır, yalnız BALANSDAN düşən saat ləğv olunur.
        public bool PlanSayimiLegvEdildi { get; set; } = false;
        public string? PlanSayimiLegvSebebi { get; set; }
        public DateTime? PlanSayimiLegvTarixi { get; set; }
        public int? PlanSayimiLegvEdenIsciId { get; set; }
    }

    public enum IcazeCixisGirisStatus
    {
        Gozlenir   = 1,  // Hələ çıxmayıb
        Cixdi      = 2,  // Çıxdı, qayıdış gözlənilir
        Tamamlandi = 3,  // Qayıdış qeyd olundu / birdefelik tamamlandı
        LegvEdildi = 4   // İcazə ləğv edildi
    }
}
