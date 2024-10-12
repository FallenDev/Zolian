using ComponentAce.Compression.Libs.zlib;
using Darkages.Network.Server;

namespace Darkages.Compression;

public static class CompressionProvider
{
    public static byte[] Deflate(ReadOnlySpan<byte> buffer)
    {
        var ret = new MemoryStream();
        using var compressed = new MemoryStream();
        using var compressor = new ZOutputStream(compressed, zlibConst.Z_DEFAULT_COMPRESSION);

        try
        {
            compressor.Write(buffer);
            compressor.finish();

            compressed.Position = 0;
            compressed.CopyTo(ret);
            ret.Position = 0;

            return ret.ToArray();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);
            return null;
        }
    }

    public static byte[] Inflate(byte[] buffer)
    {
        var ret = new MemoryStream();
        using var outData = new MemoryStream();
        using var decompressor = new ZOutputStream(outData);

        try
        {
            decompressor.Write(buffer);
            decompressor.finish();

            outData.Position = 0;
            outData.CopyTo(ret);
            ret.Position = 0;

            return ret.ToArray();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);
            return null;
        }
    }
}