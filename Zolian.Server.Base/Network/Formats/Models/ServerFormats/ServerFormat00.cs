using Darkages.Network.Security;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat00 : NetworkFormat
    {
        /// <summary>
        /// CryptoKey
        /// </summary>
        public ServerFormat00()
        {
            Encrypted = false;
            Command = 0x00;
        }

        public uint Hash { get; init; }
        public SecurityParameters Parameters { get; init; }
        public byte Type { get; init; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);
            writer.Write(Hash);
            writer.Write(Parameters);
        }
    }
}