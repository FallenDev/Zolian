using Darkages.Types;
using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Sprites;

namespace Darkages.Scripting
{
    public abstract class AreaScript : IScriptBase
    {
        protected Area Area;
        public GameServerTimer Timer { get; set; }

        protected AreaScript(Area area) => Area = area;
        public abstract void Update(TimeSpan elapsedTime);
        
        public virtual void OnMapEnter(GameClient client) { }
        public virtual void OnMapExit(GameClient client) { }
        public virtual void OnMapClick(GameClient client, int x, int y) { }
        public virtual void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }
        public virtual void OnItemDropped(GameClient client, Item item, Position location) { }
        public virtual void OnGossip(GameClient client, string message) { }
    }
}