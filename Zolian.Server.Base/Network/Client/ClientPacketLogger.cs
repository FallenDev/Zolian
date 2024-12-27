using System.Collections.Concurrent;
using System.Net;

namespace Darkages.Network.Client;

public class ClientPacketLogger
{
    private const int MaxLogSize = 15;
    private ConcurrentDictionary<IPAddress, Queue<string>> _packetLog = [];
    private readonly Lock _logLock = new();

    public void LogPacket(IPAddress ip, string packetDetails)
    {
        lock (_logLock)
        {
            _packetLog.TryGetValue(ip, out var packetLog);
            if (packetLog is null) return;
            if (packetLog.Count >= MaxLogSize)
                packetLog.Dequeue();  // Remove the oldest log

            packetLog.Enqueue(packetDetails);
        }
    }

    public IEnumerable<string> GetRecentLogs(IPAddress ip)
    {
        lock (_logLock)
        {
            _packetLog.TryGetValue(ip, out var packetLog);
            return packetLog;
        }
    }
}