using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.TempleOfLight;

[Script("Cloud")]
public class Cloud : MundaneScript
{
    public Cloud(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client) => TopMenu(client);

    public override void TopMenu(IGameClient client)
    {
        var options = new List<OptionsDataItem>();
        var expFraction = int.MaxValue / client.Aisling.BaseHp;
        var apFraction = int.MaxValue * 0.10;

        if (client.Aisling.ExpTotal >= expFraction)
        {
            options.Add(new(0x01, "Dedicate experience"));
        }

        if (client.Aisling.AbpTotal >= apFraction)
        {
            options.Add(new(0x02, "Dedicate ability"));
        }

        client.SendOptionsDialog(Mundane, "I can help you transpose your experience with your health.", options.ToArray());
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
                client.SendMessage(0x02, "{=cSee you soon");
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

                client.SendOptionsDialog(Mundane, "Ready? Mediate and we'll convert experience to health.", options.ToArray());
                break;
            }
            case 0x02:
            {
                break;
            }
            case 0x03:
            {
                var baseHp = client.Aisling.BaseHp;
                var baseExp = client.Aisling.ExpTotal;
                var i = baseHp * 500;

                if (baseExp - i >= 0)
                {
                    client.Aisling.ExpTotal -= (uint)i;
                    client.Aisling.BaseHp += 50;
                    client.SendStats(StatusFlags.ExpSpend);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(2, client.Aisling.Pos));
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(0));
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "Not enough experience, sorry.");
                    break;
                }

                var options = new List<OptionsDataItem>
                {
                    new(0x03, "{=qMeditate x1"),
                    new(0x04, "{=qMeditate x10"),
                    new(0x00, "{=bSecond thought")
                };

                client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Health: {client.Aisling.BaseHp}", options.ToArray());
                break;
            }
            case 0x04:
            {
                var baseHp = client.Aisling.BaseHp;
                var baseExp = client.Aisling.ExpTotal;
                var i = baseHp * 5000;

                if (baseExp - i >= 0)
                {
                    client.Aisling.ExpTotal -= (uint)i;
                    client.Aisling.BaseHp += 500;
                    client.SendStats(StatusFlags.ExpSpend);
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(2, client.Aisling.Pos));
                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat19(0));
                }
                else
                {
                    client.SendOptionsDialog(Mundane, "Not enough experience, sorry.");
                    break;
                }

                var options = new List<OptionsDataItem>
                {
                    new(0x03, "{=qMeditate x1"),
                    new(0x04, "{=qMeditate x10"),
                    new(0x00, "{=bSecond thought")
                };

                client.SendOptionsDialog(Mundane, $"{client.Aisling.ExpTotal} left\nBase Health: {client.Aisling.BaseHp}", options.ToArray());
                break;
            }
        }
    }
}