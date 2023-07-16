using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Sprites;

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

public class ReactorInputSequence
{
    private readonly WorldClient _client;
    private readonly string _captionA;
    private readonly string _captionB;
    private readonly int _inputLength;
    private readonly Mundane _mundane;

    public ReactorInputSequence(WorldClient gameClient, Mundane mundane, string captionA, string captionB, int inputLength = 48)
    {
        _client = gameClient;
        _mundane = mundane;
        _captionA = captionA;
        _captionB = captionB;
        _inputLength = inputLength;
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

public class Pursuit
{
    private readonly WorldClient _client;
    private Dialog Sequence { get; }

    public Pursuit(WorldClient client, Dialog sequenceMenu)
    {
        _client = client;
        Sequence = sequenceMenu;
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