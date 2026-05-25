using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Kompensasiya
{
    /// <summary>
    /// Anlıq hesablama nəticəsi (preview — yadda saxlanmayan).
    /// </summary>
    public class KompensasiyaHesablamaNeticesiDto
    {
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string? DepartamentAd { get; set; }
        public string? VezifeAd { get; set; }
        public DateTime IseQebulTarixi { get; set; }
        public DateTime AyrilmaTarixi { get; set; }

        // Keçmiş illər qalığı (illər üzrə)
        public List<KompensasiyaIlDto> KecmisIller { get; set; } = new();
        public decimal KecmisQaligGun { get; set; }

        // Cari anniversary ili
        public int CariIl { get; set; }
        public DateTime AnniversaryBaslangic { get; set; }  // cari məzuniyyət ili başlanğıcı
        public decimal CariIlToplamGun { get; set; }
        public decimal CariIlIstifadeOlunan { get; set; }
        public DateTime? SonuncuMezuniyyetBitmeTarixi { get; set; }
        public int? SonuncuMezuniyyetId { get; set; }
        public bool SonuncuMezuniyyetIsheQebulTarixindenGoturuldu { get; set; }
        public int KecenGunSayi { get; set; }
        public decimal CariIlProrateGun { get; set; }

        // Yekun gün
        public decimal CemiKompensasiyaGun { get; set; }

        // Pul
        public decimal CariMaas { get; set; }
        public decimal Son12AyCemQazanc { get; set; }
        public decimal Son12AyDuzelmisQazanc { get; set; }
        public int Son12AyQeydSayi { get; set; }
        public decimal GunlukMezPul { get; set; }
        public decimal GunlukMaas { get; set; }
        public decimal GunlukRate { get; set; }
        public string Qalib { get; set; } = "MH";  // MH və ya ƏH
        public decimal CemiMebleg { get; set; }

        // Hesablananacaq dövr
        public int HesablananIl { get; set; }
        public int HesablananAy { get; set; }

        // İzahat addımları (transparency üçün)
        public List<string> Izahatlar { get; set; } = new();
        public List<string> Xeberdarliqlar { get; set; } = new();
    }

    public class KompensasiyaIlDto
    {
        public int Il { get; set; }
        public decimal ToplamGun { get; set; }
        public decimal IstifadeOlunanGun { get; set; }
        public decimal QaligGun { get; set; }
    }

    /// <summary>
    /// Yadda saxlama input-u.
    /// </summary>
    public class KompensasiyaYaratDto
    {
        public int IsciId { get; set; }
        public DateTime AyrilmaTarixi { get; set; }
        public int HesablananIl { get; set; }
        public int HesablananAy { get; set; }
        public string? Qeyd { get; set; }
    }

    /// <summary>
    /// Index siyahı sətri.
    /// </summary>
    public class KompensasiyaListDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public DateTime AyrilmaTarixi { get; set; }
        public int HesablananIl { get; set; }
        public int HesablananAy { get; set; }
        public decimal CemiKompensasiyaGun { get; set; }
        public decimal CemiMebleg { get; set; }
        public KompensasiyaStatus Status { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public int? MaasId { get; set; }
    }

    /// <summary>
    /// Detal/breakdown — Index-də sətrə klik edildikdə.
    /// </summary>
    public class KompensasiyaDetalDto : KompensasiyaHesablamaNeticesiDto
    {
        public int Id { get; set; }
        public KompensasiyaStatus Status { get; set; }
        public string? Qeyd { get; set; }
        public int? MaasId { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}
