using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Scripting
{
    public abstract class MonsterScript : ObjectManager, IScriptBase
    {
        protected readonly Area Map;
        protected readonly Monster Monster;

        protected MonsterScript(Monster monster, Area map)
        {
            Monster = monster;
            Map = map;
        }

        public abstract void Update(TimeSpan elapsedTime);
        public abstract void OnClick(GameClient client);
        public abstract void OnDeath(GameClient client = null);

        public virtual void MonsterState(TimeSpan elapsedTime) { }
        public virtual void OnApproach(GameClient client) { }
        public virtual void OnLeave(GameClient client) { }
        public virtual bool OnGossip(GameClient client) => false;
        public virtual bool OnDispelled(GameClient client) => false;
        public virtual void OnSkulled(GameClient client) { }
        public virtual void OnDamaged(GameClient client, long dmg, Sprite source) { }
        public virtual void OnItemDropped(GameClient client, Item item) { }
        public virtual void OnGoldDropped(GameClient client, uint gold) { }
    }
}