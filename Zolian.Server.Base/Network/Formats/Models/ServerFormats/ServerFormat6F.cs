using Darkages.Enums;
using Darkages.Meta;
using Darkages.Network.Client;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat6F : NetworkFormat
{
    public GameClient Client;
    public string Name;
    public byte Type;

    /// <summary>
    /// Metadata Load
    /// </summary>
    public ServerFormat6F()
    {
        Encrypted = true;
        Command = 0x6F;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Type);

        if (Type == 0x00)
            if (Name != null)
            {
                if (!Name.Contains("Class"))
                {
                    writer.Write(MetafileManager.GetMetaFile(Name));
                    return;
                }

                var file = MetafileManager.GetMetaFile(Name);
                var orgFileName = file.Name;
                if (Client?.Aisling == null) return;

                switch (Client.Aisling.Path)
                {
                    case Class.Berserker:
                        file.Name = "SClass1";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                    case Class.Defender:
                        file.Name = "SClass2";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                    case Class.Assassin:
                        file.Name = "SClass3";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                    case Class.Cleric:
                        file.Name = "SClass4";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                    case Class.Arcanus:
                        file.Name = "SClass5";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                    case Class.Monk:
                        file.Name = "SClass6";
                        writer.Write(file);
                        file.Name = orgFileName;
                        break;
                }
            }

        if (Type == 0x01)
            writer.Write(MetafileManager.GetMetaFiles());
    }
}