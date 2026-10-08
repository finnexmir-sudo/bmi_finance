namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Bir Qara Jetonun kaskadının (bax <c>JetonService.QaraJetonKesintisiTetbiqEtAsync</c>)
    /// hər bir atomik addımının qeydi — 08.10.2026, "Qara Jetonu ləğv etsəm kəsilən
    /// geri qayıdır?" tələbinə görə.
    ///
    /// <see cref="IsciJetonu.QaraJetonId"/> TƏK bir audit sahəsidir və eyni hədəf
    /// jeton bir neçə Qara Jetondan təsirlənərsə (nadir, amma mümkün) üzərinə yazıla
    /// bilər — bu cədvəl isə HƏR addımı AYRI sətirdə saxlayır, ona görə ləğv edəndə
    /// DƏQİQ miqdarı (nə qədər götürülübsə, o qədərini) geri qaytarmaq mümkündür.
    ///
    /// FK YOXDUR (qəsdən) — <see cref="IsciJetonu"/> və <see cref="QaraJetonBorcu"/>-ya
    /// sadə int istinad, CLAUDE.md-dəki "audit üçün sadə int" qaydasına uyğun.
    /// </summary>
    public class QaraJetonKesinti : BaseEntity
    {
        /// <summary>Kəsintini yaradan Qara Jeton — IsciJetonu.Id (Nov=Menfi).</summary>
        public int QaraJetonId { get; set; }

        public QaraJetonKesintiTuru Tur { get; set; }

        /// <summary>Tur=MusbetKesinti/IllikHuquqKesinti/BorcOdenisi olanda — təsirlənən IsciJetonu.Id.</summary>
        public int? HedefJetonId { get; set; }

        /// <summary>Tur=BorcYarandi/BorcOdenisi olanda — QaraJetonBorcu.Id.</summary>
        public int? BorcId { get; set; }

        /// <summary>Bu addımda hərəkət edən saat miqdarı (mütləq qiymət).</summary>
        public decimal Miqdar { get; set; }

        /// <summary>Qara Jeton ləğv edilərkən bu addım artıq geri qaytarılıbmı.</summary>
        public bool GeriQaytarilib { get; set; }
    }
}
