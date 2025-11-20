using System.Diagnostics;
using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using SunCalcNet;

namespace Darkages.Network.Components;

public class MoonPhaseComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 900_000;
    private double _dayStored;

    protected internal override async Task Update()
    {
        var sw = Stopwatch.StartNew();
        var target = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            var elapsed = sw.Elapsed.TotalMilliseconds;

            if (elapsed < target)
            {
                var remaining = (int)(target - elapsed);
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 5000));
                continue;
            }

            UpdateMoonPhase();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private void UpdateMoonPhase()
    {
        var moon = MoonCalc.GetMoonIllumination(DateTime.UtcNow).Fraction;
        _dayStored = moon;

        ServerSetup.Instance.MoonPhase = moon switch
        {
            >= 0.00 and <= 0.04 => "NewMoon",
            >= 0.96 and <= 1.00 => "FullMoon",
            _ => "None"
        };

        switch (ServerSetup.Instance.MoonPhase)
        {
            case "NewMoon":
                MoveNosferatu();
                MoveCarnNormal();
                break;

            case "FullMoon":
                MoveFenrir();
                MoveCarnSleeping();
                break;

            default:
                ResetNightCreatures();
                MoveCarnNormal();
                break;
        }
    }

    private static void MoveNpc(Mundane npc, int map, int x, int y, byte dir)
    {
        npc.CurrentMapId = map;
        npc.X = x;
        npc.Y = y;
        npc.Direction = dir;

        ServerSetup.Instance.GlobalMundaneCache.TryUpdate(npc.Serial, npc, npc);
        npc.UpdateAddAndRemove();
        ObjectManager.AddObject(npc);
    }

    /// <summary>
    /// Provides the predefined spawn locations for Nosferatu entities during the New Moon event.
    /// </summary>
    /// <remarks>Each tuple in the array specifies the map identifier, X and Y coordinates, and facing
    /// direction for a Nosferatu spawn point. These locations are used to position Nosferatu entities at the start of
    /// the event.</remarks>
    private static readonly (int map, int x, int y, byte dir)[] NosferatuSpots =
    {
        (100, 9, 3, 2),
        (286, 16, 1, 1),
        (1505, 1, 23, 2)
    };

    private static void MoveNosferatu()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Nosferatu") continue;

            var loc = NosferatuSpots[Generator.RandNumGen3()];
            MoveNpc(npc, loc.map, loc.x, loc.y, loc.dir);
        }
    }

    /// <summary>
    /// Provides the predefined spawn locations for Fenrir during the Full Moon event.
    /// </summary>
    /// <remarks>Each tuple in the array specifies the map identifier, X and Y coordinates, and the initial
    /// direction for Fenrir's spawn. This array is intended for use in event logic that requires knowledge of Fenrir's
    /// possible spawn points.</remarks>
    private static readonly (int map, int x, int y, byte dir)[] FenrirSpots =
    {
        (100, 9, 3, 2),
        (6719, 4, 3, 1),
        (3212, 29, 12, 1)
    };

    private static void MoveFenrir()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Fenrir") continue;

            var loc = FenrirSpots[Generator.RandNumGen3()];
            MoveNpc(npc, loc.map, loc.x, loc.y, loc.dir);
        }
    }

    /// <summary>
    /// Moves all NPCs named "Carn" and "Sleeping Carn" to their designated locations in the game world.
    /// </summary>
    /// <remarks>This method is intended to update the positions of specific NPCs based on their current
    /// state. It should be called when synchronizing or resetting the locations of these NPCs. Only NPCs with the exact
    /// names "Carn" or "Sleeping Carn" are affected.</remarks>
    private static void MoveCarnNormal()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;

            MoveNpc(npc, 5031, 30, 12, 2);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;

            MoveNpc(npc, 14759, 3, 1, 1);
        }
    }

    /// <summary>
    /// Moves all NPCs named "Carn" and "Sleeping Carn" to their designated locations in the game world.
    /// </summary>
    /// <remarks>This method is intended to update the positions of specific NPCs based on their current
    /// state. It should be called when synchronizing or resetting the locations of these NPCs. Only NPCs with the exact
    /// names "Carn" or "Sleeping Carn" are affected.</remarks>
    private static void MoveCarnSleeping()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Carn") continue;

            MoveNpc(npc, 14759, 4, 1, 1);
        }

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name != "Sleeping Carn") continue;

            MoveNpc(npc, 5031, 30, 11, 1);
        }
    }

    /// <summary>
    /// Resets the positions of the Fenrir and Nosferatu NPCs to their default locations within the game world.
    /// </summary>
    /// <remarks>This method restores Fenrir and Nosferatu to their initial states by moving them to
    /// predefined coordinates. It should be called when a reset of these specific NPCs is required, such as during
    /// server initialization or game state restoration. This method does not affect other NPCs.</remarks>
    private static void ResetNightCreatures()
    {
        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            if (npc.Name is not ("Fenrir" or "Nosferatu")) continue;

            if (npc.Name == "Fenrir")
                MoveNpc(npc, 14759, 1, 2, 1);
            else
                MoveNpc(npc, 14759, 1, 1, 1);
        }
    }
}