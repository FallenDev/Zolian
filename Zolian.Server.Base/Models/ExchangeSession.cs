using Darkages.Sprites;

namespace Darkages.Models
{
    public class ExchangeSession
    {
        public ExchangeSession(Aisling user)
        {
            Trader = user;
            Items = new List<Item>();
        }

        public bool Confirmed { get; set; }
        public uint Gold { get; set; }
        public List<Item> Items { get; }
        public Aisling Trader { get; }
        public int Weight { get; set; }
    }
}