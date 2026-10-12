namespace FinNex.Application.Interfaces.Communication;

public interface IGedenMailImapSyncer
{
    Task<int> SyncSentNowAsync(string imapHost, string email, string password, int sahibUserId, CancellationToken ct = default);
}
