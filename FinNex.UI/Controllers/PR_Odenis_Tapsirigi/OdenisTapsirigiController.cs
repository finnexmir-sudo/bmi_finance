using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;
using FinNex.UI.Services.PR_Odenis_Tapsirigi;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Controllers
{
    [Authorize]
    public class OdenisTapsirigiController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IOdenisTapsirigiNomreService _odenisNomreService;
        public OdenisTapsirigiController(IUnitOfWork uow, IOdenisTapsirigiNomreService odenisNomreService)
        {
            _uow = uow;
            _odenisNomreService = odenisNomreService;
        }

        // Ana hub sehifesi
        public IActionResult Index()
        {
            return View();

        }

        // Odenis tapsiriglarinin siyahisi
        public async Task<IActionResult> Siyahi()
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


        [HttpPost]
        public async Task<IActionResult> GenerateWord([FromBody] OdenisTapsirigiWordDto dto)
        {
            int nomre = await _odenisNomreService.NovbetiNomreAlAsync();

            dto.Nomre = nomre.ToString("D6"); // 000001
            dto.Tarix = DateTime.Now.ToString("dd MMMM yyyy");

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Files",
                "Word",
                "Odenis_tapsirigi.docx"
            );

            var bytes = OdenisTapsirigiWordService.GenerateFromTemplate(templatePath, dto);

            var fileName = $"OdenisTapsirigi_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );
        }


    }

}
