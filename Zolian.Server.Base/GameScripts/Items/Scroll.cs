using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Items;

[Script("Scroll")]
public class Scroll(Item item) : ItemScript(item)
{
    public override void OnUse(Sprite sprite, byte slot)
    {
        if (sprite == null) return;
        if (Item?.Template == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (aisling.Path == Class.Peasant)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You are not allowed to use this.");
            return;
        }

        switch (client.Aisling.Flags)
        {
            case AislingFlags.Normal:
                {
                    switch (Item.Template.Name)
                    {
                        case "Abel Scroll":
                            client.TransitionToMap(3014, new Position(11, 11));
                            return;
                        case "Mileth Scroll":
                            client.TransitionToMap(500, new Position(53, 3));
                            return;
                        case "Tagor Scroll":
                            client.TransitionToMap(662, new Position(28, 81));
                            return;
                        case "Rucesion Scroll":
                            client.TransitionToMap(505, new Position(24, 39));
                            return;
                        case "Piet Scroll":
                            client.TransitionToMap(3020, new Position(15, 2));
                            return;
                        case "Rionnag Scroll":
                            client.TransitionToMap(3210, new Position(28, 18));
                            return;
                        case "Oren Scroll":
                            client.TransitionToMap(6228, new Position(57, 168));
                            return;
                        case "Undine Scroll":
                            client.TransitionToMap(3008, new Position(13, 15));
                            return;
                        case "Suomi Scroll":
                            client.TransitionToMap(3016, new Position(16, 2));
                            return;
                        case "Loures Scroll":
                            client.TransitionToMap(3000, new Position(35, 16));
                            return;
                        case "Cascade Falls Scroll":
                            client.TransitionToMap(1201, new Position(9, 8));
                            return;
                        case "Cthonic Guild Scroll":
                            {
                                foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                                {
                                    if (npc.Value.Scripts is null) continue;
                                    if (!npc.Value.Scripts.TryGetValue("Cthonic Portals", out var scriptObj)) continue;
                                    scriptObj.OnClick(client, npc.Value.Serial);
                                    break;
                                }
                            }
                            return;
                    }

                    return;
                }
            case AislingFlags.Ghost:
                return;
        }
    }

    public override void Equipped(Sprite sprite, byte displaySlot) { }

    public override void UnEquipped(Sprite sprite, byte displaySlot) { }
}