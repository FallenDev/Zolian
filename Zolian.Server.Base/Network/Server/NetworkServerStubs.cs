using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ClientFormats;

namespace Darkages.Network.Server;

public abstract partial class NetworkServer<TClient>
{
    protected virtual void Format00Handler(LoginClient client, ClientFormat00 format) { }
    protected virtual void Format02Handler(TClient client, ClientFormat02 format) { }
    protected virtual void Format03Handler(TClient client, ClientFormat03 format) { }
    protected virtual void Format04Handler(TClient client, ClientFormat04 format) { }
    protected virtual void Format05Handler(TClient client, ClientFormat05 format) { }
    protected virtual void Format06Handler(TClient client, ClientFormat06 format) { }
    protected virtual void Format07Handler(TClient client, ClientFormat07 format) { }
    protected virtual void Format08Handler(TClient client, ClientFormat08 format) { }
    protected virtual void Format0BHandler(TClient client, ClientFormat0B format) { }
    protected virtual void Format0CHandler(TClient client, ClientFormat0C format) { }
    protected virtual void Format0DHandler(TClient client, ClientFormat0D format) { }
    protected virtual void Format0EHandler(TClient client, ClientFormat0E format) { }
    protected virtual void Format0FHandler(TClient client, ClientFormat0F format) { }
    protected abstract Task Format10Handler(TClient client, ClientFormat10 format);
    protected virtual void Format11Handler(TClient client, ClientFormat11 format) { }
    protected virtual void Format13Handler(TClient client, ClientFormat13 format) { }
    protected virtual void Format18Handler(TClient client, ClientFormat18 format) { }
    protected virtual void Format19Handler(TClient client, ClientFormat19 format) { }
    protected virtual void Format1BHandler(TClient client, ClientFormat1B format) { }
    protected virtual void Format1CHandler(TClient client, ClientFormat1C format) { }
    protected virtual void Format1DHandler(TClient client, ClientFormat1D format) { }
    protected virtual void Format24Handler(TClient client, ClientFormat24 format) { }
    protected virtual void Format26Handler(TClient client, ClientFormat26 format) { }
    protected virtual void Format29Handler(TClient client, ClientFormat29 format) { }
    protected virtual void Format2AHandler(TClient client, ClientFormat2A format) { }
    protected virtual void Format2DHandler(TClient client, ClientFormat2D format) { }
    protected virtual void Format2EHandler(TClient client, ClientFormat2E format) { }
    protected virtual void Format2FHandler(TClient client, ClientFormat2F format) { }
    protected virtual void Format30Handler(TClient client, ClientFormat30 format) { }
    protected virtual void Format32Handler(TClient client, ClientFormat32 format) { }
    protected virtual void Format38Handler(TClient client, ClientFormat38 format) { }
    protected virtual void Format39Handler(TClient client, ClientFormat39 format) { }
    protected virtual void Format3AHandler(TClient client, ClientFormat3A format) { }
    protected virtual void Format3BHandler(TClient client, ClientFormat3B format) { }
    protected virtual void Format3EHandler(TClient client, ClientFormat3E format) { }
    protected virtual void Format3FHandler(TClient client, ClientFormat3F format) { }
    protected virtual void Format43Handler(TClient client, ClientFormat43 format) { }
    protected virtual void Format44Handler(TClient client, ClientFormat44 format) { }
    protected virtual void Format45Handler(TClient client, ClientFormat45 format) { }
    protected virtual void Format47Handler(TClient client, ClientFormat47 format) { }
    protected virtual void Format4AHandler(TClient client, ClientFormat4A format) { }
    protected virtual void Format4BHandler(TClient client, ClientFormat4B format) { }
    protected virtual void Format4DHandler(TClient client, ClientFormat4D format) { }
    protected virtual void Format4EHandler(TClient client, ClientFormat4E format) { }
    protected virtual void Format4FHandler(TClient client, ClientFormat4F format) { }
    protected virtual void Format57Handler(TClient client, ClientFormat57 format) { }
    protected virtual void Format62Handler(TClient client, ClientFormat62 format) { }
    protected virtual void Format68Handler(TClient client, ClientFormat68 format) { }
    protected virtual void Format75Handler(TClient client, ClientFormat75 format) { }
    protected virtual void Format79Handler(TClient client, ClientFormat79 format) { }
    protected virtual void Format7BHandler(TClient client, ClientFormat7B format) { }
    protected virtual void Format89Handler(TClient client, ClientFormat89 format) { }
}