using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

namespace Darkages.Network;

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
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }
}