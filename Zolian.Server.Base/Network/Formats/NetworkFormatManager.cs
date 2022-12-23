using System.Globalization;

using Microsoft.Extensions.Logging;

namespace Darkages.Network.Formats
{
    public static class NetworkFormatManager
    {
        static NetworkFormatManager()
        {
            ClientFormats = new Type[256];

            for (var i = 0; i < 256; i++)
            {
                ClientFormats[i] = Type.GetType(string.Format(CultureInfo.CurrentCulture, "Darkages.Network.Formats.Models.ClientFormats.ClientFormat{0:X2}", i), false, false);
            }
        }

        private static Type[] ClientFormats { get; }

        public static NetworkFormat GetClientFormat(byte command)
        {
            try
            {
                return Activator.CreateInstance(ClientFormats[command]) as NetworkFormat;
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                throw;
            }
        }
    }
}