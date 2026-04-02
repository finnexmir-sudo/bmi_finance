using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FinNex.Application.BackgroundJobs;

/// <summary>
/// ZKTeco/Datalab cihazına SDK (UDP port 4370) ilə qoşulub
/// davamiyyət məlumatlarını çəkən background servis.
/// </summary>
public class ZkTecoSdkService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkTecoSdkService> _logger;

    private const string DeviceIp = "192.168.0.95";
    private const int DevicePort = 4370;
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    // Cihaz statusu
    public static DateTime? SonElaqa { get; private set; }
    public static bool IsOnline { get; private set; }
    public static string? DeviceSN { get; private set; }

    // ZKTeco UDP protocol constants
    private const ushort CMD_CONNECT = 1000;
    private const ushort CMD_EXIT = 1001;
    private const ushort CMD_ATTLOG_RRQ = 13;
    private const ushort CMD_CLEAR_ATTLOG = 14;
    private const ushort CMD_ACK_OK = 2000;
    private const ushort CMD_ACK_DATA = 2002;
    private const ushort CMD_ACK_UNAUTH = 2005;
    private const ushort CMD_PREPARE_DATA = 1500;
    private const ushort CMD_DATA = 1501;
    private const ushort CMD_FREE_DATA = 1502;

    private ushort _sessionId;
    private ushort _replyNumber;

    public ZkTecoSdkService(
        IServiceScopeFactory scopeFactory,
        ILogger<ZkTecoSdkService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ZkTecoSdkService başladı. Cihaz: {Ip}:{Port} (UDP)", DeviceIp, DevicePort);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PullAttendanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                IsOnline = false;
                _logger.LogError(ex, "ZkTecoSdkService xətası.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PullAttendanceAsync(CancellationToken ct)
    {
        var endpoint = new IPEndPoint(IPAddress.Parse(DeviceIp), DevicePort);
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 5000;
        udp.Client.SendTimeout = 5000;

        try
        {
            udp.Connect(endpoint);
        }
        catch (Exception ex)
        {
            IsOnline = false;
            _logger.LogWarning("Cihaza qoşulmaq mümkün olmadı: {Msg}", ex.Message);
            return;
        }

        _replyNumber = 0;
        _sessionId = 0;

        // 1. Connect
        var connectReply = await SendAndReceiveAsync(udp, CMD_CONNECT, Array.Empty<byte>());
        if (connectReply == null)
        {
            _logger.LogWarning("Cihaz CMD_CONNECT-ə cavab vermədi.");
            IsOnline = false;
            return;
        }

        var replyCmd = GetCommandId(connectReply);
        _logger.LogInformation("CMD_CONNECT cavabı: {Cmd} (gözlənilən: {Expected}), data uzunluğu: {Len}",
            replyCmd, CMD_ACK_OK, connectReply.Length);

        if (replyCmd != CMD_ACK_OK)
        {
            _logger.LogWarning("Cihaz CMD_CONNECT rədd etdi. Cavab kodu: {Cmd}", replyCmd);
            IsOnline = false;
            return;
        }

        _sessionId = GetSessionId(connectReply);
        IsOnline = true;
        SonElaqa = DateTime.Now;
        _logger.LogInformation("Cihaza qoşuldu! SessionId: {SessionId}", _sessionId);

        // 2. Get attendance logs
        var attendanceLogs = await GetAttendanceLogsAsync(udp);

        if (attendanceLogs.Count > 0)
        {
            _logger.LogInformation("{Count} davamiyyət qeydi tapıldı.", attendanceLogs.Count);
            await SaveAttendanceAsync(attendanceLogs);

            // 3. Clear logs from device after saving
            var clearReply = await SendAndReceiveAsync(udp, CMD_CLEAR_ATTLOG, Array.Empty<byte>());
            if (clearReply != null && GetCommandId(clearReply) == CMD_ACK_OK)
            {
                _logger.LogInformation("Cihaz logları təmizləndi.");
            }
        }
        else
        {
            _logger.LogInformation("Yeni davamiyyət qeydi yoxdur.");
        }

        // 4. Disconnect
        await SendAsync(udp, CMD_EXIT, Array.Empty<byte>());

        SonElaqa = DateTime.Now;
    }

    private async Task<List<AttendanceRecord>> GetAttendanceLogsAsync(UdpClient udp)
    {
        var records = new List<AttendanceRecord>();

        var reply = await SendAndReceiveAsync(udp, CMD_ATTLOG_RRQ, Array.Empty<byte>());
        if (reply == null)
        {
            _logger.LogWarning("CMD_ATTLOG_RRQ cavab gəlmədi.");
            return records;
        }

        var cmdId = GetCommandId(reply);
        _logger.LogInformation("ATTLOG cavabı: CMD={Cmd}, Len={Len}", cmdId, reply.Length);

        if (cmdId == CMD_PREPARE_DATA)
        {
            // Böyük data gələcək
            int totalSize = 0;
            if (reply.Length >= 12)
                totalSize = BitConverter.ToInt32(reply, 8);

            _logger.LogInformation("Böyük data gözlənilir: {Size} bytes", totalSize);

            var allData = new List<byte>();

            while (allData.Count < totalSize)
            {
                var dataPacket = await ReceiveAsync(udp);
                if (dataPacket == null) break;

                // Data paketlərinin payload hissəsini əlavə et
                allData.AddRange(dataPacket);
            }

            // Free data buffer
            await SendAndReceiveAsync(udp, CMD_FREE_DATA, Array.Empty<byte>());

            records = ParseAttendanceLogs(allData.ToArray());
        }
        else if (cmdId == CMD_ACK_DATA)
        {
            // Kiçik data tək paketdə
            if (reply.Length > 8)
            {
                records = ParseAttendanceLogs(reply.Skip(8).ToArray());
            }
        }

        return records;
    }

    private List<AttendanceRecord> ParseAttendanceLogs(byte[] data)
    {
        var records = new List<AttendanceRecord>();

        // Əvvəlcə binary format (ZKTeco standart)
        const int recordSize = 40;
        if (data.Length >= recordSize)
        {
            for (int i = 0; i + recordSize <= data.Length; i += recordSize)
            {
                try
                {
                    int userId = BitConverter.ToUInt16(data, i);

                    // ZKTeco encoded timestamp: offset 24, 4 bytes
                    uint encoded = BitConverter.ToUInt32(data, i + 24);
                    var dateTime = DecodeZkTime(encoded);

                    if (dateTime.Year < 2020 || dateTime.Year > 2030) continue;

                    int status = data[i + 28];

                    records.Add(new AttendanceRecord
                    {
                        UserId = userId,
                        DateTime = dateTime,
                        Status = status
                    });
                }
                catch { continue; }
            }
        }

        // Fallback: text format
        if (records.Count == 0 && data.Length > 10)
        {
            try
            {
                var text = Encoding.UTF8.GetString(data);
                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 3) continue;

                    if (!int.TryParse(parts[0].Trim(), out int userId)) continue;
                    if (!DateTime.TryParse(parts[1].Trim(), out DateTime dateTime)) continue;

                    int status = 0;
                    if (parts.Length > 2) int.TryParse(parts[2].Trim(), out status);

                    records.Add(new AttendanceRecord
                    {
                        UserId = userId,
                        DateTime = dateTime,
                        Status = status
                    });
                }
            }
            catch { }
        }

        _logger.LogInformation("Parse nəticəsi: {Count} qeyd, data uzunluğu: {Len}", records.Count, data.Length);
        return records;
    }

    /// <summary>
    /// ZKTeco encoded time: ((year-2000)*12*31+month*31+day)*24*60*60 + hour*60*60 + minute*60 + second
    /// </summary>
    private static DateTime DecodeZkTime(uint encoded)
    {
        int second = (int)(encoded % 60);
        encoded /= 60;
        int minute = (int)(encoded % 60);
        encoded /= 60;
        int hour = (int)(encoded % 24);
        encoded /= 24;
        int day = (int)(encoded % 31) + 1;
        encoded /= 31;
        int month = (int)(encoded % 12) + 1;
        encoded /= 12;
        int year = (int)encoded + 2000;

        try { return new DateTime(year, month, day, hour, minute, second); }
        catch { return DateTime.MinValue; }
    }

    private async Task SaveAttendanceAsync(List<AttendanceRecord> records)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var record in records)
        {
            try
            {
                bool girisdir = record.Status == 0 || record.Status == 4;
                var tarix = record.DateTime.Date;

                var movcud = await db.Davamiyyetler
                    .FirstOrDefaultAsync(x => x.IsciId == record.UserId && x.Tarix == tarix);

                if (movcud == null)
                {
                    var yeni = new Davamiyyet
                    {
                        IsciId = record.UserId,
                        Tarix = tarix,
                        GirisVaxti = girisdir ? record.DateTime : null,
                        CixisVaxti = girisdir ? null : record.DateTime,
                        Status = HesablaStatus(record.DateTime, girisdir)
                    };
                    await db.Davamiyyetler.AddAsync(yeni);
                }
                else
                {
                    if (girisdir)
                    {
                        if (movcud.GirisVaxti == null || record.DateTime < movcud.GirisVaxti)
                        {
                            movcud.GirisVaxti = record.DateTime;
                            movcud.Status = HesablaStatus(record.DateTime, true);
                        }
                    }
                    else
                    {
                        if (movcud.CixisVaxti == null || record.DateTime > movcud.CixisVaxti)
                        {
                            movcud.CixisVaxti = record.DateTime;
                        }
                    }
                }

                await db.SaveChangesAsync();

                _logger.LogInformation(
                    "Davamiyyət yazıldı: IsciId={UserId}, Tarix={Tarix}, {Nov}={Vaxt}",
                    record.UserId, tarix, girisdir ? "Giriş" : "Çıxış", record.DateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Davamiyyət yadda saxlama xətası: UserId={UserId}", record.UserId);
            }
        }
    }

    private static DavamiyyetStatus HesablaStatus(DateTime girisVaxti, bool girisdir)
    {
        if (!girisdir) return DavamiyyetStatus.Isde;

        var isBaslamaVaxti = girisVaxti.Date.AddHours(9);

        return girisVaxti > isBaslamaVaxti.AddMinutes(5)
            ? DavamiyyetStatus.Gecikme
            : DavamiyyetStatus.Isde;
    }

    #region ZKTeco UDP Protocol Helpers

    private async Task<byte[]?> SendAndReceiveAsync(UdpClient udp, ushort command, byte[] data)
    {
        await SendAsync(udp, command, data);
        return await ReceiveAsync(udp);
    }

    private async Task SendAsync(UdpClient udp, ushort command, byte[] data)
    {
        try
        {
            var packet = BuildUdpPacket(command, data);
            await udp.SendAsync(packet, packet.Length);
            _replyNumber++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP paket göndərmə xətası: CMD={Cmd}", command);
        }
    }

    private byte[] BuildUdpPacket(ushort command, byte[] data)
    {
        // UDP payload: [CMD 2B][Checksum 2B][SessionID 2B][ReplyNo 2B][Data ...]
        int payloadSize = 8 + data.Length;
        var payload = new byte[payloadSize];

        // Command ID
        payload[0] = (byte)(command & 0xFF);
        payload[1] = (byte)((command >> 8) & 0xFF);

        // Checksum placeholder
        payload[2] = 0;
        payload[3] = 0;

        // Session ID
        payload[4] = (byte)(_sessionId & 0xFF);
        payload[5] = (byte)((_sessionId >> 8) & 0xFF);

        // Reply Number
        payload[6] = (byte)(_replyNumber & 0xFF);
        payload[7] = (byte)((_replyNumber >> 8) & 0xFF);

        // Data
        if (data.Length > 0)
            Array.Copy(data, 0, payload, 8, data.Length);

        // Calculate checksum
        ushort checksum = CalculateChecksum(payload);
        payload[2] = (byte)(checksum & 0xFF);
        payload[3] = (byte)((checksum >> 8) & 0xFF);

        return payload;
    }

    private static ushort CalculateChecksum(byte[] payload)
    {
        var temp = (byte[])payload.Clone();
        temp[2] = 0;
        temp[3] = 0;

        uint sum = 0;
        int i = 0;
        while (i + 1 < temp.Length)
        {
            sum += (uint)(temp[i] | (temp[i + 1] << 8));
            i += 2;
        }
        if (i < temp.Length)
            sum += temp[i];

        sum = (sum >> 16) + (sum & 0xFFFF);
        sum = (sum >> 16) + (sum & 0xFFFF);
        return (ushort)(~sum & 0xFFFF);
    }

    private async Task<byte[]?> ReceiveAsync(UdpClient udp)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await udp.ReceiveAsync(cts.Token);
            return result.Buffer;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UDP cavab timeout (5 san).");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("UDP alma xətası: {Msg}", ex.Message);
            return null;
        }
    }

    private static ushort GetCommandId(byte[] payload)
    {
        if (payload.Length < 2) return 0;
        return BitConverter.ToUInt16(payload, 0);
    }

    private static ushort GetSessionId(byte[] payload)
    {
        if (payload.Length < 6) return 0;
        return BitConverter.ToUInt16(payload, 4);
    }

    #endregion

    private class AttendanceRecord
    {
        public int UserId { get; set; }
        public DateTime DateTime { get; set; }
        public int Status { get; set; }
    }
}
