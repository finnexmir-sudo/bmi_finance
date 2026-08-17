using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class OrganizasiyaController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrganizasiyaController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ── GET /HR/Organizasiya ─────────────────────────────────
        public IActionResult Index()
        {
            ViewData["Title"] = "Organizasiya Sxemi";
            return View();
        }

        // ── GET /HR/Organizasiya/GetOrgData ──────────────────────
        [HttpGet]
        public async Task<IActionResult> GetOrgData()
        {
            // Departamentleri getir
            var departamentler = await _unitOfWork.Repository<Departament>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            // ── AKTIV TƏYİNAT = `Aktivdir`, `BitmeTarixi` YOX (17.08.2026) ──
            // Əvvəl şərt `BitmeTarixi == null` idi. `TeyinatRedakteEtAsync` isə redaktədə
            // `BitmeTarixi`-ni formadan yazır, `Aktivdir`-ə toxunmur → sətir `Aktivdir=1`
            // VƏ `BitmeTarixi=<tarix>` qalır. Bazada 29 təyinatın 22-si məhz belə idi:
            // İşçilər siyahısı (Aktivdir) hamısını göstərirdi, bu səhifə isə cəmi 6-nı
            // sayırdı → departamentlərin çoxu "0 işçi / İşçi yoxdur" görünürdü.
            // `BitmeTarixi` planlaşdırılmış bitmə tarixidir — sətrin bitdiyini SÜBUT ETMİR.
            //
            // ÇIXMIŞ İŞÇİ: təyinat sətri işçi işdən çıxanda avtomatik passivləşmir, ona görə
            // işçinin ÖZ statusu da yoxlanılır — yoxsa çıxmış işçi sxemdə qalar.
            var teyinatlar = await _unitOfWork.Repository<IsciTeyinat>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib
                         && x.Isci.Status == IsciStatus.Aktiv && !x.Isci.Silinib)
                .Include(x => x.Isci)
                .Include(x => x.Vezife)
                .Include(x => x.Departament)
                .ToListAsync();

            // Struktur rollari — çıxmış/silinmiş işçinin rolu sxemdə görünməməlidir
            // (rol sətri işçi çıxanda avtomatik deaktiv olmur).
            var strukturRollar = await _unitOfWork.Repository<IsciStrukturRolu>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib
                         && x.Isci.Status == IsciStatus.Aktiv && !x.Isci.Silinib)
                .Include(x => x.Isci)
                .ToListAsync();

            var orgData = departamentler.Select(dept =>
            {
                var deptIsciler = teyinatlar.Where(t => t.DepartamentId == dept.Id).ToList();
                var deptRollar = strukturRollar.Where(r => r.DepartamentId == dept.Id).ToList();

                // Sobe Reisi
                var sobeReisi = deptRollar
                    .FirstOrDefault(r => r.RolTipi == StrukturRolTipi.SobeReisi);

                // Sıralama: şöbə rəisi öndə, sonra CLAUDE.md-dəki kanonik qayda —
                // Sira (HR-ın drag-and-drop sırası) → Ad → Soyad. Yalnız ada görə
                // sıralamaq "İşçi Sıralaması" səhifəsindəki sıranı gözardı edərdi.
                var iscilerList = deptIsciler
                    .OrderByDescending(t => sobeReisi != null && sobeReisi.IsciId == t.IsciId)
                    .ThenBy(t => t.Isci.Sira)
                    .ThenBy(t => t.Isci.Ad)
                    .ThenBy(t => t.Isci.Soyad)
                    .Select(t => new
                    {
                        Id = t.IsciId,
                        Ad = $"{t.Isci.Ad} {t.Isci.Soyad}",
                        Vezife = t.Vezife?.Ad ?? "—",
                        IsSobeReisi = sobeReisi != null && sobeReisi.IsciId == t.IsciId,
                        RolTipi = deptRollar.FirstOrDefault(r => r.IsciId == t.IsciId)?.RolTipi.ToString()
                    })
                    .ToList();

                return new
                {
                    Id = dept.Id,
                    Ad = dept.Ad,
                    Aciqlama = dept.Aciqlama,
                    IsciSayi = deptIsciler.Count,
                    SobeReisi = sobeReisi != null ? $"{sobeReisi.Isci.Ad} {sobeReisi.Isci.Soyad}" : null,
                    Isciler = iscilerList
                };
            }).ToList();

            // Rehber (ust seviyye)
            var rehberler = strukturRollar
                .Where(r => r.RolTipi == StrukturRolTipi.Rehber)
                .Select(r => new
                {
                    Id = r.IsciId,
                    Ad = $"{r.Isci.Ad} {r.Isci.Soyad}",
                    RolTipi = "Rehber"
                })
                .ToList();

            return Json(new
            {
                Rehberler = rehberler,
                Departamentler = orgData
            });
        }
    }
}
