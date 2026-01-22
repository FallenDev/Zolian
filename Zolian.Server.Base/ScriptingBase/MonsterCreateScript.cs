using Darkages.Models;
using Darkages.Sprites.Entity;

namespace Darkages.ScriptingBase;

public abstract class MonsterCreateScript : MonsterRaceModel
{
    public abstract Monster Create();
}