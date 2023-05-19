using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Barber")]
public class Barber : MundaneScript
{
    private int _styleNumber;
    private int _colorNumber;

    public Barber(GameServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(GameServer server, GameClient client)
    {
        if (Mundane.WithinEarShotOf(client.Aisling))
        {
            TopMenu(client);
        }
    }

    public override void TopMenu(IGameClient client)
    {
        var opts = new List<OptionsDataItem>
        {
            new (0x02, "Style"),
            new (0x03, "Color")
        };

        client.SendOptionsDialog(Mundane, "Looking for a change? Ok, get in the chair.", opts.ToArray());
    }

    public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
    {
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return;
        }

        if (!Mundane.WithinEarShotOf(client.Aisling)) return;

        while (true)
        {
            switch (responseID)
            {
                case 0x01:
                {
                    if (args != null && client.Aisling.Styling == 2)
                    {
                        int.TryParse(args, out var numbers);
                        if (numbers > 0)
                        {
                            _styleNumber = numbers;
                            responseID = 0x0A;
                            args = null;
                            continue;
                        }

                        responseID = 0x0D;
                        args = null;
                        continue;
                    }

                    if (args != null && client.Aisling.Coloring == 2)
                    {
                        int.TryParse(args, out var numbers);
                        if (numbers > 0)
                        {
                            _colorNumber = numbers;
                            responseID = 0x014;
                            args = null;
                            continue;
                        }

                        responseID = 0x0060;
                        args = null;
                        continue;
                    }
                }
                    break;
                #region Styling

                case 0x02:
                {
                    client.Aisling.Styling = 2;
                    client.Aisling.HairStyle = client.Aisling.OldStyle;
                    client.UpdateDisplay();
                    client.Send(new ReactorInputSequence(Mundane, "Which style are you interested in?", "1-65", 2));
                }
                    break;
                case 0x0A:
                {
                    if (_styleNumber is 0 or > 65)
                    {
                        client.Send(new ReactorInputSequence(Mundane, "Please select a valid hairstyle:", "1-65", 2));
                    }
                    else
                    {
                        responseID = 0x0B;
                        args = null;
                        continue;
                    }
                }
                    break;
                case 0x0B:
                {
                    var opts = new List<OptionsDataItem>
                    {
                        new (0x02, "It's not doing it for me"),
                        new (0x0C, "Let's go with that")
                    };

                    client.Aisling.Styling = 1;
                    client.Aisling.HairStyle = Convert.ToByte(_styleNumber);
                    client.UpdateDisplay();
                    client.SendOptionsDialog(Mundane, $"So you're interested in #{_styleNumber}?\nIt'll cost you 5000 gold.", opts.ToArray());
                }
                    break;
                case 0x0C:
                {
                    if (client.Aisling.GoldPoints >= 5000)
                    {
                        client.Aisling.GoldPoints -= 5000;
                        client.SendMessage(0x03, "Great! Thank you for the business, come again.");
                        client.Aisling.Styling = 0;
                        client.Aisling.OldStyle = client.Aisling.HairStyle;
                        client.SendStats(StatusFlags.StructC);
                        TopMenu(client);
                    }
                    else
                    {
                        client.Aisling.HairStyle = client.Aisling.OldStyle;
                        client.UpdateDisplay();
                        client.SendOptionsDialog(Mundane, "No money, no style.");
                    }
                }
                    break;
                case 0x0D:
                {
                    client.Send(new ReactorInputSequence(Mundane, "Please select a valid hairstyle:", "1-65", 2));
                }
                    break;

                #endregion
                #region Coloring

                case 0x03:
                {
                    client.Aisling.Coloring = 2;
                    client.Aisling.HairColor = client.Aisling.OldColor;
                    client.UpdateDisplay();
                    client.Send(new ReactorInputSequence(Mundane, "Which dye would you like in your hair?", "0-55", 2));
                }
                    break;
                case 0x014:
                {
                    if (_colorNumber is <= 0 or > 55)
                    {
                        client.Send(new ReactorInputSequence(Mundane, "Please select a valid color:", "0-55", 2));
                    }
                    else
                    {
                        responseID = 0x015;
                        args = null;
                        continue;
                    }
                }
                    break;
                case 0x015:
                {
                    var opts = new List<OptionsDataItem>
                    {
                        new (0x03, "It's not doing it for me"),
                        new (0x016, "Let's go with that")
                    };

                    client.Aisling.Coloring = 1;
                    client.Aisling.HairColor = Convert.ToByte(_colorNumber);
                    client.UpdateDisplay();
                    client.SendOptionsDialog(Mundane, $"You're interested in #{_colorNumber}?\nIt'll cost you 20000 gold.", opts.ToArray());
                }
                    break;
                case 0x016:
                {
                    if (client.Aisling.GoldPoints >= 20000)
                    {
                        client.Aisling.GoldPoints -= 20000;
                        client.SendMessage(0x03, "Great! Thank you for the business, come again.");
                        client.Aisling.Coloring = 0;
                        client.Aisling.OldColor = client.Aisling.HairColor;
                        client.SendStats(StatusFlags.StructC);
                        TopMenu(client);
                    }
                    else
                    {
                        client.Aisling.HairColor = client.Aisling.OldColor;
                        client.UpdateDisplay();
                        client.SendOptionsDialog(Mundane, "No money, no color.");
                    }
                }
                    break;
                case 0x017:
                {
                    client.Send(new ReactorInputSequence(Mundane, "Please select a valid color:", "0-55", 2));
                }
                    break;

                #endregion
            }
            break;
        }
    }
}