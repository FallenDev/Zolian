using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Hubae")]
public class Hubae(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.Path == Class.Monk || client.Aisling.PastClass == Class.Monk)
        {
            if (client.Aisling.LegendBook.Has("Yellow Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Pummel"))
                options.Add(new(0x01, "Learn Pummel"));
            if (client.Aisling.LegendBook.Has("Orange Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Thump"))
                options.Add(new(0x02, "Learn Thump"));
            if (client.Aisling.LegendBook.Has("Green Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Healing Palms"))
                options.Add(new(0x03, "Learn Healing Palms"));
            if (client.Aisling.LegendBook.Has("Purple Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Eye Gouge"))
                options.Add(new(0x04, "Learn Eye Gouge"));
            if (client.Aisling.LegendBook.Has("Blue Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Calming Mist"))
                options.Add(new(0x05, "Learn Calming Mist"));
            if (client.Aisling.LegendBook.Has("Brown Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Tiger Palm"))
                options.Add(new(0x06, "Learn Tiger Palm"));
            if (client.Aisling.LegendBook.Has("Red Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Ember Strike"))
                options.Add(new(0x07, "Learn Ember Strike"));
            if (client.Aisling.LegendBook.Has("Black Belt Attainment") && !client.Aisling.SkillBook.HasSkill("Ninth Gate"))
                options.Add(new(0x08, "Learn Ninth Gate"));
        }

        client.SendOptionsDialog(Mundane, "My purpose is to train you, come; Let's tame your soul.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x01:
                var skill = Skill.GiveTo(client.Aisling, "Pummel");
                if (skill) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cPummel");
                break;
            case 0x02:
                var skill2 = Skill.GiveTo(client.Aisling, "Thump");
                if (skill2) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cThump");
                break;
            case 0x03:
                var skill3 = Skill.GiveTo(client.Aisling, "Healing Palms");
                if (skill3) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cHealing Palms");
                break;
            case 0x04:
                var skill4 = Skill.GiveTo(client.Aisling, "Eye Gouge");
                if (skill4) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cEye Gouge");
                break;
            case 0x05:
                var skill5 = Skill.GiveTo(client.Aisling, "Calming Mist");
                if (skill5) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cCalming Mist");
                break;
            case 0x06:
                var skill6 = Skill.GiveTo(client.Aisling, "Tiger Palm");
                if (skill6) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cTiger Palm");
                break;
            case 0x07:
                var skill7 = Skill.GiveTo(client.Aisling, "Ember Strike");
                if (skill7) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cEmber Strike");
                break;
            case 0x08:
                var skill8 = Skill.GiveTo(client.Aisling, "Ninth Gate");
                if (skill8) client.LoadSkillBook();
                client.SendOptionsDialog(Mundane, $"Your dedication has taught you the ability of {{=cNinth Gate");
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}