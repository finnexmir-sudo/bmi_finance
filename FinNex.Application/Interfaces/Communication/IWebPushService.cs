namespace FinNex.Application.Interfaces.Communication
{
    public interface IWebPushService
    {
        Task AboneOlAsync(int isciId, string endpoint, string p256dh, string auth);
        Task AbonelikSilAsync(int isciId, string endpoint);
        Task BildirisSonderAsync(int isciId, string bashliq, string metn, string? url = null);
        Task HamisinaSonderAsync(string bashliq, string metn, string? url = null);
    }
}
