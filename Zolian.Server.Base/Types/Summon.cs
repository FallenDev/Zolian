using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Types;

public interface IEphermeral
{
    void UpdateSpawns(TimeSpan elapsedTime);
    void Spawn(string creatureName, string script, double lifespan = 120, double updateRate = 650, int count = 1);
}

public abstract class Summon : ObjectManager, IEphermeral
{
    private readonly WorldClient _client;

    private WorldServerTimer ObjectsUpdateTimer { get; set; }
    private WorldServerTimer ObjectsRemovedTimer { get; set; }

    public List<(string, Monster)> Spawns = new();

    private KeyValuePair<string, MonsterTemplate> Template { get; set; }
    private string Script { get; set; }

    protected Summon(WorldClient client)
    {
        _client = client;
    }

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
        if (_client == null)
            return;

        switch (template)
        {
            case MonsterTemplate monsterTemplate:
            {
                for (var i = 0; i < count; i++)
                {
                    //Share similar attributes as the summoner.
                    monsterTemplate.Level = (ushort)(_client.Aisling.ExpLevel + 3);
                    monsterTemplate.LootType = LootQualifer.None;
                    monsterTemplate.DefenseElement = _client.Aisling.DefenseElement;
                    monsterTemplate.OffenseElement = _client.Aisling.OffenseElement;
                    monsterTemplate.SkillScripts = new List<string>(_client.Aisling.SkillBook.Skills.Where(n => n.Value != null).Select(n => n.Value.Template.ScriptName).ToList());

                    var monster = Monster.Create(monsterTemplate, _client.Aisling.Map);

                    monster.SummonerId = _client.Aisling.Serial;
                    monster.X = _client.Aisling.LastPosition.X;
                    monster.Y = _client.Aisling.LastPosition.Y;
                    monster.CurrentMapId = _client.Aisling.CurrentMapId;

                    lock (Spawns)
                    {
                        Spawns.Add((_client.Aisling.Username, monster));
                    }

                    AddObject(monster);
                }

                break;
            }
        }
    }

    public abstract void UpdateSpawns(TimeSpan elapsedTime);
}