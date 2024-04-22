using Elskom.Generic.Libs;

namespace Darkages.Compression;

public static class CompressionProvider
{
    public static byte[] Deflate(byte[] buffer)
    {
        var iStream = new MemoryStream(buffer);
        var oStream = new MemoryStream();
        var zStream = new ZOutputStream(oStream, (ZlibCompression)6);

        try
        {
            CopyStream(iStream, zStream);
            zStream.Finish();

            return oStream.ToArray();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);
            return null;
        }
        finally
        {
            zStream.Close();
            oStream.Close();
            iStream.Close();
        }
    }

    public static byte[] Inflate(byte[] buffer)
    {
        var iStream = new MemoryStream(buffer);
        var oStream = new MemoryStream();
        var zStream = new ZOutputStream(oStream);

        try
        {
            CopyStream(iStream, zStream);
            zStream.Finish();

            return oStream.ToArray();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);
            return null;
        }
        finally
        {
            zStream.Close();
            oStream.Close();
            iStream.Close();
        }
    }

    private static void CopyStream(Stream src, Stream dst)
    {
        var buffer = new byte[4096];
        int length;

        while ((length = src.Read(buffer, 0, buffer.Length)) > 0) dst.Write(buffer, 0, length);

        dst.Flush();
    }
}