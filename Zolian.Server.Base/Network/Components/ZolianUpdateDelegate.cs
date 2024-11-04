using Darkages.Network.Server;
using Microsoft.Extensions.Logging;

namespace Darkages.Network.Components;

public static class ZolianUpdateDelegate
{
    public static void Update(Action operation)
    {
        if (operation == null) return;

        try
        {
            operation();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"{operation.Method.Name}: {ex.Message}", LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }
    }
}