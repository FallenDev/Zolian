using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat33 : NetworkFormat
    {
        private ServerFormat33()
        {
            Encrypted = true;
            Command = 0x33;
        }

        /// <summary>
        /// Display Player
        /// </summary>
        /// <param name="aisling"></param>
        public ServerFormat33(Aisling aisling) : this() => Aisling = aisling;

        private Aisling Aisling { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (Aisling.Abyss) return;

            writer.Write((ushort)Aisling.Pos.X);
            writer.Write((ushort)Aisling.Pos.Y);
            writer.Write(Aisling.Direction);
            writer.Write((uint)Aisling.Serial);

            if (Aisling.MonsterForm > 0)
            {
                writer.Write((byte)0xFF);
                writer.Write((byte)0xFF);
                writer.Write(Aisling.MonsterForm);
                writer.Write((byte)0x01);
                writer.Write((byte)0x3A);
                writer.Write(new byte[7]);
                writer.WriteStringA(Aisling.Username);
            }
            else
            {
                var displayFlag = (byte)Aisling.Gender;
                byte displaySwitch = 0x00;

                if (Aisling.Dead)
                    displayFlag += (byte)(Aisling.Gender == Gender.Male ? 0x20 : 0x10);
                else
                {
                    if (Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.NumbFall) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Plagued))
                    {
                        displaySwitch = 0x01;
                    }

                    if (Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Stricken) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.LockJoint) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.TheShakes))
                    {
                        displaySwitch = 0x02;
                    }

                    if (Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Rabies) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Petrified) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Diseased) ||
                        Aisling.Afflictions.AfflictionFlagIsSet(RacialAfflictions.Hallowed))
                    {
                        displaySwitch = 0x03;
                    }

                    switch (displaySwitch)
                    {
                        case 0x00:
                            break;
                        // green
                        case 0x01:
                            displayFlag += (byte)(Aisling.Gender == Gender.Male ? 0x60 : 0x40);
                            break;
                        // red
                        case 0x02:
                            displayFlag += (byte)(Aisling.Gender == Gender.Male ? 0xA0 : 0xB0);
                            break;
                        // blue
                        case 0x03:
                            displayFlag += (byte)(Aisling.Gender == Gender.Male ? 0x90 : 0x20);
                            break;
                        // vampire
                        case 0x04:
                            break;
                        // lycan
                        case 0x05:
                            break;
                    }
                }

                if (Aisling.Invisible)
                    displayFlag = (byte)Gender.Invisible;

                if (!Aisling.Invisible)
                {
                    if (!Aisling.Dead)
                        switch (Aisling.Gender)
                        {
                            //Hair Style
                            case Gender.Male when Aisling.HelmetImg > 100 && !Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill):
                                writer.Write((ushort)Aisling.HelmetImg);
                                break;
                            case Gender.Male:
                                writer.Write((ushort)Aisling.HairStyle);
                                break;
                            case Gender.Female when Aisling.HelmetImg > 100 && !Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill):
                                writer.Write((ushort)Aisling.HelmetImg);
                                break;
                            case Gender.Female:
                                writer.Write((ushort)Aisling.HairStyle);
                                break;
                        }
                    else
                        writer.Write((ushort)0);
                }
                else
                {
                    writer.Write((ushort)0);
                }

                writer.Write(Aisling.Dead || Aisling.Invisible ? displayFlag : (byte)(displayFlag + Aisling.Pants));

                if (!Aisling.Dead && !Aisling.Invisible)
                {
                    writer.Write((ushort)Aisling.ArmorImg);
                    writer.Write((byte)Aisling.BootsImg);
                    writer.Write((ushort)Aisling.ArmorImg);
                    writer.Write((byte)Aisling.ShieldImg);
                    writer.Write((byte)Aisling.WeaponImg);
                    writer.Write(Aisling.HairColor);
                    writer.Write(Aisling.BootColor);
                    writer.Write((ushort)Aisling.HeadAccessory1Img);
                    writer.Write(Aisling.Lantern);
                    writer.Write((byte)Aisling.HeadAccessory2Img);
                    writer.Write((byte)Aisling.HeadAccessory2Img);
                    writer.Write((byte)Aisling.Resting);
                    writer.Write((ushort)Aisling.OverCoatImg);
                    writer.Write(Aisling.OverCoatColor);
                }
                else
                {
                    writer.Write((ushort)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)0);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write(Aisling.HairColor);
                    writer.Write(Aisling.BootColor);
                    writer.Write((ushort)0);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)0);
                    writer.Write((byte)0);
                }
            }

            if (Aisling.Map is {Ready: true} && Aisling.LoggedIn)
            {
                if (Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                {
                    writer.Write((byte)NameDisplayStyle.RedAlwaysOn);
                }
                else
                {
                    writer.Write((byte)NameDisplayStyle.GreyHover);
                }
            }
            else
                writer.Write((byte)NameDisplayStyle.GreenHover);

            writer.WriteStringA(Aisling.Username ?? string.Empty);
        }
    }
}