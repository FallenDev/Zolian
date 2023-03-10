using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.TempleOfLight;

[Script("Aerith")]
public class Aerith : MundaneScript
{
    public Aerith(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client) => TopMenu(client);

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>();
        var expFraction = int.MaxValue / client.Aisling.BaseMp;
        var apFraction = int.MaxValue * 0.10;

        if (client.Aisling.ExpTotal >= expFraction)
        {
            options.Add(new(0x01, "Dedicate experience"));
        }

        if (client.Aisling.AbpTotal >= apFraction)
        {
            options.Add(new(0x02, "Dedicate ability"));
        }

        client.SendOptionsDialog(Mundane, "Let's see if we can transpose your experience to mana.", options.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        switch (responseID)
        {
            case 0x00:
                client.SendMessage(0x02, "{=cCome back again!");
                client.CloseDialog();
                break;
            case 0x01:
            {
                var options = new List<OptionsDataItem>
                {
                    new(0x03, "{=qMeditate x1"),
                    new(0x04, "{=qMeditate x10"),
                    new(0x00, "{=bSecond thought")
                };

                client.SendOptionsDialog(Mundane, "Close your eyes, focus, now let's attempt the conversion.", options.ToArray());
                break;
            }
            case 0x02:
            {
                break;
            }
            case 0x03:
            {
                var baseMp = client.Aisling.BaseMp;
                var baseExp = client.Aisling.ExpTotal;
                var i = baseMp * 500;

                if (baseExp - i >= 0)
                {
                    client.Aisling.ExpTotal -= (uint)i;
                    client.Aisling.BaseMp += 25;
                    client.SendStats(StatusFlags.ExpSpend);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(1, client.Aisling.Pos));
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(8));
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "uhh, hmm, I'm sorry looks like you don't have enough experience.");
                    break;
                }

                var options = new List<OptionsDataItem>
                {
                    new(0x03, "{=qMeditate x1"),
                    new(0x04, "{=qMeditate x10"),
                    new(0x00, "{=bSecond thought")
                };

                client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Mana: {client.Aisling.BaseMp}", options.ToArray());
                break;
            }
            case 0x04:
            {
                var baseMp = client.Aisling.BaseMp;
                var baseExp = client.Aisling.ExpTotal;
                var i = baseMp * 5000;

                if (baseExp - i >= 0)
                {
                    client.Aisling.ExpTotal -= (uint)i;
                    client.Aisling.BaseMp += 250;
                    client.SendStats(StatusFlags.ExpSpend);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(1, client.Aisling.Pos));
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(8));
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "uhh, hmm, I'm sorry looks like you don't have enough experience.");
                    break;
                }

                var options = new List<OptionsDataItem>
                {
                    new(0x03, "{=qMeditate x1"),
                    new(0x04, "{=qMeditate x10"),
                    new(0x00, "{=bSecond thought")
                };

                client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Mana: {client.Aisling.BaseMp}", options.ToArray());
                break;
            }
        }
    }
}