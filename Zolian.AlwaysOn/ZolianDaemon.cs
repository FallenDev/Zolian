using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Zolian.AlwaysOn;

internal class ZolianDaemon
{
    private static Process _serverProcess;

    public ZolianDaemon()
    {
        StartProcess();
        ObtainProcess();
        var WorldServerCheckThread = new Thread(CheckWorldServer) { IsBackground = false };
        WorldServerCheckThread.Start();
        Thread.CurrentThread.Join();
    }

    private static void WorldServerProcessExited(object sender, EventArgs e)
    {
        Console.Write("AlwaysOn: Restarting Server\n");
        StartProcess();
    }

    private static void CheckWorldServer()
    {
        do
        {
            try
            {
                Console.Write("-Server AlwaysOn Enabled-\n");
                _serverProcess!.EnableRaisingEvents = true;
                _serverProcess.Exited += WorldServerProcessExited;
                _serverProcess.WaitForExit();
            }
            catch
            {
                // ignored
            }
            finally
            {
                ObtainProcess();
                Thread.Sleep(1000);
            }
        } while (true);
    }

    private static void StartProcess()
    {
        var exePath = Directory.GetCurrentDirectory() + "\\Zolian.WorldServer.exe";
        if (!File.Exists(exePath)) return;
        var pr = new Process();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath
            };

            pr.StartInfo = psi;
        }
        catch
        {
            // ignored
        }
        finally
        {
            pr.Start();
            ObtainProcess();
        }
    }

    private static void ObtainProcess()
    {
        var serverProcesses = Process.GetProcessesByName("Zolian.WorldServer");
        if (serverProcesses.Length == 0)
        {
            Console.Write("-AlwaysOn Failed to obtain Process-\n");
            return;
        }

        _serverProcess = serverProcesses.First();
    }
}