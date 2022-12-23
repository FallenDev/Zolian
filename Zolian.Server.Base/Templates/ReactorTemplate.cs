﻿using System.Collections.Concurrent;
using Darkages.Common;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

using Newtonsoft.Json;

namespace Darkages.Templates
{
    [Serializable]
    public class ReactorTemplate : Template
    {
        [JsonIgnore] public readonly int Id;

        public List<DialogSequence> Sequences = new();

        public ReactorTemplate()
        {
            Id = Generator.GenerateNumber();
        }

        public string CallBackScriptKey { get; set; }
        public string CallingReactor { get; set; }
        public bool CanActAgain { get; set; }
        [JsonIgnore] public DialogSequence Current => Sequences[Index];
        [JsonIgnore] public ConcurrentDictionary<string, ReactorScript> Decorators { get; set; }
        [JsonIgnore] public int Index { get; set; }
        public Position Location { get; set; }
        public string ScriptKey { get; set; }

        public void Goto(GameClient client, int Idx)
        {
            client.Aisling.ActiveReactor.Index = Idx;
            client.Aisling.ActiveSequence = client.Aisling.ActiveReactor.Sequences[Idx];

            client.Send(new ReactorSequence(client, client.Aisling.ActiveSequence));

            if (Sequences[Idx].OnSequenceStep != null)
                Sequences[Idx].OnSequenceStep.Invoke(client.Aisling, Sequences[Idx]);
        }

        public void Next(GameClient client, bool start = false)
        {
            if (Sequences.Count == 0)
                return;

            if (Index < 0)
                Index = 0;

            if (!start)
            {
                if (client.Aisling.ActiveSequence != null)
                {
                    var mundane = GetObject<Mundane>(client.Aisling.Map,
                        i => i.WithinRangeOf(client.Aisling) && i.Alive);

                    if (client.Aisling.ActiveSequence.HasOptions && client.Aisling.ActiveSequence.Options.Length > 0)
                    {
                        if (mundane != null)
                            client.SendOptionsDialog(mundane,
                                client.Aisling.ActiveSequence.DisplayText,
                                client.Aisling.ActiveSequence.Options);
                    }
                    else if (client.Aisling.ActiveSequence.IsCheckPoint)
                    {
                        var results = new List<bool>();
                        var valid = false;

                        if (valid)
                            Goto(client, Index);
                        else
                            client.SendOptionsDialog(mundane, client.Aisling.ActiveSequence.ConditionFailMessage,
                                "failed");
                    }
                    else
                    {
                        Goto(client, Index);
                    }

                    if (Sequences[Index].OnSequenceStep != null)
                        Sequences[Index].OnSequenceStep.Invoke(client.Aisling, Sequences[Index]);
                }

                return;
            }

            var first = Sequences[Index = 0];
            if (first != null) client.Send(new ReactorSequence(client, first));
        }

        public void Update(GameClient client)
        {
            if (client.Aisling.CanReact)
            {
                client.Aisling.CanReact = false;

                if (Decorators != null)
                    foreach (var script in Decorators.Values)
                        script?.OnTriggered(client.Aisling);
            }
        }
    }
}