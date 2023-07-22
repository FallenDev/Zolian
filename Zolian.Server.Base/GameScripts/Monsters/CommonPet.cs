using System.Security.Cryptography;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;
using Microsoft.AppCenter.Crashes;

namespace Darkages.GameScripts.Monsters;

[Script("Common Pet")]
public class CommonPet : MonsterScript
{
    private readonly List<SkillScript> _skillScripts = new();
    private readonly List<SpellScript> _spellScripts = new();

    public CommonPet(Monster monster, Area map)
        : base(monster, map)
    {
        //example lazy load some spell scripts, normally we load these from template.
        LoadSkillScript("Assail", true);
        LoadSpellScript("Ard Srad");
        LoadSpellScript("Ard Cradh");
        LoadSpellScript("Pramh");

        if (Monster.Template.SpellScripts != null)
            foreach (var spellScriptStr in Monster.Template.SpellScripts)
                LoadSpellScript(spellScriptStr);

        if (Monster.Template.SkillScripts == null) return;
        foreach (var skillScriptStr in Monster.Template.SkillScripts)
            LoadSkillScript(skillScriptStr);
    }

    public override void OnDeath(WorldClient client)
    {
        client.Aisling.SummonObjects?.DeSpawn();
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster == null) return;

        if (!Monster.IsAlive)
        {
            Monster.Remove();
            Monster.Summoner?.SummonObjects?.DeSpawn();
            return;
        }

        if (Monster.CantAttack || Monster.CantCast) return;

        if (Monster.Summoner != null)
            if (!Monster.Summoner.View.ContainsKey(Monster.Serial))
            {
                Monster.ShowTo(Monster.Summoner);
                Monster.Summoner.View.TryAdd(Monster.Serial, Monster);
            }

        MonsterState(elapsedTime);
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        void UpdateTarget()
        {
            Monster.Target = Monster.Summoner.Target ?? GetObjects(Monster.Map,
                p => p.Target != null && Monster.Summoner.Serial != p.Serial && p.Target.Serial == Monster.Summoner?.Serial &&
                     p.Target.Serial != Monster.Serial, Get.All).MinBy(i => i.Position.DistanceFrom(Monster.Summoner.Position));


            if (Monster.Target == null) return;
            if (Monster.Target.CurrentHp == 0 ||
                !Monster.WithinRangeOf(Monster.Target) ||
                Monster.Target != null &&
                Monster.Summoner != null &&
                Monster.Target.Serial == Monster.Summoner.Serial)
                Monster.Target = null;
        }

        void PetMove()
        {
            if (!Monster.WalkTimer.Update(elapsedTime)) return;

            try
            {
                // get target.
                UpdateTarget();

                if (Monster.Target == null)
                {
                    // get the summoner from the obj manager, in case state was lost (summoner re-logged during lifecycle)
                    var summoner = GetObject<Aisling>(null,
                        i => string.Equals(i.Username, Monster.Summoner.Username, StringComparison.CurrentCultureIgnoreCase));

                    if (summoner != null && Monster.Position.DistanceFrom(summoner.Position) > 2)
                    {
                        Monster.WalkTo(summoner.X, summoner.Y);
                    }
                }
                else
                {
                    Monster.WalkTo(Monster.Target.X, Monster.Target.Y);
                }
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        void PetCast()
        {
            if (!Monster.CastTimer.Update(elapsedTime)) return;

            UpdateTarget();

            if (Monster.CantCast)
                return;

            if (Monster.Target == null)
                return;

            if (!Monster.Target.WithinRangeOf(Monster))
                return;

            if (Monster?.Target == null || _spellScripts.Count <= 0) return;
            var spellIdx = RandomNumberGenerator.GetInt32(_spellScripts.Count);
            if (_spellScripts[spellIdx] == null) return;
            _spellScripts[spellIdx].OnUse(Monster, Monster.Target);
        }

        void PetAttack()
        {
            if (!Monster.BashTimer.Update(elapsedTime))
                return;

            UpdateTarget();

            if (Monster.Target != null &&
                !Monster.Facing(Monster.Target.X, Monster.Target.Y, out var facingDirection))
            {
                if (Monster.Position.IsNextTo(Monster.Target.Position))
                {
                    Monster.Direction = (byte)facingDirection;
                    Monster.Turn();
                }

                if (Monster.Facing(Monster.Target.X, Monster.Target.Y, out var newDirection))
                {
                    DefaultAttack();
                }
            }

            if (Monster.Target != null && Monster.Facing(Monster.Target.X, Monster.Target.Y, out var facing))
            {
                DefaultAttack();
            }
        }

        bool DefaultAttack()
        {
            var sObj = _skillScripts.FirstOrDefault(i => i.Skill.Ready);

            if (sObj == null)
                return true;

            var skill = sObj.Skill;
            sObj.OnUse(Monster);
            {
                skill.InUse = true;
                skill.CurrentCooldown = skill.Template.Cooldown > 0 ? skill.Template.Cooldown : 0;
            }

            skill.InUse = false;
            return false;
        }

        PetAttack();
        PetMove();
        PetCast();
    }

    private void LoadSkillScript(string skillScriptStr, bool primary = false)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSkillTemplateCache.ContainsKey(skillScriptStr)) return;
            var scripts = ScriptManager.Load<SkillScript>(skillScriptStr,
                Skill.Create(1, ServerSetup.Instance.GlobalSkillTemplateCache[skillScriptStr]));

            foreach (var script in scripts.Values.Where(script => script != null))
            {
                _skillScripts.Add(script);
            }
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }

    private void LoadSpellScript(string spellScriptStr, bool primary = false)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey(spellScriptStr)) return;
            var scripts = ScriptManager.Load<SpellScript>(spellScriptStr,
                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache[spellScriptStr]));

            foreach (var script in scripts.Values.Where(script => script != null))
            {
                script.IsScriptDefault = primary;
                _spellScripts.Add(script);
            }
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }

    public override void OnClick(WorldClient client) { }
}