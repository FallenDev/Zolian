using Darkages.Types;

namespace Darkages.Models
{
    public class CastInfo
    {
        public byte Slot;
        public byte SpellLines;
        public DateTime Started;

        public string Data { get; init; }
        public Position Position { get; set; }
        public uint Target { get; init; }
    }
}