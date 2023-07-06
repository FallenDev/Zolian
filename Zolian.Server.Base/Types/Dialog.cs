using Chaos.Common.Identity;
using Darkages.Common;
using Darkages.Models;
using Darkages.Network.Client;

namespace Darkages.Types;

public class Dialog
{
    private List<DialogSequence> Sequences = new();

    public Dialog()
    {
        Serial = EphemeralRandomIdGenerator<int>.Shared.NextId;
    }

    public bool CanMoveBack => SequenceIndex - 1 >= 0;
    public bool CanMoveNext => SequenceIndex + 1 < Sequences.Count;
    public DialogSequence Current => Sequences[SequenceIndex];

    public ushort DisplayImage { get; set; }
    private int SequenceIndex { get; set; }
    private int Serial { get; set; }

    //public DialogSequence Invoke(WorldClient client)
    //{
    //    client.SendDialog(new ServerFormat30(client, this));
    //    {
    //        Current?.OnSequenceStep?.Invoke(client.Aisling, Current);
    //        return Current;
    //    }
    //}

    //public void MoveNext(GameClient client)
    //{
    //    if (CanMoveNext)
    //        SequenceIndex++;

    //    client.DlgSession.Sequence = (ushort)SequenceIndex;
    //}
}