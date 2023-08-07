using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.ScriptingBase;

public abstract class MonsterCreateScript : IScriptBase
{
    public abstract Monster Create();
}