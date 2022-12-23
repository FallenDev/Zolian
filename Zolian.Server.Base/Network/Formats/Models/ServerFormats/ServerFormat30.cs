using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ReactorInputSequence : ServerFormat30
    {
        private readonly string _captionA;
        private readonly string _captionB;
        private readonly int _inputLength;
        private readonly Mundane _mundane;

        public ReactorInputSequence(Mundane mundane, string captionA, string captionB, int inputLength = 48)
        {
            _mundane = mundane;
            _captionA = captionA;
            _captionB = captionB;
            _inputLength = inputLength;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write((byte)0x04);
            writer.Write((byte)0x01);
            writer.Write((uint)_mundane.Serial);
            writer.Write((byte)0x00);
            writer.Write(_mundane.Template.Image);
            writer.Write((byte)0x05);
            writer.Write((byte)0x05);
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
            writer.Write(ushort.MinValue);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.WriteStringA(_mundane.Template.Name);
            writer.WriteStringB(_captionA);
            writer.WriteStringA(_captionB);
            writer.Write((byte)_inputLength);
        }
    }

    public class ReactorSequence : ServerFormat30
    {
        private readonly GameClient _client;
        private readonly DialogSequence _sequence;

        public ReactorSequence(GameClient gameClient, DialogSequence sequenceMenu) : base(gameClient)
        {
            _client = gameClient;
            _sequence = sequenceMenu;
        }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (!_client.Aisling.LoggedIn) return;

            writer.Write((byte)0x00);
            writer.Write((byte)0x01);
            writer.Write((uint)_sequence.Id);
            writer.Write((byte)0x00);
            writer.Write(_sequence.DisplayImage);
            writer.Write((byte)0x00);
            writer.Write((byte)0x01);
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
            writer.Write(ushort.MaxValue);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write(_sequence.CanMoveBack);
            writer.Write(_sequence.CanMoveNext);
            writer.Write((byte)0);
            writer.WriteStringA(_sequence.Title);
            writer.WriteStringB(_sequence.DisplayText);
        }
    }

    public class ServerFormat30 : NetworkFormat
    {
        private readonly GameClient _client;

        public ServerFormat30()
        {
            Encrypted = true;
            Command = 0x30;
        }

        protected ServerFormat30(GameClient gameClient) : this() => _client = gameClient;

        /// <summary>
        /// Pursuit
        /// </summary>
        /// <param name="gameClient"></param>
        /// <param name="sequenceMenu"></param>
        public ServerFormat30(GameClient gameClient, Dialog sequenceMenu) : this(gameClient) => Sequence = sequenceMenu;

        private Dialog Sequence { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (Sequence == null)
                writer.Write((byte)10);
            else
            {
                writer.Write((byte)0x00);
                writer.Write((byte)0x01);
                writer.Write((uint)_client.DlgSession.Serial);
                writer.Write((byte)0x00);
                writer.Write(Sequence.DisplayImage);
                writer.Write((byte)0x00);
                writer.Write((byte)0x01);
                writer.Write(ushort.MinValue);
                writer.Write((byte)0x00);
                writer.Write(ushort.MinValue);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write(Sequence.CanMoveBack);
                writer.Write(Sequence.CanMoveNext);
                writer.Write((byte)0);
                writer.WriteStringA(Sequence.Current.Title);
                writer.WriteStringB(Sequence.Current.DisplayText);
            }
        }
    }
}