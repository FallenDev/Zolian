using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;
using Darkages.Models;
using Darkages.Network.Client;

namespace Darkages.Types;

public class ReactorSequence
{
    private readonly WorldClient _client;
    private readonly DialogSequence _sequence;

    public ReactorSequence(WorldClient gameClient, DialogSequence sequenceMenu)
    {
        _client = gameClient;
        _sequence = sequenceMenu;
    }

    public void Send()
    {
        var args = new DialogArgs
        {
            Color = DisplayColor.Default,
            DialogId = 0,
            DialogType = DialogType.Normal,
            EntityType = EntityType.Creature,
            HasNextButton = _sequence.CanMoveNext,
            HasPreviousButton = _sequence.CanMoveBack,
            Name = _sequence.Title,
            Options = null,
            PursuitId = ushort.MaxValue,
            SourceId = (uint)_sequence.Id,
            Sprite = _sequence.DisplayImage,
            Text = _sequence.DisplayText,
            TextBoxLength = 0
        };

        _client.Send(args);
    }
}