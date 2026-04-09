using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FinNex.UI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;

    public ChatHub(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int alanIsciId, string metn)
    {
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // İşçi tapılsın
        var gonderenIsci = await _unitOfWork.Repository<FinNex.Domain.Entities.HR.Isci>()
            .GetirAsync(x => x.AppUserId == userId);
        if (gonderenIsci == null) return;

        var mesaj = new ChatMesaj
        {
            GonderenIsciId = gonderenIsci.Id,
            AlanIsciId = alanIsciId,
            Metn = metn,
            Oxunub = false,
            GonderilmeTarixi = DateTime.Now
        };

        await _unitOfWork.Repository<ChatMesaj>().YaratAsync(mesaj);
        await _unitOfWork.YaddaSaxlaAsync();

        // Alan işçinin AppUserId-sini tapaq
        var alanIsci = await _unitOfWork.Repository<FinNex.Domain.Entities.HR.Isci>()
            .IdIleGetirAsync(alanIsciId);

        if (alanIsci?.AppUserId != null)
        {
            await Clients.Group($"user_{alanIsci.AppUserId}").SendAsync("ReceiveMessage", new
            {
                id = mesaj.Id,
                gonderenIsciId = gonderenIsci.Id,
                gonderenAd = gonderenIsci.TamAd,
                metn = mesaj.Metn,
                tarix = mesaj.GonderilmeTarixi.ToString("HH:mm")
            });
        }

        await Clients.Caller.SendAsync("MessageSent", new
        {
            id = mesaj.Id,
            alanIsciId,
            metn = mesaj.Metn,
            tarix = mesaj.GonderilmeTarixi.ToString("HH:mm")
        });
    }

    public async Task MarkAsRead(int gonderenIsciId)
    {
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var alanIsci = await _unitOfWork.Repository<FinNex.Domain.Entities.HR.Isci>()
            .GetirAsync(x => x.AppUserId == userId);
        if (alanIsci == null) return;

        var oxunmamis = await _unitOfWork.Repository<ChatMesaj>()
            .HamisiniGetirAsync(x => x.GonderenIsciId == gonderenIsciId
                                  && x.AlanIsciId == alanIsci.Id
                                  && !x.Oxunub);

        foreach (var m in oxunmamis)
        {
            m.Oxunub = true;
            await _unitOfWork.Repository<ChatMesaj>().YenileAsync(m);
        }
        await _unitOfWork.YaddaSaxlaAsync();
    }
}
