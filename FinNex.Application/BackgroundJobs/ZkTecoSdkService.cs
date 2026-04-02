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
/// </summary>
public class ZkTecoSdkService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZkTecoSdkService> _logger;

    // Cihaz ayarları — gələcəkdə appsettings-dən oxuna bilər
    private const string DeviceIp = "192.168.0.95";
    private const int DevicePort = 4370;
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    // Cihaz statusu — ADMSController kimi static paylaşılır
    public static DateTime? SonElaqa { get; private set; }
    public static bool IsOnline { get; private set; }
    public static string? DeviceSN { get; private set; }

    // ZKTeco protocol constants
    private const int CMD_CONNECT = 1000;
    private const int CMD_EXIT = 1001;
    private const int CMD_ATTLOG_RRQ = 13;
    private const int CMD_CLEAR_ATTLOG = 14;
    private const int CMD_ACK_OK = 2000;
    private const int CMD_ACK_DATA = 2002;
    private const int CMD_PREPARE_DATA = 1500;
    private const int CMD_DATA = 1501;

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
        _logger.LogInformation("ZkTecoSdkService başladı. Cihaz: {Ip}:{Port}", DeviceIp, DevicePort);

        // İlk başlamada bir az gözlə ki app tam yüklənsin
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
            _logger.LogWarning("Cihaza qoşulmaq mümkün olmadı: {Msg}", ex.Message);
            return;
        }

        using var stream = client.GetStream();
        _replyNumber = 0;
        _sessionId = 0;

        // 1. Connect
        if (!await SendCommandAsync(stream, CMD_CONNECT, Array.Empty<byte>()))
        {
            _logger.LogWarning("Cihaza CMD_CONNECT göndərilə bilmədi.");
            IsOnline = false;
            return;
        }

        var connectReply = await ReceiveAsync(stream);
        if (connectReply == null || GetCommandId(connectReply) != CMD_ACK_OK)
        {
            _logger.LogWarning("Cihaz CMD_CONNECT-ə cavab vermədi.");
            IsOnline = false;
            return;
        }

        _sessionId = GetSessionId(connectReply);
        IsOnline = true;
        SonElaqa = DateTime.Now;
        _logger.LogInformation("Cihaza qoşuldu. SessionId: {SessionId}", _sessionId);

        // 2. Get attendance logs
        var attendanceLogs = await GetAttendanceLogsAsync(stream);

        if (attendanceLogs.Count > 0)
        {
            _logger.LogInformation("{Count} davamiyyət qeydi tapıldı.", attendanceLogs.Count);
            await SaveAttendanceAsync(attendanceLogs);

            // 3. Clear logs from device after saving
            await SendCommandAsync(stream, CMD_CLEAR_ATTLOG, Array.Empty<byte>());
            var clearReply = await ReceiveAsync(stream);
            if (clearReply != null && GetCommandId(clearReply) == CMD_ACK_OK)
            {
                _logger.LogInformation("Cihaz logları təmizləndi.");
            }
        }
        else
        {
            _logger.LogDebug("Yeni davamiyyət qeydi yoxdur.");
        }

        // 4. Disconnect
        await SendCommandAsync(stream, CMD_EXIT, Array.Empty<byte>());

        SonElaqa = DateTime.Now;
    }

    private async Task<List<AttendanceRecord>> GetAttendanceLogsAsync(NetworkStream stream)
    {
        var records = new List<AttendanceRecord>();

        if (!await SendCommandAsync(stream, CMD_ATTLOG_RRQ, Array.Empty<byte>()))
            return records;

        var reply = await ReceiveAsync(stream);
        if (reply == null) return records;

        var cmdId = GetCommandId(reply);

        if (cmdId == CMD_PREPARE_DATA)
        {
            // Böyük data — hissə-hissə gələcək
            int totalSize = BitConverter.ToInt32(reply, 8 + 1);
            var allData = new List<byte>();

            while (allData.Count < totalSize)
            {
                var dataPacket = await ReceiveAsync(stream);
                if (dataPacket == null) break;

                var packetCmd = GetCommandId(dataPacket);
                if (packetCmd == CMD_DATA)
                {
                    // Data header-dən sonra actual data
                    if (dataPacket.Length > 8)
                    {
                        allData.AddRange(dataPacket.Skip(8));
                    }
                }
                else if (packetCmd == CMD_ACK_OK)
                {
                    break;
                }
            }

            records = ParseAttendanceLogs(allData.ToArray());
        }
        else if (cmdId == CMD_ACK_DATA)
        {
            // Kiçik data — tək paketdə gəldi
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
        const int recordSize = 40; // Standard ZKTeco attendance record size

        for (int i = 0; i + recordSize <= data.Length; i += recordSize)
        {
            try
            {
                int userId = BitConverter.ToUInt16(data, i);

                // Timestamp: offset 24, 4 bytes (seconds since 2000-01-01)
                uint timestamp = BitConverter.ToUInt32(data, i + 24);
                var dateTime = new DateTime(2000, 1, 1).AddSeconds(timestamp);

                // Verify date is reasonable
                if (dateTime.Year < 2020 || dateTime.Year > 2030) continue;

                // Status: offset 28
                int status = data[i + 28];

                records.Add(new AttendanceRecord
                {
                    UserId = userId,
                    DateTime = dateTime,
                    Status = status
                });
            }
            catch
            {
                continue;
            }
        }

        // Fallback: try parsing as text-based format
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

        return records;
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

    #region ZKTeco TCP Protocol Helpers

    private async Task<bool> SendCommandAsync(NetworkStream stream, int command, byte[] data)
    {
        try
        {
            var packet = BuildPacket(command, data);
            await stream.WriteAsync(packet);
            _replyNumber++;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paket göndərmə xətası: CMD={Cmd}", command);
            return false;
        }
    }

    private byte[] BuildPacket(int command, byte[] data)
    {
        int payloadSize = 8 + data.Length;
        var payload = new byte[payloadSize];

        // Command ID (2 bytes, little-endian)
        payload[0] = (byte)(command & 0xFF);
        payload[1] = (byte)((command >> 8) & 0xFF);

        // Checksum placeholder (2 bytes) — will be filled after
        payload[2] = 0;
        payload[3] = 0;

        // Session ID (2 bytes)
        payload[4] = (byte)(_sessionId & 0xFF);
        payload[5] = (byte)((_sessionId >> 8) & 0xFF);

        // Reply Number (2 bytes)
        payload[6] = (byte)(_replyNumber & 0xFF);
        payload[7] = (byte)((_replyNumber >> 8) & 0xFF);

        // Data
        if (data.Length > 0)
            Array.Copy(data, 0, payload, 8, data.Length);

        // Calculate checksum
        ushort checksum = CalculateChecksum(payload);
        payload[2] = (byte)(checksum & 0xFF);
        payload[3] = (byte)((checksum >> 8) & 0xFF);

        // Build full packet with header
        var packet = new byte[8 + payloadSize];

        // Start marker: 0x5050827D
        packet[0] = 0x50;
        packet[1] = 0x50;
        packet[2] = 0x82;
        packet[3] = 0x7D;

        // Payload size (4 bytes, little-endian)
        var sizeBytes = BitConverter.GetBytes(payloadSize);
        Array.Copy(sizeBytes, 0, packet, 4, 4);

        // Payload
        Array.Copy(payload, 0, packet, 8, payloadSize);

        return packet;
    }

    private static ushort CalculateChecksum(byte[] payload)
    {
        // Reset checksum bytes to 0 for calculation
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

    private async Task<byte[]?> ReceiveAsync(NetworkStream stream)
    {
        try
        {
            var header = new byte[8];
            int headerRead = 0;

            while (headerRead < 8)
            {
                int read = await stream.ReadAsync(header.AsMemory(headerRead, 8 - headerRead));
                if (read == 0) return null;
                headerRead += read;
            }

            // Verify start marker
            if (header[0] != 0x50 || header[1] != 0x50 || header[2] != 0x82 || header[3] != 0x7D)
            {
                _logger.LogWarning("Yanlış paket başlığı");
                return null;
            }

            int payloadSize = BitConverter.ToInt32(header, 4);
            if (payloadSize <= 0 || payloadSize > 1024 * 1024) return null;

            var payload = new byte[payloadSize];
            int totalRead = 0;

            while (totalRead < payloadSize)
            {
                int read = await stream.ReadAsync(payload.AsMemory(totalRead, payloadSize - totalRead));
                if (read == 0) return null;
                totalRead += read;
            }

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Paket alma xətası: {Msg}", ex.Message);
            return null;
        }
    }

    private static ushort GetCommandId(byte[] payload)
    {
        return BitConverter.ToUInt16(payload, 0);
    }

    private static ushort GetSessionId(byte[] payload)
    {
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
