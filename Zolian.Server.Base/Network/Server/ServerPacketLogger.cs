namespace Darkages.Network.Server;

public class ServerPacketLogger
{
    private const int MaxLogSize = 15;
    private readonly Queue<string> _packetLog = [];
    private readonly Lock _logLock = new();

    public void LogPacket(string packetDetails)
    {
        lock (_logLock)
        {
            if (_packetLog.Count >= MaxLogSize)
                _packetLog.Dequeue();  // Remove the oldest log

            _packetLog.Enqueue(packetDetails);
        }
    }

    public IEnumerable<string> GetRecentLogs()
    {
        lock (_logLock)
        {
            return _packetLog;
        }
    }
}