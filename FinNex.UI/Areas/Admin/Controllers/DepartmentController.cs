using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Structur;
using FinNex.Application.Services.Structur;
using FinNex.UI.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DepartmentController : Controller
    {
        private readonly IDepartmentService _departmentService;
        private readonly IUserDepartmentService _userDepartmentService;

        public DepartmentController( IDepartmentService departmentService,IUserDepartmentService userDepartmentService)
        {
            _departmentService = departmentService;
            _userDepartmentService = userDepartmentService;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _departmentService.HamisiniGetirAsync();

            var vm = departments.Select(d => new DepartamentVM
            {
                Id = d.Id,
                Ad = d.Ad,
                Aciqlama = d.Aciqlama
            }).ToList();

            return View(vm);
        }


        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartamentVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            await _departmentService.YaratAsync(new DepartmentCreateDto
            {
                Ad = vm.Ad,
                Aciqlama = vm.Aciqlama
            });

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Users(int id)
        {
            var department = await _departmentService.IdIleGetirAsync(id);
            if (department == null)
                return NotFound();

            var users = await _userDepartmentService
                .GetUsersByDepartmentAsync(id);

            ViewBag.DepartmentName = department.Ad;

            return View(users);
        }

    }
}
