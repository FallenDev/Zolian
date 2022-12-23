using System.Text;

using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat39 : NetworkFormat
    {
        /// <summary>
        /// Self Profile
        /// </summary>
        /// <param name="aisling"></param>
        public ServerFormat39(Aisling aisling) : this() => Aisling = aisling;

        private ServerFormat39()
        {
            Encrypted = true;
            Command = 0x39;
        }

        private Aisling Aisling { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter packet)
        {
            if (Aisling.Abyss) return;

            packet.Write(Aisling.PlayerNation.NationId);
            packet.WriteStringA($"Level: {Aisling.ExpLevel}  DR: {Aisling.AbpLevel}");
            packet.WriteStringA(Aisling.GameMaster
                ? "Game Master"
                : $"Vit: {Aisling.BaseHp + Aisling.BaseMp * 2}"); 
            
            var isGrouped = Aisling.GroupParty?.PartyMembers != null && Aisling.GroupParty != null && Aisling.GroupParty.PartyMembers.Count(i => i.Serial != Aisling.Serial) > 0;
            var groupedCount = Aisling.GroupParty?.PartyMembers?.Count.ToString();

            if (!isGrouped)
            {
                packet.WriteStringA("Solo Hunting");
            }
            else
            {
                var sb = new StringBuilder("그룹구성원\n");
                foreach (var player in Aisling.GroupParty?.PartyMembers!)
                    sb.Append($"{(string.Equals(player.Username, Aisling.GroupParty?.LeaderName, StringComparison.CurrentCultureIgnoreCase) ? "*" : " ")} {player.Username}\n");

                sb.Append($"총 {groupedCount}명");
                packet.WriteStringA(sb.ToString());
            }

            packet.Write((byte) Aisling.PartyStatus);
            packet.Write((byte) 0x00);
            packet.Write((byte) Aisling.Path);
            packet.Write((byte) 0x00);
            packet.Write((byte) 0x00);
            packet.WriteStringA(Aisling.Stage != ClassStage.Class ? ClassStrings.StageValue(Aisling.Stage) : ClassStrings.ClassValue(Aisling.Path));
            packet.WriteStringA(Aisling.Clan);

            var legends = Aisling.LegendBook.LegendMarks.DistinctBy(m => m.Value);
            var legendsCount = Aisling.LegendBook.LegendMarks;
            var legendItems = legends.ToList();

            packet.Write((byte)legendItems.Count);

            foreach (var mark in legendItems.Where(m => m != null))
            {
                var markCount = legendsCount.Count(item => item.Value == mark.Value);
                packet.Write(mark.Icon);
                packet.Write((byte)LegendColorConverter.ColorToInt(mark.Color));
                packet.WriteStringA(mark.Category);
                packet.WriteStringA(mark.Value + $" - {mark.Time.ToShortDateString()} ({markCount})");
            }

            packet.Write((byte) 0x00);
            packet.Write((ushort) Aisling.Gender);
            packet.Write((byte) 0x02);
            packet.Write((uint) 0x00);
            packet.Write((byte) 0x00);
        }
    }
}