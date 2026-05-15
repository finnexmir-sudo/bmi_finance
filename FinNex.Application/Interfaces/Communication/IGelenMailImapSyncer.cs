namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailImapSyncer
{
    Task<int> SyncNowAsync(string imapHost, string email, string password, CancellationToken ct = default);
}
