using FinNex.Application.Common.Results;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.HR
{
    public class MezuniyyetAvansService : IMezuniyyetAvansService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMaasHesablamaService _maasHesablamaService;

        public MezuniyyetAvansService(
            IUnitOfWork unitOfWork,
            IMaasHesablamaService maasHesablamaService)
        {
            _unitOfWork = unitOfWork;
            _maasHesablamaService = maasHesablamaService;
        }

        public async Task<Result<List<Avans>>> EvezlesmeAvanslariniHazirlaAsync(
            int mezuniyyetId, decimal odenenNet, int? muhasibIsciId)
        {
            var mez = await _unitOfWork.Repository<Mezuniyyet>().IdIleGetirAsync(mezuniyyetId);
            if (mez == null || mez.Silinib)
                return Result<List<Avans>>.Fail("Məzuniyyət müraciəti tapılmadı.");

            if (mez.OdenisTipi != MezuniyyetOdenisTipi.QabaqcadanOdenis)
                return Result<List<Avans>>.Fail("Bu müraciət qabaqcadan ödənişə təyin olunmayıb.");

            if (odenenNet <= 0)
                return Result<List<Avans>>.Fail("Ödənilən net məbləğ sıfırdan böyük olmalıdır.");

            var hesab = await _maasHesablamaService.MezuniyyetOdenisiDetalliHesablaAsync(
                mez.IsciId, mez.BaslamaTarixi, mez.BitmeTarixi);

            // Leave YALNIZ bir aya düşürsə — heç bir əvəzləşmə lazım deyil.
            // Bu, XƏTA DEYİL, ən çox rast gəlinən haldır.
            if (hesab.AySliceleri.Count <= 1)
                return Result<List<Avans>>.Ok(new List<Avans>());

            // Ay-ay (brüt/vergi/net) bölgüsü — Detail səhifəsinin işlətdiyi
            // EYNİ, TƏK mənbə. hedefNet=odenenNet: netlərin cəmi faktiki
            // ödənilən məbləğə qəpiyinə bağlansın (qalıq son aya yazılır).
            var aypaylar = await _maasHesablamaService.MezuniyyetAvansAyPaylariAsync(
                mez.IsciId, mez.BaslamaTarixi, mez.BitmeTarixi, odenenNet);

            var yeniAvanslar = new List<Avans>();

            // İlk slice (AySliceleri[0]) — ödənişin VERİLDİYİ ay, onsuz da
            // həmin ayın öz payroll hesablaması bunu düzgün əhatə edir.
            // Yalnız SONRAKI aylar üçün əvəzləşmə lazımdır (mühasibin əl ilə
            // yazdığı sətrin məhz bu olduğu, Fuad Qulu Zadə hadisəsi ilə
            // təsdiqlənib — bax yuxarıdaki sinif şərhi).
            foreach (var slice in hesab.AySliceleri.Skip(1))
            {
                // Detail səhifəsinin ÖZ naxışı (MezuniyyetOdenisController.Detail)
                // tapılmayan ay-payını sakitcə ötürür (continue), fatal saymır —
                // hər iki metod eyni (isciId, baslama, bitme) ilə çağırıldığı üçün
                // bu, strukturca baş verməməlidir, amma eyni "proven" qaydaya
                // uyğunlaşdırılıb.
                var payi = aypaylar.FirstOrDefault(x => x.Il == slice.Il && x.Ay == slice.Ay);
                if (payi == null || payi.Net <= 0)
                    continue;

                var movcud = await _unitOfWork.Repository<Avans>().Query()
                    .AnyAsync(x => !x.Silinib
                        && x.MezuniyyetId == mezuniyyetId
                        && x.Il == slice.Il
                        && x.Ay == slice.Ay);
                if (movcud)
                    continue; // artıq yaradılıb (manual bərpanın təkrar klikindən qorunma)

                var avans = new Avans
                {
                    IsciId = mez.IsciId,
                    Il = slice.Il,
                    Ay = slice.Ay,
                    Mebleg = payi.Net,
                    Sebeb = $"Qabaqcadan ödənilmiş məzuniyyətin ({mez.BaslamaTarixi:dd.MM.yyyy}–" +
                             $"{mez.BitmeTarixi:dd.MM.yyyy}) {slice.Ay:D2}.{slice.Il} ayına düşən " +
                             "əvəzləşməsi (avtomatik, ikiqat ödəniş olmasın deyə).",
                    Status = AvansStatus.Tesdiqlenib,
                    MuhasibId = muhasibIsciId,
                    TesdiqTarixi = DateTime.Now,
                    MezuniyyetId = mezuniyyetId
                };

                await _unitOfWork.Repository<Avans>().YaratAsync(avans);
                yeniAvanslar.Add(avans);
            }

            return Result<List<Avans>>.Ok(yeniAvanslar);
        }
    }
}
