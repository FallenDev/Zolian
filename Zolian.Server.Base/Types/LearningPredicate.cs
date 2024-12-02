using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Templates;
using System.Text;

namespace Darkages.Types;

public class ItemPredicate
{
    public int AmountRequired { get; init; }
    public string Item { get; init; }
}

public class LearningPredicate
{
    private Template _template;
    public List<ItemPredicate> ItemsRequired = [];
    public string DisplayName { get; set; }
    public Class ClassRequired { get; set; }
    public Class SecondaryClassRequired { get; set; }
    public Race RaceRequired { get; set; }
    public int StrRequired { get; set; }
    public int IntRequired { get; set; }
    public int WisRequired { get; set; }
    public int ConRequired { get; set; }
    public int DexRequired { get; set; }
    public int ExpLevelRequired { get; set; }
    public uint ExperienceRequired { get; set; }
    public uint GoldRequired { get; set; }
    public int SkillLevelRequired { get; set; }
    public string SkillRequired { get; set; }
    public int SpellLevelRequired { get; set; }
    public string SpellRequired { get; set; }
    public ClassStage StageRequired { get; set; }
    public Job JobRequired { get; set; }
    private bool MasterRequired => StageRequired.StageFlagIsSet(ClassStage.Master);
    private int AbpLevelRequired()
    {
        var jobRequired = Convert.ToByte(StageRequired.StageFlagIsSet(ClassStage.Job));
        return jobRequired == 0 ? 0 : ExpLevelRequired;
    }

    internal string[] MetaData =>
    [
        $"{ExpLevelRequired}/{Convert.ToByte(MasterRequired)}/{AbpLevelRequired()}",
        $"{(_template is SkillTemplate template ? template.Icon : ((SpellTemplate) _template).Icon)}/0/0",
        $"{(StrRequired == 0 ? 5 : StrRequired)}/{(IntRequired == 0 ? 5 : IntRequired)}/{(WisRequired == 0 ? 5 : WisRequired)}/{(DexRequired == 0 ? 5 : DexRequired)}/{(ConRequired == 0 ? 5 : ConRequired)}",
        $"{(!string.IsNullOrEmpty(SkillRequired) ? SkillRequired : "0")}/{(SkillLevelRequired > 0 ? SkillLevelRequired : 0)}",
        $"{(!string.IsNullOrEmpty(SpellRequired) ? SpellRequired : "0")}/{(SpellLevelRequired > 0 ? SpellLevelRequired : 0)}",
        $"{_template.Description}\n\nItems Required: {(ItemsRequired.Count > 0 ? string.Join(",", ItemsRequired.Select(i => i.AmountRequired + " " + i.Item)) : "None")} \nGold: {(GoldRequired > 0 ? GoldRequired : 0)}\n{Script()}"
    ];

    private string Script()
    {
        var sb = new StringBuilder();
        switch (_template)
        {
            case SkillTemplate skillTemplate:
                sb.Append(NpcLocation(skillTemplate.NpcKey) ?? "Unknown");
                break;
            case SpellTemplate spellTemplate:
                sb.Append(NpcLocation(spellTemplate.NpcKey) ?? "Unknown");
                break;
        }

        sb.Append(DamageModifiers(_template));
        return sb.ToString();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"Stats Required: ({StrRequired.ToString()} STR, {IntRequired.ToString()} INT, {WisRequired.ToString()} WIS, {ConRequired.ToString()} CON, {DexRequired.ToString()} DEX)");
        sb.Append("\nDo you wish to learn this new ability?");
        return sb.ToString();
    }

    public void AssociatedWith<T>(T template) where T : Template
    {
        _template = template;
    }

    public bool IsMet(Aisling player, Action<string, bool> callbackMsg = null)
    {
        if (ServerSetup.Instance.Config.DevModeExemptions != null && player.GameMaster &&
            ServerSetup.Instance.Config.DevModeExemptions.Contains("learning_predicates"))
            return true;

        var result = new Dictionary<int, Tuple<bool, object>>();
        var n = 0;
        try
        {
            n = CheckSpellandSkillPredicates(player, result, n);
            n = CheckAttributePredicates(player, result, n);
            CheckItemPredicates(player, result, n);
        }
        catch (Exception ex)
        {
            player.Client.CloseDialog();
            player.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Hmm, you're not ready yet.");
            player.SendAnimationNearby(94, null, player.Serial);

            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            SentrySdk.CaptureException(ex);

            return false;
        }

        var ready = CheckPredicates(callbackMsg, result);
        {
            if (ready) player.SendAnimationNearby(94, null, player.Serial);
        }

        return ready;
    }

    private static bool CheckPredicates(Action<string, bool> callbackMsg,
        Dictionary<int, Tuple<bool, object>> result)
    {
        if (result == null || result.Count == 0)
            return false;

        var predicateResult = result.ToList().TrueForAll(i => i.Value.Item1);

        if (predicateResult)
        {
            callbackMsg?.Invoke("You have met all prerequisites, Do you wish to proceed?.", true);
            return true;
        }

        var sb = string.Empty;
        {
            var errorCaps = result.Select(i => i.Value).Distinct();

            sb += "{=sYou're not worthy. \n{=u";
            sb = errorCaps.Where(predicate => predicate is { Item1: false }).Aggregate(sb, (current, predicate) => current + ((string)predicate.Item2 + "\n"));
        }

        callbackMsg?.Invoke(sb, false);
        return false;
    }

    private static string NpcLocation(string npcKey)
    {
        if (string.IsNullOrEmpty(npcKey)) npcKey = "None";
        if (!ServerSetup.Instance.GlobalMundaneTemplateCache.TryGetValue(npcKey, out _)) return "\nThe location of this ability is not written on the scroll.\n";

        var npc = ServerSetup.Instance.GlobalMundaneTemplateCache[npcKey];

        if (!ServerSetup.Instance.GlobalMapCache.TryGetValue(npc.AreaID, out _)) return "\nThe location of this ability is not written on the scroll.\n";

        var map = ServerSetup.Instance.GlobalMapCache[npc.AreaID];
        {
            return $"\nFrom: {npc.Name}\nLocation: {map.Name}\n";
        }
    }

    private static string DamageModifiers(Template temp)
    {
        return temp is SkillTemplate ? $"\nSkill Modifiers: {temp.DamageMod ?? "None"}" : $"\nSpell Modifiers: {temp.DamageMod ?? "None"}";
    }

    private int CheckAttributePredicates(Player player, IDictionary<int, Tuple<bool, object>> result, int n)
    {
        result[n++] = new Tuple<bool, object>(player.ExpLevel >= ExpLevelRequired, $"Come back when you're the appropriate insight. (Level {ExpLevelRequired} Required)");
        result[n++] = new Tuple<bool, object>(player.Str >= StrRequired, $"Your muscle fibers aren't up for the task. ({StrRequired} Str Required)");
        result[n++] = new Tuple<bool, object>(player.Int >= IntRequired, $"You must study harder and come back when you know more. ({IntRequired} Int Required)");
        result[n++] = new Tuple<bool, object>(player.Wis >= WisRequired, $"You do not quite possess the wisdom to understand this. ({WisRequired} Wis Required)");
        result[n++] = new Tuple<bool, object>(player.Con >= ConRequired, $"You lack stamina. ({ConRequired} Con Required)");
        result[n++] = new Tuple<bool, object>(player.Dex >= DexRequired, $"You must increase your dexterity. ({DexRequired} Dex Required)");
        result[n++] = new Tuple<bool, object>(player.GoldPoints >= GoldRequired, $"My services aren't free. ({GoldRequired} Gold Required)");
        result[n++] = new Tuple<bool, object>(player.Stage >= StageRequired, "You must transcend further before you can learn this.");
        if (JobRequired != Job.None)
            result[n++] = new Tuple<bool, object>(JobRequired.JobFlagIsSet(player.JobClass), "This is not your profession.");
        else
            result[n++] = new Tuple<bool, object>(ClassRequired.ClassFlagIsSet(player.Path) || ClassRequired.ClassFlagIsSet(player.PastClass) || ClassRequired.ClassFlagIsSet(Class.Peasant)
                                              || SecondaryClassRequired.ClassFlagIsSet(player.Path) || SecondaryClassRequired.ClassFlagIsSet(player.PastClass) || SecondaryClassRequired.ClassFlagIsSet(Class.Peasant), "This is not your profession, " + player.Username);
        return n;
    }

    private int CheckItemPredicates(Aisling player, IDictionary<int, Tuple<bool, object>> result, int n)
    {
        if (ItemsRequired is not { Count: > 0 }) return n;
        var msg = new StringBuilder(ServerSetup.Instance.Config.ItemNotRequiredMsg);
        var items = ItemsRequired.Select(i => $"{i.Item} ({i.AmountRequired}) ");

        foreach (var itemStrings in items) msg.Append(itemStrings);

        var errorMsg = msg.ToString();
        var formatted = errorMsg.Replace(") ", "), ").TrimEnd(',', ' ');

        foreach (var ir in ItemsRequired)
        {
            if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(ir.Item))
            {
                result[n] = new Tuple<bool, object>(false, formatted);

                break;
            }

            var item = ServerSetup.Instance.GlobalItemTemplateCache[ir.Item];

            if (item == null)
            {
                result[n] = new Tuple<bool, object>(false, formatted);
                break;
            }

            var itemObtained = player.Inventory.Get(i => i.Template.Name.Equals(item.Name));

            var itemTotal = 0;

            foreach (var itemObj in itemObtained)
            {
                var itemCount = 0;
                if (itemObj.Template.CanStack)
                    itemCount += itemObj.Stacks;
                else
                    itemCount++;

                itemTotal += itemCount;
            }

            if (itemTotal >= ir.AmountRequired)
                result[n] = new Tuple<bool, object>(true, string.Empty);
            else
                result[n] = new Tuple<bool, object>(false, formatted);

            n++;
        }

        return n;
    }

    private int CheckSpellandSkillPredicates(Aisling player, IDictionary<int, Tuple<bool, object>> result, int n)
    {
        if (SkillRequired is not (null or ""))
        {
            var skill = ServerSetup.Instance.GlobalSkillTemplateCache[SkillRequired];
            var skillRetainer = player.SkillBook.GetSkills(i => i.Template?.Name.Equals(skill.Name) ?? false)
                .FirstOrDefault();

            if (skillRetainer == null)
            {
                result[n++] = new Tuple<bool, object>(false, $"You don't have the skill required. ({SkillRequired})");
            }
            else if (skillRetainer.Level >= SkillLevelRequired)
            {
                result[n++] = new Tuple<bool, object>(true, "Skills Required.");
            }
            else
            {
                result[n++] = new Tuple<bool, object>(false, $"{skill.Name} Must be level {SkillLevelRequired} - Enlighten {Math.Abs(skillRetainer.Level - SkillLevelRequired)} more levels.");
            }
        }

        if (SpellRequired is null or "") return n;
        {
            var spell = ServerSetup.Instance.GlobalSpellTemplateCache[SpellRequired];
            var spellRetainer = player.SpellBook.TryGetSpells(i => i?.Template != null && i.Template.Name.Equals(spell.Name)).FirstOrDefault();

            if (spellRetainer == null)
            {
                result[n++] = new Tuple<bool, object>(false, $"You don't have the spell required. ({SpellRequired})");
            }
            else if (spellRetainer.Level >= SpellLevelRequired)
            {
                result[n++] = new Tuple<bool, object>(true, "Spells Required.");
            }
            else
            {
                result[n++] = new Tuple<bool, object>(false, $"{spell.Name} Must be level {SpellLevelRequired} - Enlighten {Math.Abs(spellRetainer.Level - SpellLevelRequired)} more levels.");
            }
        }

        return n;
    }
}