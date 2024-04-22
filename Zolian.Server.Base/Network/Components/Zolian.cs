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
            ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }
}