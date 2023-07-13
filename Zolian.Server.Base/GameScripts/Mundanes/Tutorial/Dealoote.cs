using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tutorial;

[Script("First Quest")]
public class Dealoote : MundaneScript
{
    public Dealoote(WorldServer server, Mundane mundane) : base(server, mundane) { }


    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        if (client.Aisling.Path == Class.Peasant)
        {
            var options = new List<Dialog.OptionsDataItem>
            {
                new (0x01, "{=qKeys and Controls"),
                new (0x02, "{=bZolian Guide"),
                new (0x03, "Where am I? How do I get out of here?!")
            };

            client.SendOptionsDialog(Mundane, "Hey! Wow, I'm sure glad to see someone else here.", options.ToArray());
        }
        else
        {
            client.SendOptionsDialog(Mundane, "Don't forget about me.");
        }
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 1:
                client.SendOptionsDialog(Mundane, "{=bZolian {=ais a private server using DarkAges ({=qNexon{=a)'s client architecture. \nThe controls are simple, however they are old and unique to this system.",
                    new Dialog.OptionsDataItem(0x04, "{=qTop"),
                    new Dialog.OptionsDataItem(0x06, "Player Movement"),
                    new Dialog.OptionsDataItem(0x07, "User Interface"),
                    new Dialog.OptionsDataItem(0x08, "Messages & Chat"),
                    new Dialog.OptionsDataItem(0x05, "{=bEnd"));
                break;
            case 2:
                client.SendOptionsDialog(Mundane, "{=aIf you press ({=qa{=a) on your keyboard it will take you to your inventory. There you'll find an item called '{=bZolian Guide{=a', {=qdouble click {=ait to open up a window which will give you a brief overview of pretty much everything in {=bZolian{=a.",
                    new Dialog.OptionsDataItem(0x04, "{=qTop"),
                    new Dialog.OptionsDataItem(0x05, "{=bEnd"));
                break;
            case 3:
                client.SendOptionsDialog(Mundane, "{=cThe Tutorial.\n{=aThere was someone up ahead that refused to talk to me, perhaps you'll have better luck. \nAlso, watch out for the mantis here, they seem to have an unique item in hand.");
                break;
            case 4:
                TopMenu(client);
                break;
            case 5:
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "I'll be here if you ever need me.");
                client.CloseDialog();
                break;
            case 6:
                client.SendOptionsDialog(Mundane, "{=aUnlike most MMO's where you can use '{=qwasd{=a' instead you may use '{=qzxcv{=a' or your arrow keys. \nYou may also use your mouse to right click around this 2.5D game. \nTo see your position at any given time, press '{=qTAB{=a'.",
                    new Dialog.OptionsDataItem(0x04, "{=qTop"),
                    new Dialog.OptionsDataItem(0x01, "{=cBack"),
                    new Dialog.OptionsDataItem(0x05, "{=bEnd"));
                break;
            case 7:
                client.SendOptionsDialog(Mundane, "{=uCharacter Sheet: {=aPress ({=qa{=a) x2 \n{=uInventory: {=aPress ({=qa{=a) \n{=uSkills: {=aPress ({=qs{=a) \n{=uAdv Skills: {=aHold '{=qShift{=a' + ({=qs{=a) \n{=uSpells: {=aPress ({=qd{=a) \n{=uAdv Spells: {=aHold '{=qShift{=a' + ({=qd{=a) \n{=uTactical Spells & Skills: {=aPress ({=qh{=a) \n{=uStats: {=aPress ({=qg{=a) \n{=uAdv Stats: {=aPress '{=qF1{=a'",
                    new Dialog.OptionsDataItem(0x04, "{=qTop"),
                    new Dialog.OptionsDataItem(0x01, "{=cBack"),
                    new Dialog.OptionsDataItem(0x05, "{=bEnd"));
                break;
            case 8:
                client.SendOptionsDialog(Mundane, "{=uChat Pane: {=aPress ({=qf{=a), To talk press '{=qEnter{=a' first. \n{=uAlert Pane: {=aHold '{=qShift{=a' + ({=qf{=a) \n{=uPersonal Message: {=aHold '{=qShift{=a' + ({=q'{=a), then enter in the player's {=qname{=a. \n{=uGroup Chat: {=aHold '{=qShift{=a' + ({=q'{=a), then enter in ({=q!!{=a). \n{=uGuild Chat: {=aHold '{=qShift{=a' + ({=q'{=a), then enter in ({=q!{=a).",
                    new Dialog.OptionsDataItem(0x04, "{=qTop"),
                    new Dialog.OptionsDataItem(0x01, "{=cBack"),
                    new Dialog.OptionsDataItem(0x05, "{=bEnd"));
                break;
        }
    }
}