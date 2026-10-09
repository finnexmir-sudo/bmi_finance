using FinNex.Application.DTOs.Kassa;

namespace FinNex.UI.Areas.Kassa.ViewModels
{
    public class ExchangeIndexVM
    {
        public KassaKursGunlukDto Gunluk { get; set; } = null!;
        public IList<KassaKursSiyahiDto> SonQeydler { get; set; } = new List<KassaKursSiyahiDto>();

        // Mərkəzi Bank (CBAR) rəsmi kursu, bugünkü gün — kassirə istinad üçün.
        // Mənbə: IBmiValyutaService (Oracle kurval, YALNIZ SELECT). Oracle
        // əlçatmaz olsa / bugünkü kurs hələ dərc olunmayıbsa null qayıdır —
        // tilin özü YENƏ DƏ göstərilir ("—" + izah ilə), forma bloklanmır.
        public decimal? UsdMbKurs { get; set; }
        public decimal? AvroMbKurs { get; set; }
        public DateTime MbTarix { get; set; }
    }
}
