using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Scripting;

public abstract class MonsterCreateScript : IScriptBase
{
    public abstract Monster Create();
}