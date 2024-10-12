using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Types;

public interface IEphermeral
{
    void UpdateSpawns(TimeSpan elapsedTime);
    void Spawn(string creatureName, string script, double lifespan = 120, double updateRate = 650, int count = 1);
}

public abstract class Summon(WorldClient client) : ObjectManager, IEphermeral
{
    private WorldServerTimer ObjectsUpdateTimer { get; set; }
    private WorldServerTimer ObjectsRemovedTimer { get; set; }

    public List<(string, Monster)> Spawns = [];

    private KeyValuePair<string, MonsterTemplate> Template { get; set; }
    private string Script { get; set; }

    public void Spawn(string creatureName, string script, double lifespan = 120, double updateRate = 650, int count = 1)
    {
        ObjectsRemovedTimer = new WorldServerTimer(TimeSpan.FromSeconds(lifespan));
        ObjectsUpdateTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(updateRate));

        Template = ServerSetup.Instance.GlobalMonsterTemplateCache.FirstOrDefault(i => i.Value.BaseName == creatureName);
        Script = script;

        CreateLocal(count);
    }

    private void CreateLocal(int count)
    {
        if (Template.Value != null)
            Create(Template.Value, Script, count);
    }

    public void DeSpawn()
    {
        lock (Spawns)
        {
            foreach (var (_, spawn) in Spawns)
            {
                spawn.Remove();
            }

            Spawns.Clear();
        }
    }

    public void Update(TimeSpan elapsedTime)
    {
        if (ObjectsRemovedTimer != null && ObjectsRemovedTimer.Update(elapsedTime))
        {
            DeSpawn();
        }

        if (ObjectsUpdateTimer != null && ObjectsUpdateTimer.Update(elapsedTime))
        {
            UpdateSpawns(elapsedTime);
        }

    }

    private void Create(Template template, string script, int count = 1)
    {
        if (client == null)
            return;

        switch (template)
        {
            case MonsterTemplate monsterTemplate:
                {
                    for (var i = 0; i < count; i++)
                    {
                        //Share similar attributes as the summoner.
                        monsterTemplate.Level = (ushort)(client.Aisling.ExpLevel + 3);
                        monsterTemplate.LootType = LootQualifer.None;
                        monsterTemplate.DefenseElement = client.Aisling.DefenseElement;
                        monsterTemplate.OffenseElement = client.Aisling.OffenseElement;
                        monsterTemplate.SkillScripts =
                        [
                            ..client.Aisling.SkillBook.Skills.Where(n => n.Value != null)
                                .Select(n => n.Value.Template.ScriptName).ToList()
                        ];

                        var monster = Monster.Create(monsterTemplate, client.Aisling.Map);

                        monster.SummonerId = client.Aisling.Serial;
                        monster.X = client.Aisling.LastPosition.X;
                        monster.Y = client.Aisling.LastPosition.Y;
                        monster.CurrentMapId = client.Aisling.CurrentMapId;

                        lock (Spawns)
                        {
                            Spawns.Add((client.Aisling.Username, monster));
                        }

                        AddObject(monster);
                    }

                    break;
                }
        }
    }

    public abstract void UpdateSpawns(TimeSpan elapsedTime);
}