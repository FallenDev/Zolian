﻿using Darkages.Models;

using Newtonsoft.Json;

namespace Darkages.Templates;

public class ServerTemplate : Template
{
    [JsonProperty] public IList<Politics> Politics = new List<Politics>();

    [JsonProperty] public Dictionary<string, int> Variables = new();
}