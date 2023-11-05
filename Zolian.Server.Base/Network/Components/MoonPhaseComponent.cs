using Darkages.Common;
using Darkages.Network.Server;

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
                break;
            case "FullMoon":
                SetLycanNpc();
                break;
            case "None":
                ClearNpcs();
                break;
        }
    }

    private static void SetVampireNpc()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Template.Name != "Nosferatu") continue;
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 9;
                    npc.Template.X = 9;
                    npc.Y = 3;
                    npc.Template.Y = 3;
                    npc.Direction = 1;
                    npc.Template.Direction = 1;
                    npc.UpdateAddAndRemove();
                    break;
                case 1:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 16;
                    npc.Template.X = 16;
                    npc.Y = 1;
                    npc.Template.Y = 1;
                    npc.Direction = 2;
                    npc.Template.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
                case 2:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 20;
                    npc.Template.X = 20;
                    npc.Y = 1;
                    npc.Template.Y = 1;
                    npc.Direction = 2;
                    npc.Template.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
            }
        }
    }

    private static void SetLycanNpc()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Template.Name != "Fenrir") continue;
            var rand = Generator.RandNumGen3();
            switch (rand)
            {
                case 0:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 9;
                    npc.Template.X = 9;
                    npc.Y = 3;
                    npc.Template.Y = 3;
                    npc.Direction = 1;
                    npc.Template.Direction = 1;
                    npc.UpdateAddAndRemove();
                    break;
                case 1:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 16;
                    npc.Template.X = 16;
                    npc.Y = 1;
                    npc.Template.Y = 1;
                    npc.Direction = 2;
                    npc.Template.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
                case 2:
                    npc.Map.ID = 100;
                    npc.Template.AreaID = 100;
                    npc.X = 20;
                    npc.Template.X = 20;
                    npc.Y = 1;
                    npc.Template.Y = 1;
                    npc.Direction = 2;
                    npc.Template.Direction = 2;
                    npc.UpdateAddAndRemove();
                    break;
            }
        }
    }

    private static void ClearNpcs()
    {
        var mundanes = ServerSetup.Instance.GlobalMundaneCache.ToConcurrentDictionary();
        foreach (var npc in mundanes.Values)
        {
            if (npc.Template.Name is not ("Fenrir" or "Nosferatu")) continue;
            if (npc.Template.Name is "Fenrir")
            {
                npc.X = 1;
                npc.Template.X = 1;
                npc.Y = 2;
                npc.Template.Y = 2;
            }
            else
            {
                npc.X = 1;
                npc.Template.X = 1;
                npc.Y = 1;
                npc.Template.Y = 1;
            }

            npc.Map.ID = 14759;
            npc.Template.AreaID = 14759;
            npc.Direction = 1;
            npc.Template.Direction = 1;
            npc.UpdateAddAndRemove();
        }
    }
}