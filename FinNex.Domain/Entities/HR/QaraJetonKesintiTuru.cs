namespace FinNex.Domain.Entities.HR
{
    /// <summary>Bir Qara Jetonun bir atomik kəsinti addımının növü (08.10.2026).</summary>
    public enum QaraJetonKesintiTuru
    {
        /// <summary>Müsbət (mükafat) jetondan FIFO ilə çıxılıb — <see cref="QaraJetonKesinti.HedefJetonId"/>.</summary>
        MusbetKesinti = 1,

        /// <summary>"36 Saat Hüququ" jetonundan çıxılıb — <see cref="QaraJetonKesinti.HedefJetonId"/>.</summary>
        IllikHuquqKesinti = 2,

        /// <summary>Heç nədən ödənə bilməyib, <see cref="QaraJetonKesinti.BorcId"/> borc yazılıb.</summary>
        BorcYarandi = 3,

        /// <summary>Sonradan verilmiş yeni jeton (<see cref="QaraJetonKesinti.HedefJetonId"/>)
        /// gözləyən borcu (<see cref="QaraJetonKesinti.BorcId"/>) ödəyib.</summary>
        BorcOdenisi = 4
    }
}
