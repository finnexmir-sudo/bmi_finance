using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class MaasParametriController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public MaasParametriController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /HR/MaasParametri
        public async Task<IActionResult> Index()
        {
            var parametrler = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Nov)
                .ToListAsync();

            var pilleler = await _unitOfWork.Repository<VergiPille>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Nov).ThenBy(x => x.Sira)
                .ToListAsync();

            ViewBag.VergiPilleleri = pilleler;

            ViewData["Title"] = "Vergi Parametrləri";
            return View(parametrler);
        }

        // ───────────────────────────────────────────────────────────
        // VergiPille (pilləli vergi dərəcələri) CRUD
        // ───────────────────────────────────────────────────────────

        // GET /HR/MaasParametri/PilleGetById/5
        [HttpGet]
        public async Task<IActionResult> PilleGetById(int id)
        {
            var p = await _unitOfWork.Repository<VergiPille>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (p == null)
                return Json(new { success = false, message = "Pillə tapılmadı." });

            return Json(new
            {
                success = true,
                data = new
                {
                    p.Id,
                    Nov = (int)p.Nov,
                    p.Sira,
                    p.AsagiHedd,
                    p.YuxariHedd,
                    p.Faiz,
                    p.SabitMebleg,
                    p.Aciqlama,
                    BaslamaTarixi = p.BaslamaTarixi.ToString("yyyy-MM-dd"),
                    BitmeTarixi = p.BitmeTarixi?.ToString("yyyy-MM-dd"),
                    p.Aktivdir
                }
            });
        }

        // POST /HR/MaasParametri/PilleCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PilleCreate(
            MaasParametrNovu nov,
            int sira,
            decimal asagiHedd,
            decimal? yuxariHedd,
            decimal faiz,
            decimal sabitMebleg,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi)
        {
            if (faiz < 0)
                return Json(new { success = false, message = "Faiz mənfi ola bilməz." });

            if (asagiHedd < 0)
                return Json(new { success = false, message = "Aşağı hədd mənfi ola bilməz." });

            if (yuxariHedd.HasValue && yuxariHedd <= asagiHedd)
                return Json(new { success = false, message = "Yuxarı hədd aşağı həddən böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            var entity = new VergiPille
            {
                Nov = nov,
                Sira = sira,
                AsagiHedd = asagiHedd,
                YuxariHedd = yuxariHedd,
                Faiz = faiz,
                SabitMebleg = sabitMebleg,
                Aciqlama = aciqlama,
                BaslamaTarixi = baslamaTarixi,
                BitmeTarixi = bitmeTarixi,
                Aktivdir = true
            };

            await _unitOfWork.Repository<VergiPille>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Pillə uğurla əlavə edildi." });
        }

        // POST /HR/MaasParametri/PilleEdit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PilleEdit(
            int id,
            MaasParametrNovu nov,
            int sira,
            decimal asagiHedd,
            decimal? yuxariHedd,
            decimal faiz,
            decimal sabitMebleg,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi,
            bool aktivdir)
        {
            var entity = await _unitOfWork.Repository<VergiPille>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Pillə tapılmadı." });

            if (faiz < 0)
                return Json(new { success = false, message = "Faiz mənfi ola bilməz." });

            if (asagiHedd < 0)
                return Json(new { success = false, message = "Aşağı hədd mənfi ola bilməz." });

            if (yuxariHedd.HasValue && yuxariHedd <= asagiHedd)
                return Json(new { success = false, message = "Yuxarı hədd aşağı həddən böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            entity.Nov = nov;
            entity.Sira = sira;
            entity.AsagiHedd = asagiHedd;
            entity.YuxariHedd = yuxariHedd;
            entity.Faiz = faiz;
            entity.SabitMebleg = sabitMebleg;
            entity.Aciqlama = aciqlama;
            entity.BaslamaTarixi = baslamaTarixi;
            entity.BitmeTarixi = bitmeTarixi;
            entity.Aktivdir = aktivdir;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Pillə uğurla yeniləndi." });
        }

        // POST /HR/MaasParametri/PilleDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PilleDelete(int id)
        {
            var entity = await _unitOfWork.Repository<VergiPille>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Pillə tapılmadı." });

            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.Aktivdir = false;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Pillə uğurla silindi." });
        }

        // GET /HR/MaasParametri/GetById/5
        [HttpGet]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (p == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            return Json(new
            {
                success = true,
                data = new
                {
                    p.Id,
                    Nov = (int)p.Nov,
                    Tip = (int)p.Tip,
                    p.Deyer,
                    p.Aciqlama,
                    BaslamaTarixi = p.BaslamaTarixi.ToString("yyyy-MM-dd"),
                    BitmeTarixi = p.BitmeTarixi?.ToString("yyyy-MM-dd"),
                    p.Aktivdir
                }
            });
        }

        // POST /HR/MaasParametri/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MaasParametrNovu nov,
            MaasParametrTipi tip,
            decimal deyer,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi)
        {
            if (deyer <= 0)
                return Json(new { success = false, message = "Dəyər 0-dan böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            // ── Köhnə aktiv sətrləri avtomatik bağla ─────────────
            // Eyni növdən (nov) olan və yeni sətrin başlama tarixi ilə
            // üst-üstə düşən aktiv sətrləri tap, onların BitmeTarixi-ni
            // yeni BaslamaTarixi - 1 gün kimi qoy
            var yeniBitmeTarixi = baslamaTarixi.AddDays(-1);

            var kohneAktiv = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x =>
                    x.Nov == nov &&
                    x.Aktivdir &&
                    !x.Silinib &&
                    x.BaslamaTarixi < baslamaTarixi &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= baslamaTarixi))
                .ToListAsync();

            foreach (var kohne in kohneAktiv)
            {
                kohne.BitmeTarixi = yeniBitmeTarixi;
            }

            // Eyni və ya daha sonrakı BaslamaTarixi ilə aktiv sətr varsa —
            // onları sadəcə deaktiv et (tarix intervalı qurmaq mümkün deyil)
            var gelecekAktiv = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x =>
                    x.Nov == nov &&
                    x.Aktivdir &&
                    !x.Silinib &&
                    x.BaslamaTarixi >= baslamaTarixi)
                .ToListAsync();

            foreach (var gelecek in gelecekAktiv)
            {
                gelecek.Aktivdir = false;
            }

            var entity = new MaasParametri
            {
                Nov = nov,
                Tip = tip,
                Deyer = deyer,
                Aciqlama = aciqlama,
                BaslamaTarixi = baslamaTarixi,
                BitmeTarixi = bitmeTarixi,
                Aktivdir = true
            };

            await _unitOfWork.Repository<MaasParametri>().YaratAsync(entity);
            await _unitOfWork.YaddaSaxlaAsync();

            var bagliSayi = kohneAktiv.Count + gelecekAktiv.Count;
            var mesaj = bagliSayi > 0
                ? $"Parametr əlavə edildi. {bagliSayi} köhnə aktiv sətr avtomatik bağlandı."
                : "Parametr uğurla əlavə edildi.";

            return Json(new { success = true, message = mesaj });
        }

        // POST /HR/MaasParametri/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            MaasParametrNovu nov,
            MaasParametrTipi tip,
            decimal deyer,
            string? aciqlama,
            DateTime baslamaTarixi,
            DateTime? bitmeTarixi,
            bool aktivdir)
        {
            var entity = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            if (deyer <= 0)
                return Json(new { success = false, message = "Dəyər 0-dan böyük olmalıdır." });

            if (bitmeTarixi.HasValue && bitmeTarixi <= baslamaTarixi)
                return Json(new { success = false, message = "Bitmə tarixi başlama tarixindən sonra olmalıdır." });

            entity.Nov = nov;
            entity.Tip = tip;
            entity.Deyer = deyer;
            entity.Aciqlama = aciqlama;
            entity.BaslamaTarixi = baslamaTarixi;
            entity.BitmeTarixi = bitmeTarixi;
            entity.Aktivdir = aktivdir;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Parametr uğurla yeniləndi." });
        }

        // POST /HR/MaasParametri/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);

            if (entity == null)
                return Json(new { success = false, message = "Parametr tapılmadı." });

            entity.Silinib = true;
            entity.SilinmeTarixi = DateTime.Now;
            entity.Aktivdir = false;

            await _unitOfWork.YaddaSaxlaAsync();

            return Json(new { success = true, message = "Parametr uğurla silindi." });
        }
    }
}
