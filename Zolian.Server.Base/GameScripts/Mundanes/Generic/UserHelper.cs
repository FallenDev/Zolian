using System.Globalization;
using System.Text;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;

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

        var a = client.Aisling;
        var level = a.ExpLevel;
        var ability = a.AbpLevel;
        var baseStr = D3(a._Str);
        var baseInt = D3(a._Int);
        var baseWis = D3(a._Wis);
        var baseCon = D3(a._Con);
        var baseDex = D3(a._Dex);
        var gearStr = D3(a.BonusStr);
        var gearInt = D3(a.BonusInt);
        var gearWis = D3(a.BonusWis);
        var gearCon = D3(a.BonusCon);
        var gearDex = D3(a.BonusDex);
        var playerStr = D3(a.Str);
        var playerInt = D3(a.Int);
        var playerWis = D3(a.Wis);
        var playerCon = D3(a.Con);
        var playerDex = D3(a.Dex);

        var playerDmg = a.Dmg;
        var playerAc = a.SealedAc;
        var playerRegen = a.Regen;

        var fort = a.Fortitude.ToString("0.#", CultureInfo.InvariantCulture);
        var reflex = a.Reflex.ToString("0.#", CultureInfo.InvariantCulture);
        var will = a.Will.ToString("0.#", CultureInfo.InvariantCulture);
        var playerOffPriElement = ElementManager.ElementValue(a.OffenseElement);
        var playerDefPriElement = ElementManager.ElementValue(a.DefenseElement);
        var playerOffSecElement = ElementManager.ElementValue(a.SecondaryOffensiveElement);
        var playerDefSecElement = ElementManager.ElementValue(a.SecondaryDefensiveElement);
        var amplifiedPct = (a.Amplified * 100).ToString("0.#", CultureInfo.InvariantCulture);

        var rtt = client.LastRttMs;
        var avg = client.SmoothedRttMs;
        var rttCode = ColorCodeLatency(rtt);
        var avgCode = ColorCodeLatency(avg);

        // Non-color cue for spikes (colorblind helper)
        static string Cue(int ms) => ms >= 250 ? "!!" : ms >= 150 ? "!" : "";

        var mapNum = a.Map.ID;
        if (mapNum is >= 800 and <= 810) mapNum = 0;

        var sb = new StringBuilder(900);

        // Player Info Card
        sb.Append("{=q").Append(a.Username)
          .Append("    {=gLvl: {=b").Append(level)
          .Append(" {=gJob: {=b").Append(ability)
          .Append(" {=gIns: {=b").Append(level + ability)
          .Append('\n');

        // Map, Ping, EMA Latency
        sb.Append("{=gMap#: {=e").Append(mapNum)
          .Append(" {=gGrid: {=e").Append(a.Position.X).Append("{=c,").Append("{=e").Append(a.Position.Y)
          .Append("   {=gPing: {=").Append(rttCode).Append(rtt).Append("{=ams").Append(Cue(rtt))
          .Append(" {=gAvg: {=").Append(avgCode).Append(avg).Append("{=ams").Append(Cue(avg))
          .Append('\n');

        // Stats Blocks
        sb.Append("{=gBase| {=cS:{=a").Append(baseStr).Append("{=c, I:{=a").Append(baseInt)
          .Append("{=c, W:{=a").Append(baseWis).Append("{=c, C:{=a").Append(baseCon)
          .Append("{=c, D:{=a").Append(baseDex).Append('\n');

        sb.Append("{=gGear| {=cS:{=a").Append(gearStr).Append("{=c, I:{=a").Append(gearInt)
          .Append("{=c, W:{=a").Append(gearWis).Append("{=c, C:{=a").Append(gearCon)
          .Append("{=c, D:{=a").Append(gearDex).Append('\n');

        sb.Append("{=gMax | {=cS:{=a").Append(playerStr).Append("{=c, I:{=a").Append(playerInt)
          .Append("{=c, W:{=a").Append(playerWis).Append("{=c, C:{=a").Append(playerCon)
          .Append("{=c, D:{=a").Append(playerDex).Append('\n');

        // Offense / Defense
        sb.Append("{=gOff | {=sDMG{=c:{=a").Append(playerDmg).Append("{=c, {=sAmp{=c:{=a").Append(amplifiedPct)
          .Append("{=c%, {=sPri{=c:{=a").Append(playerOffPriElement).Append("{=c, {=sSec{=c:{=a").Append(playerOffSecElement)
          .Append('\n');

        sb.Append("{=gDef |  {=sAC{=c:{=a").Append(playerAc).Append("{=c, {=sRegen{=c:{=a").Append(playerRegen)
          .Append("{=c%, {=sPri{=c:{=a").Append(playerDefPriElement).Append("{=c, {=sSec{=c:{=a").Append(playerDefSecElement)
          .Append('\n');

        // Saving Throws
        sb.Append("{=eSAVE{=g| {=sFort{=c:{=a").Append(fort).Append("{=c%, ")
          .Append("{=sRef{=c:{=a").Append(reflex).Append("{=c%, ")
          .Append("{=sWill{=c:{=a").Append(will).Append("{=c%")
          .Append("\n\n");

        // Exp
        sb.Append("{=cExp Boxed: {=a").Append(a.ExpTotal).Append("    {=q--Buffs Below").Append('\n');

        // Buff Grid
        sb.Append("{=bBleed{=c: {=").Append(BuffColor(a.Bleeding)).Append(a.Bleeding)
          .Append("{=c, {=rRend{=c: {=").Append(BuffColor(a.Rending)).Append(a.Rending)
          .Append("{=c, {=sAegis{=c: {=").Append(BuffColor(a.Aegis)).Append(a.Aegis)
          .Append("{=c, {=nReap{=c: {=").Append(BuffColor(a.Reaping)).Append(a.Reaping)
          .Append('\n');

        sb.Append("{=bVamp{=c: {=").Append(BuffColor(a.Vampirism)).Append(a.Vampirism)
          .Append("{=c, {=uGhost{=c: {=").Append(BuffColor(a.Ghosting)).Append(a.Ghosting)
          .Append("{=c, {=cHaste{=c: {=").Append(BuffColor(a.Haste)).Append(a.Haste)
          .Append("{=c, {=wSpikes{=c: {=").Append(BuffColor(a.Spikes)).Append(a.Spikes)
          .Append("{=c, {=uGust{=c: {=").Append(BuffColor(a.Gust)).Append(a.Gust)
          .Append('\n');

        sb.Append("{=uQuake{=c: {=").Append(BuffColor(a.Quake)).Append(a.Quake)
          .Append("{=c, {=uRain{=c: {=").Append(BuffColor(a.Rain)).Append(a.Rain)
          .Append("{=c, {=uFlame{=c: {=").Append(BuffColor(a.Flame)).Append(a.Flame)
          .Append("{=c, {=uDusk{=c: {=").Append(BuffColor(a.Dusk)).Append(a.Dusk)
          .Append("{=c, {=uDawn{=c: {=").Append(BuffColor(a.Dawn)).Append(a.Dawn);

        client.SendServerMessage(ServerMessageType.ScrollWindow, sb.ToString());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (Mundane.Serial != client.EntryCheck)
        {
            client.CloseDialog();
        }
    }

    private static string ColorCodeLatency(int time)
    {
        if (time < 100) return "q";
        if (time < 200) return "c";
        return time >= 250 ? "n" : "b";
    }

    private static string BuffColor(int value) => value > 0 ? "q" : "a";

    private static string D3(int value)
    {
        if (value < 0) value = 0;
        if (value > 1000) value = 1000;

        return value switch
        {
            < 10 => $"   {value}",
            < 100 => $"  {value}",
            < 1000 => $" {value}",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };
    }
}