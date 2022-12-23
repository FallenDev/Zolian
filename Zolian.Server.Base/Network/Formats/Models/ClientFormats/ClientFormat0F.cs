using Darkages.Types;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat0F : NetworkFormat
    {
        /// <summary>
        /// Spell Use
        /// </summary>
        public ClientFormat0F()
        {
            Encrypted = true;
            Command = 0x0F;
        }

        public string Data { get; private set; }
        public byte Index { get; private set; }
        public Position Point { get; private set; }
        public uint Serial { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            var data = CheckData(reader);

            reader.Position = 0;
            Index = reader.ReadByte();

            try
            {
                if (reader.GetCanRead())
                    Serial = reader.ReadUInt32();

                if (reader.Position + 4 < reader.Packet.Data.Length)
                    Point = reader.ReadPosition();
            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                ServerSetup.Logger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
                Crashes.TrackError(ex);
            }
            finally
            {
                Data = data.Trim('\0');
            }
        }

        public override void Serialize(NetworkPacketWriter writer) { }

        private string CheckData(NetworkPacketReader reader)
        {
            Index = reader.ReadByte();

            var data = string.Empty;
            char character;

            do
            {
                character = Convert.ToChar(reader.ReadByte());
                data += new string(character, 1);
            } while (character != char.Parse("\0"));

            return data;
        }
    }
}