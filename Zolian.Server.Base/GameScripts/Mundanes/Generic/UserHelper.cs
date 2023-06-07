using System.Globalization;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("User Helper Menu")]
public class UserHelper : MundaneScript
{
    public UserHelper(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameClient client, int serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var playerStr = client.Aisling.BonusStr.ToString();
        var playerInt = client.Aisling.BonusInt.ToString();
        var playerWis = client.Aisling.BonusWis.ToString();
        var playerCon = client.Aisling.BonusCon.ToString();
        var playerDex = client.Aisling.BonusDex.ToString();
        var playerDmg = client.Aisling.Dmg.ToString();
        var playerAc = client.Aisling.Ac.ToString();
        var playerFort = client.Aisling.Fortitude.ToString(CultureInfo.CurrentCulture);
        var playerReflex = client.Aisling.Reflex.ToString(CultureInfo.CurrentCulture);
        var playerWill = client.Aisling.Will.ToString(CultureInfo.CurrentCulture);
        var playerRegen = client.Aisling.Regen.ToString();
        var playerBleeding = client.Aisling.Bleeding.ToString();
        var playerRending = client.Aisling.Rending.ToString();
        var playerAegis = client.Aisling.Aegis.ToString();
        var playerReaping = client.Aisling.Reaping.ToString();
        var playerVamp = client.Aisling.Vampirism.ToString();
        var playerHaste = client.Aisling.Haste.ToString();
        var playerSpikes = client.Aisling.Spikes.ToString();
        var playerGust = client.Aisling.Gust.ToString();
        var playerQuake = client.Aisling.Quake.ToString();
        var playerRain = client.Aisling.Rain.ToString();
        var playerFlame = client.Aisling.Flame.ToString();
        var playerDusk = client.Aisling.Dusk.ToString();
        var playerDawn = client.Aisling.Dawn.ToString();
        var playerOffElement = ElementManager.ElementValue(client.Aisling.SecondaryOffensiveElement);
        var playerDefElement = ElementManager.ElementValue(client.Aisling.SecondaryDefensiveElement);
        var amplified = (client.Aisling.Amplified * 100).ToString(CultureInfo.CurrentCulture);
        var latency = client.LastPingResponse - client.LastPing;
        var latencyMs = $"{(uint)latency.TotalMilliseconds} ms";
        var latencyCode = ColorCodeLatency(latency);
        var mapNum = client.Aisling.Map.ID;

        client.SendMessage(0x08, $"{{=gClient Latency: {{={latencyCode}{latencyMs} {{=gMap#: {{=a{mapNum}\n{{=cSTR: {{=a{playerStr}{{=c, INT: {{=a{playerInt}{{=c, WIS: {{=a{playerWis}{{=c, CON: {{=a{playerCon}{{=c, DEX: {{=a{playerDex}\n" +
                                 $"{{=cDMG: {{=a{playerDmg}{{=c, Regen: {{=a{playerRegen}{{=c, {{=sArmor{{=c: {{=a{playerAc}, {{=sAmplified{{=c: {{=a{amplified}%\n" +
                                 $"{{=sSupport Offense{{=c: {{=a{playerOffElement}{{=c, {{=sSupport Defense{{=c: {{=a{playerDefElement}{{=c\n\n" +
                                 $"{{=eSave Throws{{=c: {{=sFortitude{{=c:{{=a{playerFort}%{{=c, {{=sReflex{{=c:{{=a{playerReflex}%{{=c, {{=sWill{{=c:{{=a{playerWill}%\n\n" +
                                 $"{{=bBleeding{{=c: {{=a{playerBleeding}{{=c, {{=rRending{{=c: {{=a{playerRending}{{=c, {{=sAegis{{=c: {{=a{playerAegis}{{=c, {{=nReaping{{=c: {{=a{playerReaping}\n" +
                                 $"{{=bVampirism{{=c: {{=a{playerVamp}{{=c, {{=cHaste{{=c: {{=a{playerHaste}{{=c, {{=wSpikes{{=c: {{=a{playerSpikes}, {{=uGust{{=c: {{=a{playerGust}{{=c\n" +
                                 $"{{=uQuake{{=c: {{=a{playerQuake}{{=c, {{=uRain{{=c: {{=a{playerRain}, {{=uFlame{{=c: {{=a{playerFlame}{{=c, {{=uDusk{{=c: {{=a{playerDusk}{{=c, {{=uDawn{{=c: {{=a{playerDawn}");
    }

    public override void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
        }
    }

    private static string ColorCodeLatency(TimeSpan time)
    {
        if (time < TimeSpan.FromMilliseconds(100)) return "q";
        if (time < TimeSpan.FromMilliseconds(200)) return "c";
        return time >= TimeSpan.FromMilliseconds(250) ? "b" : "n";
    }
}