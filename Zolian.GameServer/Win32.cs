using System.Runtime.InteropServices;

namespace Zolian.WorldServer;

internal static class Win32
{
    [DllImport("Kernel32")]
    public static extern void AllocConsole();
}
