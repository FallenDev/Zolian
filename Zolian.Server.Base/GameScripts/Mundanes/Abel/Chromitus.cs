using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using ServiceStack;

namespace Darkages.GameScripts.Mundanes.Abel;

[Script("Rifting Warden")]
public class Chromitus : MundaneScript
{
    public Chromitus(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Level + client.Aisling.AbpLevel >= 500)
            options.Add(new Dialog.OptionsDataItem(0x01, "Enter Rift"));

        options.Add(new Dialog.OptionsDataItem(0x00, "Not now"));


        client.SendOptionsDialog(Mundane, "You found your way here.. If you're strong enough, I'll allow you entry to complete the trials.\nAre you ready?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        var rand = Random.Shared.Next(0, 1);
        var randMap = Random.Shared.Next(801, 810);
        var map = ServerSetup.Instance.GlobalMapCache.GetValueOrDefault(randMap);
        if (map == null) return;

        switch (responseID)
        {
            case 0x00:
                client.CloseDialog();
                break;
            case 0x01:
                {
                    var damageables = GetObjects<Damageable>(map, p => p != null && p.Map.ID == randMap);

                    if (damageables.IsNullOrEmpty())
                        PopulateMapWithAppropriateMonsters(client, map);

                    if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                    {
                        PortGroup(client, map, rand);
                        return;
                    }

                    client.TransitionToMap(map, rand == 0 ? new Position(4, 4) : new Position(122, 117));
                }
                break;
        }
    }

    private void PopulateMapWithAppropriateMonsters(WorldClient client, Area area)
    {
        var sprites = new List<int>
        {
            Random.Shared.Next(1, 10),
            Random.Shared.Next(13, 18),
            Random.Shared.Next(46, 55),
            68, 69,
            Random.Shared.Next(85, 92),
            Random.Shared.Next(97, 105),
            Random.Shared.Next(136, 140),
            Random.Shared.Next(148, 153),
            156, 160,
            Random.Shared.Next(164, 172),
            Random.Shared.Next(175, 190),
            Random.Shared.Next(196, 211),
            237, 240, 242, 243,
            Random.Shared.Next(254, 266),
            Random.Shared.Next(268, 282),
            Random.Shared.Next(305, 314),
            Random.Shared.Next(319, 325),
            Random.Shared.Next(329, 345),
            388, 389, 390, 391, 392, 412, 413,
            414, 415, 416,
            Random.Shared.Next(418, 424),
            Random.Shared.Next(451, 455)
        };

        var monsterTemplates = new List<MonsterTemplate>();

        for (var i = 0; i < 10; i++)
        {
            var temp = new MonsterTemplate
            {
                ScriptName = "RiftMob",
                BaseName = "Rift Mob",
                Name = $"{Random.Shared.NextInt64()}RiftMob",
                AreaID = area.ID,
                Image = (ushort)sprites.RandomIEnum(),
                ElementType = ElementQualifer.Random,
                PathQualifer = PathQualifer.Wander,
                SpawnType = SpawnQualifer.Random,
                SpawnSize = Random.Shared.Next(10, 20),
                MoodType = MoodQualifer.Aggressive,
                MonsterType = MonsterType.Rift,
                MonsterArmorType = Enum.GetValues<MonsterArmorType>().RandomIEnum(),
                MonsterRace = Enum.GetValues<MonsterRace>().RandomIEnum(),
                IgnoreCollision = false,
                Waypoints = [],
                MovementSpeed = 1000,
                EngagedWalkingSpeed = Random.Shared.Next(800, 1400),
                AttackSpeed = Random.Shared.Next(500, 1000),
                CastSpeed = Random.Shared.Next(3000, 6000),
                LootType = LootQualifer.RandomGold,
                Level = (ushort)(client.Aisling.ExpLevel + client.Aisling.AbpLevel + Random.Shared.Next(1, 15)),
                SkillScripts = [],
                AbilityScripts = [],
                SpellScripts = []
            };

            if (temp.MonsterRace.MonsterRaceIsSet(MonsterRace.HigherBeing))
                temp.IgnoreCollision = true;

            monsterTemplates.Add(temp);
        }

        foreach (var template in monsterTemplates)
        {
            for (var i = 0; i < template.SpawnSize; i++)
            {
                var monster = Monster.Create(template, area);
                if (monster == null) continue;
                AddObject(monster);
            }
        }
    }

    private static void PortGroup(WorldClient client, Area map, int rand)
    {
        foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 188))
        {
            player.Client.TransitionToMap(map, rand == 0 ? new Position(4, 4) : new Position(122, 117));
        }
    }
}