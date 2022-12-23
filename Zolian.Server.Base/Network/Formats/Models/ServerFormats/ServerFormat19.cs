using Darkages.Network.Client;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat19 : NetworkFormat
    {
        private readonly byte _number;
        private readonly GameClient _gameClient;

        /// <summary>
        /// Play Sound
        /// </summary>
        public ServerFormat19(byte number) : this() => _number = number;

        /// <summary>
        /// Send Music
        /// </summary>
        public ServerFormat19(GameClient client, byte number) : this()
        {
            _gameClient = client;
            _number = number;
        }

        private ServerFormat19()
        {
            Encrypted = true;
            Command = 0x19;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (_gameClient != null)
            {
                writer.Write((byte)0xFF);
            }

            writer.Write(_number);
        }
    }
}