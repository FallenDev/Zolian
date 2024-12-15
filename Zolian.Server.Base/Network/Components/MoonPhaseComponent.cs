﻿using System.Diagnostics;
using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;

using ServiceStack;

using SunCalcNet;

namespace Darkages.Network.Components;

public class MoonPhaseComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 900000;
    private double _dayStored = DateTime.Today.Day;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(5000);
                continue;
            }

            UpdateMoonPhase();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private void UpdateMoonPhase()
    {
        var moonCalc = MoonCalc.GetMoonIllumination(DateTime.UtcNow);
        _dayStored = moonCalc.Fraction;

        ServerSetup.Instance.MoonPhase = _dayStored switch
        {
            >= 0.00 and <= 0.039999 => "NewMoon",
            >= 0.04 and <= 0.959999 => "None",
            >= 0.96 and <= 1.00 => "FullMoon",
            _ => "None"
        };

        switch (ServerSetup.Instance.MoonPhase)
        {
            case "NewMoon":
                SetVampireNpc();
                SetCarn();
                break;
            case "FullMoon":
                SetLycanNpc();
                SetSleepingCarn();
                break;
            case "None":
                ClearNpcs();
                SetCarn();
                break;
        }
    }

    private static void SetVampireNpc()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Nosferatu") continue;
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.CurrentMapId = 100;
                    npc.X = 9;
                    npc.Y = 3;
                    npc.Direction = 2;
                    break;
                case 1:
                    npc.CurrentMapId = 286;
                    npc.X = 16;
                    npc.Y = 1;
                    npc.Direction = 1;
                    break;
                case 2:
                    npc.CurrentMapId = 1505;
                    npc.X = 1;
                    npc.Y = 23;
                    npc.Direction = 2;
                    break;
            }

            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }
    }

    private static void SetLycanNpc()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Fenrir") continue;
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.CurrentMapId = 100;
                    npc.X = 9;
                    npc.Y = 3;
                    npc.Direction = 2;
                    break;
                case 1:
                    npc.CurrentMapId = 6719;
                    npc.X = 4;
                    npc.Y = 3;
                    npc.Direction = 1;
                    break;
                case 2:
                    npc.CurrentMapId = 3212;
                    npc.X = 29;
                    npc.Y = 12;
                    npc.Direction = 1;
                    break;
            }

            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }
    }

    private static void SetCarn()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;
            if (npc.CurrentMapId == 5031) continue;
            npc.CurrentMapId = 5031;
            npc.X = 30;
            npc.Y = 12;
            npc.Direction = 2;
            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;
            if (npc.CurrentMapId == 14759) continue;
            npc.CurrentMapId = 14759;
            npc.X = 3;
            npc.Y = 1;
            npc.Direction = 1;
            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }
    }

    private static void SetSleepingCarn()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;
            if (npc.CurrentMapId == 14759) continue;
            npc.CurrentMapId = 14759;
            npc.X = 4;
            npc.Y = 1;
            npc.Direction = 1;
            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;
            if (npc.CurrentMapId == 5031) continue;
            npc.CurrentMapId = 5031;
            npc.X = 30;
            npc.Y = 11;
            npc.Direction = 1;
            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }
    }

    private static void ClearNpcs()
    {
        var mundanes = ServerSetup.Instance.GlobalMundaneCache.ToConcurrentDictionary();
        foreach (var npc in mundanes.Values)
        {
            if (npc.Name is not ("Fenrir" or "Nosferatu")) continue;
            if (npc.Name is "Fenrir")
            {
                npc.X = 1;
                npc.Y = 2;
            }
            else
            {
                npc.X = 1;
                npc.Y = 1;
            }

            npc.CurrentMapId = 14759;
            npc.Direction = 1;

            ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
            npc.UpdateAddAndRemove();
            ObjectManager.AddObject(npc);
        }
    }
}