using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using ServiceStack;

using SunCalcNet;

namespace Darkages.Network.Components;

public class MoonPhaseComponent(WorldServer server) : WorldServerComponent(server)
{
    private double _dayStored = DateTime.Today.Day;

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdateMoonPhase);
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
            ObjectService.RemoveGameObject(npc);
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.CurrentMapId = 100;
                    npc.X = 9;
                    npc.Y = 3;
                    npc.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
                case 1:
                    npc.CurrentMapId = 286;
                    npc.X = 16;
                    npc.Y = 1;
                    npc.Direction = 1;
                    npc.UpdateAddAndRemove();
                    break;
                case 2:
                    npc.CurrentMapId = 1505;
                    npc.X = 1;
                    npc.Y = 23;
                    npc.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
            }

            ObjectService.AddGameObject(npc);
        }
    }

    private static void SetLycanNpc()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Fenrir") continue;
            ObjectService.RemoveGameObject(npc);
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.CurrentMapId = 100;
                    npc.X = 9;
                    npc.Y = 3;
                    npc.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
                case 1:
                    npc.CurrentMapId = 6719;
                    npc.X = 4;
                    npc.Y = 3;
                    npc.Direction = 1;
                    npc.UpdateAddAndRemove();
                    break;
                case 2:
                    npc.CurrentMapId = 3212;
                    npc.X = 29;
                    npc.Y = 12;
                    npc.Direction = 1;
                    npc.UpdateAddAndRemove();
                    break;
            }

            ObjectService.AddGameObject(npc);
        }
    }

    private static void SetCarn()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;
            ObjectService.RemoveGameObject(npc);
            npc.CurrentMapId = 5031;
            npc.X = 30;
            npc.Y = 12;
            npc.Direction = 2;
            npc.UpdateAddAndRemove();
            ObjectService.AddGameObject(npc);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;
            ObjectService.RemoveGameObject(npc);
            npc.CurrentMapId = 14759;
            npc.X = 3;
            npc.Y = 1;
            npc.Direction = 1;
            npc.UpdateAddAndRemove();
            ObjectService.AddGameObject(npc);
        }
    }

    private static void SetSleepingCarn()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;
            ObjectService.RemoveGameObject(npc);
            npc.CurrentMapId = 14759;
            npc.X = 4;
            npc.Y = 1;
            npc.Direction = 1;
            npc.UpdateAddAndRemove();
            ObjectService.AddGameObject(npc);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;
            ObjectService.RemoveGameObject(npc);
            npc.CurrentMapId = 5031;
            npc.X = 30;
            npc.Y = 11;
            npc.Direction = 1;
            npc.UpdateAddAndRemove();
            ObjectService.AddGameObject(npc);
        }
    }

    private static void ClearNpcs()
    {
        var mundanes = ServerSetup.Instance.GlobalMundaneCache.ToConcurrentDictionary();
        foreach (var npc in mundanes.Values)
        {
            if (npc.Name is not ("Fenrir" or "Nosferatu")) continue;
            ObjectService.RemoveGameObject(npc);
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
            npc.UpdateAddAndRemove();
            ObjectService.AddGameObject(npc);
        }
    }
}