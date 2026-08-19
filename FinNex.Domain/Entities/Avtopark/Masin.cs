using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// İdarənin xidməti maşını — maşın kartı.
    ///
    /// Bir maşının eyni anda YALNIZ BİR açıq çıxışı ola bilər
    /// (<c>MasinMuracietStatus.Cixib</c>) — bu qayda servisdə tətbiq olunur,
    /// burada saxlanılmır ki, iki mənbə bir-birindən ayrı düşməsin.
    /// </summary>
    public class Masin : BaseEntity
    {
        /// <summary>Dövlət qeydiyyat nişanı — məs. «10-AA-123». UNİKAL.</summary>
        public string DovletNomresi { get; set; } = null!;

        public string? Marka { get; set; }
        public string? Model { get; set; }
        public int? BuraxilisIli { get; set; }
        public string? Reng { get; set; }

        /// <summary>Texpasportdan — ban/şassi nömrəsi.</summary>
        public string? Ban { get; set; }

        /// <summary>Texpasportdan — VIN.</summary>
        public string? Vin { get; set; }

        /// <summary>Minik / mikroavtobus / yük — sərbəst mətn, siyahı deyil.</summary>
        public string? Novu { get; set; }

        /// <summary>Hansı şöbəyə aiddir. Boş = ümumi istifadə.</summary>
        public int? DepartamentId { get; set; }
        public Departament? Departament { get; set; }

        /// <summary>Təhkim olunmuş sürücü (varsa).</summary>
        public int? TehkimSurucuId { get; set; }
        public Isci? TehkimSurucu { get; set; }

        public MasinStatus Status { get; set; } = MasinStatus.Aktiv;

        /// <summary>
        /// Son bilinən spidometr göstəricisi.
        ///
        /// ⚠️ 19.08.2026 QƏRARI: spidometr İSTİFADƏ OLUNMUR — ekranda sahə yoxdur,
        /// müddət izləməsi yalnız TARİXƏ görədir («yağ dəyişmə — ildə bir dəfə»).
        /// Sütun boş qalır və gələcəkdə km-ə görə izləmə istənilsə YALNIZ formaya
        /// input əlavə olunur; cədvəl dəyişikliyi və deploy riski olmur.
        /// Boş sütunu doldurmağa çalışan kod YAZMA — dəyəri etibarsızdır.
        /// </summary>
        public int? CariKm { get; set; }

        public string? Qeyd { get; set; }

        public ICollection<MasinMuraciet> Muracietler { get; set; } = new List<MasinMuraciet>();
        public ICollection<MasinMuddet> Muddetler { get; set; } = new List<MasinMuddet>();
    }
}
