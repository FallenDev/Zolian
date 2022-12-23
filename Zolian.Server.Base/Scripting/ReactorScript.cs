using Darkages.Interfaces;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Scripting
{
    public abstract class ReactorScript : IScriptBase
    {
        protected ReactorScript(ReactorTemplate reactor) => Reactor = reactor;

        public ReactorTemplate Reactor { get; }

        public abstract void OnBack(Aisling aisling);
        public abstract void OnClose(Aisling aisling);
        public abstract void OnNext(Aisling aisling);
        public abstract void OnTriggered(Aisling aisling);
    }
}