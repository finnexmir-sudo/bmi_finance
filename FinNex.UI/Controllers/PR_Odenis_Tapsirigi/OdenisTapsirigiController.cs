using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Controllers
{
    public class OdenisTapsirigiController : Controller
    {
        private readonly IUnitOfWork _uow;

        public OdenisTapsirigiController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // 📄 SİYAHI
        public async Task<IActionResult> Index()
        {
            var list = await _uow
                .Repository<OdenisTapsirigi>()
                .HamisiniGetirAsync(
                    include: q => q
                        .Include(x => x.OduyenMusteri)
                        .Include(x => x.AlanMusteri)
                );

            return View(list);
        }


        // ➕ YENİ FORM
        public async Task<IActionResult> Create()
        {
            var vm = new OdenisTapsirigiCreateVM
            {
                Banklar = (await _uow.Repository<Bank>()
                                .HamisiniGetirAsync())
                                .ToList(),

                Musteriler = (await _uow.Repository<Musteri>()
                                    .HamisiniGetirAsync())
                                    .ToList()
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Create(OdenisTapsirigiCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                // dropdown-lar boş qalmasın deyə
                vm.Banklar = (await _uow.Repository<Bank>().HamisiniGetirAsync()).ToList();
                vm.Musteriler = (await _uow.Repository<Musteri>().HamisiniGetirAsync()).ToList();
                return View(vm);
            }

            await _uow.Repository<OdenisTapsirigi>()
                      .YaratAsync(vm.Odenis);

            await _uow.YaddaSaxlaAsync();

            return RedirectToAction(nameof(Index));
        }

    }

}
