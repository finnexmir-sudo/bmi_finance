using Microsoft.AspNetCore.Mvc;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;

namespace FinNex.UI.Areas.PR_Odenis_Tapsirigi.Controllers;

[Authorize]
[Area("PR_Odenis_Tapsirigi")]
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

    // DÜZƏLİŞ – GET
    public async Task<IActionResult> Duzelis(int id)
    {
        var bank = await _uow.Repository<Bank>().IdIleGetirAsync(id);
        if (bank == null) return NotFound();
        return View(bank);
    }

    // DÜZƏLİŞ – POST
    [HttpPost]
    public async Task<IActionResult> Duzelis(Bank bank)
    {
        if (ModelState.IsValid)
        {
            await _uow.Repository<Bank>().YenileAsync(bank);
            await _uow.YaddaSaxlaAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(bank);
    }

    // SİL – POST
    [HttpPost]
    public async Task<IActionResult> Sil(int id)
    {
        await _uow.Repository<Bank>().YumshakSilAsync(id);
        await _uow.YaddaSaxlaAsync();
        return RedirectToAction(nameof(Index));
    }
}