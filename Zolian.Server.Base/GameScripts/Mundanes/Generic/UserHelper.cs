using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;

using System.Globalization;
using System.Text.RegularExpressions;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("User Helper Menu")]
public class UserHelper(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        client.EntryCheck = serial;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var level = client.Aisling.ExpLevel;
        var ability = client.Aisling.AbpLevel;
        var bStr = client.Aisling._Str.ToString("D3");
        var baseStr = Regex.Replace(bStr, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var bInt = client.Aisling._Int.ToString("D3");
        var baseInt = Regex.Replace(bInt, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var bWis = client.Aisling._Wis.ToString("D3");
        var baseWis = Regex.Replace(bWis, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var bCon = client.Aisling._Con.ToString("D3");
        var baseCon = Regex.Replace(bCon, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var bDex = client.Aisling._Dex.ToString("D3");
        var baseDex = Regex.Replace(bDex, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var gStr = client.Aisling.BonusStr.ToString("D3");
        var gearStr = Regex.Replace(gStr, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var gInt = client.Aisling.BonusInt.ToString("D3");
        var gearInt = Regex.Replace(gInt, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var gWis = client.Aisling.BonusWis.ToString("D3");
        var gearWis = Regex.Replace(gWis, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var gCon = client.Aisling.BonusCon.ToString("D3");
        var gearCon = Regex.Replace(gCon, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var gDex = client.Aisling.BonusDex.ToString("D3");
        var gearDex = Regex.Replace(gDex, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var pStr = client.Aisling.Str.ToString("D3");
        var playerStr = Regex.Replace(pStr, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var pInt = client.Aisling.Int.ToString("D3");
        var playerInt = Regex.Replace(pInt, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var pWis = client.Aisling.Wis.ToString("D3");
        var playerWis = Regex.Replace(pWis, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var pCon = client.Aisling.Con.ToString("D3");
        var playerCon = Regex.Replace(pCon, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var pDex = client.Aisling.Dex.ToString("D3");
        var playerDex = Regex.Replace(pDex, @"\b0+", m => "".PadLeft(m.Value.Length, ' '));
        var playerDmg = client.Aisling.Dmg.ToString();
        var playerAc = client.Aisling.SealedAc.ToString();
        var playerFort = client.Aisling.Fortitude.ToString(CultureInfo.CurrentCulture);
        var playerReflex = client.Aisling.Reflex.ToString(CultureInfo.CurrentCulture);
        var playerWill = client.Aisling.Will.ToString(CultureInfo.CurrentCulture);
        var playerRegen = client.Aisling.Regen.ToString();
        var playerBleeding = client.Aisling.Bleeding.ToString();
        var playerRending = client.Aisling.Rending.ToString();
        var playerAegis = client.Aisling.Aegis.ToString();
        var playerReaping = client.Aisling.Reaping.ToString();
        var playerVamp = client.Aisling.Vampirism.ToString();
        var playerGhost = client.Aisling.Ghosting.ToString();
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
        var latency = client.Latency.Elapsed;
        var latencyMs = $"{client.Latency.Elapsed.Milliseconds} ms";
        var latencyCode = ColorCodeLatency(latency);
        var mapNum = client.Aisling.Map.ID;
        var playerBoxed = client.Aisling.ExpTotal;

        client.SendServerMessage(ServerMessageType.ScrollWindow, $"{{=gMap#: {{=a{mapNum} {{=gInsight: {{=b{level} {{=gRank: {{=b{ability} {{=gLatency: {{={latencyCode}{latencyMs}\n" +
                                                                 $"{{=gBase Stats| {{=cS:{{=a{baseStr}{{=c, I:{{=a{baseInt}{{=c, W:{{=a{baseWis}{{=c, C:{{=a{baseCon}{{=c, D:{{=a{baseDex}\n" +
                                                                 $"{{=gGear Stats| {{=cS:{{=a{gearStr}{{=c, I:{{=a{gearInt}{{=c, W:{{=a{gearWis}{{=c, C:{{=a{gearCon}{{=c, D:{{=a{gearDex}\n" +
                                                                 $"{{=gFull Stats| {{=cS:{{=a{playerStr}{{=c, I:{{=a{playerInt}{{=c, W:{{=a{playerWis}{{=c, C:{{=a{playerCon}{{=c, D:{{=a{playerDex}\n" +
                                                                 $"   {{=gOffense| {{=cDMG{{=c:{{=a{playerDmg}{{=c, {{=sAmp{{=c:{{=a{amplified}%{{=c, {{=sSecondary{{=c:{{=a{playerOffElement}\n" +
                                                                 $"   {{=gDefense| {{=sAC{{=c:{{=a{playerAc}{{=c, {{=gRegen{{=c:{{=a{playerRegen}{{=c, {{=sSecondary{{=c:{{=a{playerDefElement}\n" +
                                                                 $"{{=cExp Boxed: {{=a{playerBoxed}\n\n" +
                                                                 $"{{=eSaving Throws{{=c: {{=sFort{{=c:{{=a{playerFort}%{{=c, {{=sReflex{{=c:{{=a{playerReflex}%{{=c, {{=sWill{{=c:{{=a{playerWill}%\n" +
                                                                 $"{{=bBleeding{{=c: {{=a{playerBleeding}{{=c, {{=rRending{{=c: {{=a{playerRending}{{=c, {{=sAegis{{=c: {{=a{playerAegis}{{=c, {{=nReaping{{=c: {{=a{playerReaping}\n" +
                                                                 $"{{=bVamp{{=c: {{=a{playerVamp}{{=c, {{=uGhost{{=c: {{=a{playerGhost}{{=c, {{=cHaste{{=c: {{=a{playerHaste}{{=c, {{=wSpikes{{=c: {{=a{playerSpikes}, {{=uGust{{=c: {{=a{playerGust}{{=c\n" +
                                                                 $"{{=uQuake{{=c: {{=a{playerQuake}{{=c, {{=uRain{{=c: {{=a{playerRain}, {{=uFlame{{=c: {{=a{playerFlame}{{=c, {{=uDusk{{=c: {{=a{playerDusk}{{=c, {{=uDawn{{=c: {{=a{playerDawn}");
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
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