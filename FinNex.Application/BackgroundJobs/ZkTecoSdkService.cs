using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace FinNex.Application.BackgroundJobs;

/// <summary>
/// ZKTeco/Datalab cihazına SDK (TCP port 4370) ilə qoşulub
/// davamiyyət məlumatlarını çəkən background servis.
/// Əvvəlcə raw formatda cavabı oxuyub debug edir.
/// </summary>
public class ZkTecoSdkService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkTecoSdkService> _logger;

    private const string DeviceIp = "192.168.0.95";
    private const int DevicePort = 4370;
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public static DateTime? SonElaqa { get; private set; }
    public static bool IsOnline { get; private set; }
    public static string? DeviceSN { get; private set; }

    private const ushort CMD_CONNECT = 1000;
    private const ushort CMD_EXIT = 1001;
    private const ushort CMD_ATTLOG_RRQ = 13;
    private const ushort CMD_CLEAR_ATTLOG = 14;
    private const ushort CMD_ACK_OK = 2000;
    private const ushort CMD_ACK_DATA = 2002;
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
        _logger.LogInformation("ZkTecoSdkService başladı. Cihaz: {Ip}:{Port} (TCP raw)", DeviceIp, DevicePort);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PullAttendanceAsync();
            }
            catch (Exception ex)
            {
                IsOnline = false;
                _logger.LogError(ex, "ZkTecoSdkService xətası.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PullAttendanceAsync()
    {
        using var client = new TcpClient();
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        try
        {
            await client.ConnectAsync(DeviceIp, DevicePort);
        }
        catch (Exception ex)
        {
            IsOnline = false;
            _logger.LogWarning("TCP qoşulmaq mümkün olmadı: {Msg}", ex.Message);
            return;
        }

        using var stream = client.GetStream();
        _replyNumber = 0;
        _sessionId = 0;

        // 1. CMD_CONNECT göndər — əvvəlcə raw payload (header olmadan)
        var connectPacket = BuildPayload(CMD_CONNECT, Array.Empty<byte>());
        await stream.WriteAsync(connectPacket);
        _replyNumber++;

        // Raw cavabı oxu
        var rawReply = await ReadRawAsync(stream);
        if (rawReply == null || rawReply.Length == 0)
        {
            _logger.LogWarning("Cihaz heç bir cavab vermədi (0 bayt).");
            IsOnline = false;
            return;
        }

        // Debug: ilk 20 baytı hex olaraq logla
        var hexStr = BitConverter.ToString(rawReply, 0, Math.Min(rawReply.Length, 20));
        _logger.LogInformation("Cihaz cavabı: {Len} bayt, hex: {Hex}", rawReply.Length, hexStr);

        // Cavabı analiz et - TCP header var/yox?
        byte[] payload;
        if (rawReply.Length > 8 && rawReply[0] == 0x50 && rawReply[1] == 0x50 &&
            rawReply[2] == 0x82 && rawReply[3] == 0x7D)
        {
            // TCP framing var — payload header-dən sonra
            int payloadLen = BitConverter.ToInt32(rawReply, 4);
            payload = new byte[payloadLen];
            Array.Copy(rawReply, 8, payload, 0, Math.Min(payloadLen, rawReply.Length - 8));
            _logger.LogInformation("TCP frame tapıldı. Payload: {Len} bayt", payloadLen);
        }
        else
        {
            // Raw payload — header yoxdur
            payload = rawReply;
            _logger.LogInformation("Raw payload (header yoxdur). Uzunluq: {Len}", payload.Length);
        }

        if (payload.Length < 4)
        {
            _logger.LogWarning("Payload çox qısadır: {Len} bayt", payload.Length);
            IsOnline = false;
            return;
        }

        var replyCmd = BitConverter.ToUInt16(payload, 0);
        _logger.LogInformation("Cavab CMD: {Cmd} (gözlənilən CMD_ACK_OK={Expected})", replyCmd, CMD_ACK_OK);

        if (replyCmd != CMD_ACK_OK)
        {
            _logger.LogWarning("CMD_CONNECT rədd edildi. Cavab: {Cmd}", replyCmd);
            IsOnline = false;
            return;
        }

        _sessionId = BitConverter.ToUInt16(payload, 4);
        IsOnline = true;
        SonElaqa = DateTime.Now;
        _logger.LogInformation("Cihaza qoşuldu! SessionId: {SessionId}", _sessionId);

        // 2. Attendance logları çək
        var attendanceLogs = await GetAttendanceLogsAsync(stream);

        if (attendanceLogs.Count > 0)
        {
            _logger.LogInformation("{Count} davamiyyət qeydi tapıldı.", attendanceLogs.Count);
            await SaveAttendanceAsync(attendanceLogs);

            // 3. Logları təmizlə
            await SendCommandAsync(stream, CMD_CLEAR_ATTLOG, Array.Empty<byte>());
            var clearReply = await ReadPayloadAsync(stream);
            if (clearReply != null && clearReply.Length >= 2 && BitConverter.ToUInt16(clearReply, 0) == CMD_ACK_OK)
            {
                _logger.LogInformation("Cihaz logları təmizləndi.");
            }
        }
        else
        {
            _logger.LogInformation("Yeni davamiyyət qeydi yoxdur.");
        }

        // 4. Disconnect
        await SendCommandAsync(stream, CMD_EXIT, Array.Empty<byte>());
        SonElaqa = DateTime.Now;
    }

    private async Task<List<AttendanceRecord>> GetAttendanceLogsAsync(NetworkStream stream)
    {
        var records = new List<AttendanceRecord>();

        await SendCommandAsync(stream, CMD_ATTLOG_RRQ, Array.Empty<byte>());
        var reply = await ReadPayloadAsync(stream);
        if (reply == null || reply.Length < 2)
        {
            _logger.LogWarning("ATTLOG cavab gəlmədi.");
            return records;
        }

        var cmdId = BitConverter.ToUInt16(reply, 0);
        _logger.LogInformation("ATTLOG cavab CMD: {Cmd}, Len: {Len}", cmdId, reply.Length);

        if (cmdId == CMD_PREPARE_DATA)
        {
            int totalSize = reply.Length >= 12 ? BitConverter.ToInt32(reply, 8) : 0;
            _logger.LogInformation("Böyük data: {Size} bayt gözlənilir", totalSize);

            var allData = new List<byte>();
            while (allData.Count < totalSize)
            {
                var chunk = await ReadRawAsync(stream);
                if (chunk == null || chunk.Length == 0) break;
                allData.AddRange(chunk);
            }

            // Free data
            await SendCommandAsync(stream, CMD_FREE_DATA, Array.Empty<byte>());
            await ReadPayloadAsync(stream);

            records = ParseAttendanceLogs(allData.ToArray());
        }
        else if (cmdId == CMD_ACK_DATA)
        {
            if (reply.Length > 8)
                records = ParseAttendanceLogs(reply.Skip(8).ToArray());
        }
        else if (cmdId == CMD_ACK_OK)
        {
            _logger.LogInformation("Cihazda heç bir davamiyyət qeydi yoxdur.");
        }

        return records;
    }

    private List<AttendanceRecord> ParseAttendanceLogs(byte[] data)
    {
        var records = new List<AttendanceRecord>();
        _logger.LogInformation("Parse ediləcək data: {Len} bayt", data.Length);

        // Binary format
        const int recordSize = 40;
        if (data.Length >= recordSize)
        {
            for (int i = 0; i + recordSize <= data.Length; i += recordSize)
            {
                try
                {
                    int userId = BitConverter.ToUInt16(data, i);
                    uint encoded = BitConverter.ToUInt32(data, i + 24);
                    var dateTime = DecodeZkTime(encoded);

                    if (dateTime.Year < 2020 || dateTime.Year > 2030) continue;

                    int status = data[i + 28];
                    records.Add(new AttendanceRecord { UserId = userId, DateTime = dateTime, Status = status });
                }
                catch { continue; }
            }
        }

        // Text fallback
        if (records.Count == 0 && data.Length > 10)
        {
            try
            {
                var text = Encoding.UTF8.GetString(data);
                foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 3) continue;
                    if (!int.TryParse(parts[0].Trim(), out int userId)) continue;
                    if (!DateTime.TryParse(parts[1].Trim(), out DateTime dateTime)) continue;
                    int status = parts.Length > 2 && int.TryParse(parts[2].Trim(), out int s) ? s : 0;
                    records.Add(new AttendanceRecord { UserId = userId, DateTime = dateTime, Status = status });
                }
            }
            catch { }
        }

        _logger.LogInformation("Parse nəticəsi: {Count} qeyd", records.Count);
        return records;
    }

    private static DateTime DecodeZkTime(uint encoded)
    {
        int second = (int)(encoded % 60); encoded /= 60;
        int minute = (int)(encoded % 60); encoded /= 60;
        int hour = (int)(encoded % 24); encoded /= 24;
        int day = (int)(encoded % 31) + 1; encoded /= 31;
        int month = (int)(encoded % 12) + 1; encoded /= 12;
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
                    await db.Davamiyyetler.AddAsync(new Davamiyyet
                    {
                        IsciId = record.UserId,
                        Tarix = tarix,
                        GirisVaxti = girisdir ? record.DateTime : null,
                        CixisVaxti = girisdir ? null : record.DateTime,
                        Status = HesablaStatus(record.DateTime, girisdir)
                    });
                }
                else
                {
                    if (girisdir && (movcud.GirisVaxti == null || record.DateTime < movcud.GirisVaxti))
                    {
                        movcud.GirisVaxti = record.DateTime;
                        movcud.Status = HesablaStatus(record.DateTime, true);
                    }
                    else if (!girisdir && (movcud.CixisVaxti == null || record.DateTime > movcud.CixisVaxti))
                    {
                        movcud.CixisVaxti = record.DateTime;
                    }
                }

                await db.SaveChangesAsync();
                _logger.LogInformation("Davamiyyət: IsciId={Id}, {Nov}={Vaxt}",
                    record.UserId, girisdir ? "Giriş" : "Çıxış", record.DateTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save xətası: UserId={Id}", record.UserId);
            }
        }
    }

    private static DavamiyyetStatus HesablaStatus(DateTime girisVaxti, bool girisdir)
    {
        if (!girisdir) return DavamiyyetStatus.Isde;
        var isBaslamaVaxti = girisVaxti.Date.AddHours(9);
        return girisVaxti > isBaslamaVaxti.AddMinutes(5) ? DavamiyyetStatus.Gecikme : DavamiyyetStatus.Isde;
    }

    #region Protocol Helpers

    private async Task SendCommandAsync(NetworkStream stream, ushort command, byte[] data)
    {
        var packet = BuildPayload(command, data);
        await stream.WriteAsync(packet);
        _replyNumber++;
    }

    private byte[] BuildPayload(ushort command, byte[] data)
    {
        int size = 8 + data.Length;
        var buf = new byte[size];

        buf[0] = (byte)(command & 0xFF);
        buf[1] = (byte)((command >> 8) & 0xFF);
        buf[4] = (byte)(_sessionId & 0xFF);
        buf[5] = (byte)((_sessionId >> 8) & 0xFF);
        buf[6] = (byte)(_replyNumber & 0xFF);
        buf[7] = (byte)((_replyNumber >> 8) & 0xFF);

        if (data.Length > 0)
            Array.Copy(data, 0, buf, 8, data.Length);

        ushort chk = CalcChecksum(buf);
        buf[2] = (byte)(chk & 0xFF);
        buf[3] = (byte)((chk >> 8) & 0xFF);

        return buf;
    }

    private static ushort CalcChecksum(byte[] buf)
    {
        var tmp = (byte[])buf.Clone();
        tmp[2] = 0; tmp[3] = 0;
        uint sum = 0;
        int i = 0;
        while (i + 1 < tmp.Length) { sum += (uint)(tmp[i] | (tmp[i + 1] << 8)); i += 2; }
        if (i < tmp.Length) sum += tmp[i];
        sum = (sum >> 16) + (sum & 0xFFFF);
        sum = (sum >> 16) + (sum & 0xFFFF);
        return (ushort)(~sum & 0xFFFF);
    }

    /// <summary>
    /// Raw baytları oxu — hər nə gəlirsə (TCP header olsa da olmasa da)
    /// </summary>
    private async Task<byte[]?> ReadRawAsync(NetworkStream stream)
    {
        try
        {
            var buffer = new byte[4096];
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
            if (read == 0) return null;
            var result = new byte[read];
            Array.Copy(buffer, result, read);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("TCP oxuma timeout (5 san).");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("TCP oxuma xətası: {Msg}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Payload oxu — TCP header varsa kəs, yoxdursa raw qaytar
    /// </summary>
    private async Task<byte[]?> ReadPayloadAsync(NetworkStream stream)
    {
        var raw = await ReadRawAsync(stream);
        if (raw == null || raw.Length == 0) return null;

        if (raw.Length > 8 && raw[0] == 0x50 && raw[1] == 0x50 && raw[2] == 0x82 && raw[3] == 0x7D)
        {
            int len = BitConverter.ToInt32(raw, 4);
            var payload = new byte[Math.Min(len, raw.Length - 8)];
            Array.Copy(raw, 8, payload, 0, payload.Length);
            return payload;
        }

        return raw;
    }

    #endregion

    private class AttendanceRecord
    {
        public int UserId { get; set; }
        public DateTime DateTime { get; set; }
        public int Status { get; set; }
    }
}
