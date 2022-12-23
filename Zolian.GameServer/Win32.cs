using System.Runtime.InteropServices;

namespace Zolian.GameServer;

internal static class Win32
{
    [DllImport("Kernel32")]
    public static extern void AllocConsole();
}
