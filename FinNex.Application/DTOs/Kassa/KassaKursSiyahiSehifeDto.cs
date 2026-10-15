namespace FinNex.Application.DTOs.Kassa
{
    /// <summary>"Son qeydlər" cədvəlinin səhifələnmiş görünüşü — hər səhifə
    /// BEYANNAMƏ (gün qrupu) sayı ilə ölçülür, sətir sayı ilə yox (hər
    /// beyannamə 5 valyuta sətri doğurur). Default — yalnız ƏN SON beyannamə
    /// (15.10.2026, istifadəçi tələbi: "səhifə belə uzanır, ən sonuncunu verək,
    /// istəsə hamısını seçər, səhifələmə ilə").</summary>
    public class KassaKursSiyahiSehifeDto
    {
        public IList<KassaKursSiyahiDto> Setirler { get; set; } = new List<KassaKursSiyahiDto>();

        public int Sehife { get; set; } = 1;

        /// <summary>Səhifədəki beyannamə sayı. 0 və ya mənfi — "Hamısı" (limitsiz).</summary>
        public int SehifeOlcusu { get; set; } = 1;

        public int UmumiBeyannameSayi { get; set; }

        public bool HamisiSecilib => SehifeOlcusu <= 0;

        public bool EvvelkiVarMi => !HamisiSecilib && Sehife > 1;

        public bool NovbetiVarMi => !HamisiSecilib && Sehife * SehifeOlcusu < UmumiBeyannameSayi;
    }
}
