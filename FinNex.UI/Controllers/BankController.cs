using Microsoft.AspNetCore.Mvc;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Controllers;

public class BankController : Controller
{
    private readonly IUnitOfWork _uow;

    public BankController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // BANKLARIN SİYAHISINI GÖSTƏRİR
    public async Task<IActionResult> Index()
    {
        var banklar = await _uow.Repository<Bank>().HamisiniGetirAsync();
        return View(banklar);
    }

    // YENİ BANK SƏHİFƏSİ
    public IActionResult Yarat() => View();

    [HttpPost]
    public async Task<IActionResult> Yarat(Bank bank)
    {
        if (ModelState.IsValid)
        {
            await _uow.Repository<Bank>().YaratAsync(bank);
            await _uow.YaddaSaxlaAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bank);
    }
}