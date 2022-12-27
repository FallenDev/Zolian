using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Mehadi")]
public class Mehadi : AreaScript
{
    public Mehadi(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(GameClient client)
    {
        if (client == null) return;
        if (client.Aisling.QuestManager.SwampAccess) return;

        const string script = " Shreek";
        var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == script);
        scriptObj.Value?.OnClick(client.Server, client);
        client.TransitionToMap(3071, new Position(3, 7));
    }

    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation)
    {
        if (client == null) return;
        if (client.Aisling.QuestManager.SwampAccess) return;
        if (client.Aisling.Map.ID == 3071) return;
        client.CloseDialog();

        const string script = " Shreek";
        var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == script);
        scriptObj.Value?.OnClick(client.Server, client);
        client.TransitionToMap(3071, new Position(3, 7));
    }
}