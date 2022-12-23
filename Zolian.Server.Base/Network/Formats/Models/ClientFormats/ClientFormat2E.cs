namespace Darkages.Network.Formats.Models.ClientFormats
{
    public class ClientFormat2E : NetworkFormat
    {
        /// <summary>
        /// Request Party Join
        /// </summary>
        public ClientFormat2E()
        {
            Encrypted = true;
            Command = 0x2E;
        }

        public string Name { get; private set; }
        private bool ShowOnMap { get; set; }
        public byte Type { get; private set; }

        public override void Serialize(NetworkPacketReader reader)
        {
            Type = reader.ReadByte();

            switch (Type)
            {
                case 0x02:
                    Name = reader.ReadStringA();
                    break;
                case 0x08:
                    Name = reader.ReadStringA();
                    ShowOnMap = reader.ReadBool();
                    break;
            }
        }

        public override void Serialize(NetworkPacketWriter writer) { }
    }
}