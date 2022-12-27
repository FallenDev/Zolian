using Newtonsoft.Json;
using Darkages.Compression;
using ServiceStack;
using Darkages.Network.Security;
using Darkages.Models;

namespace Darkages.Meta;

public class Node
{
    public List<string> Atoms { get; set; }
    public string Name { get; set; }
}

public class MetafileManager
{
    protected static readonly MetafileCollection Metafiles;

    static MetafileManager()
    {
        var filePath = Path.Combine(ServerSetup.Instance.StoragePath, "metafile");

        if (!Directory.Exists(filePath)) return;

        var files = Directory.GetFiles(filePath);

        Metafiles = new MetafileCollection(short.MaxValue);

        foreach (var file in files)
        {
            var metaFile = CompressableObject.Load<Metafile>(file);

            if (metaFile.Name.StartsWith("SEvent")) continue;

            if (metaFile.Name.StartsWith("SClass")) continue;

            if (metaFile.Name.StartsWith("ItemInfo")) continue;

            Metafiles.Add(metaFile);
        }

        CreateFromTemplates();
        LoadQuestDescriptions();
    }

    private static void LoadQuestDescriptions()
    {
        var metaFile1 = new Metafile { Name = "SEvent1", Nodes = new List<MetafileNode>() };
        var metaFileLocation1 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle1";
        var metaFile2 = new Metafile { Name = "SEvent2", Nodes = new List<MetafileNode>() };
        var metaFileLocation2 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle2";
        var metaFile3 = new Metafile { Name = "SEvent3", Nodes = new List<MetafileNode>() };
        var metaFileLocation3 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle3";
        var metaFile4 = new Metafile { Name = "SEvent4", Nodes = new List<MetafileNode>() };
        var metaFileLocation4 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle4";
        var metaFile5 = new Metafile { Name = "SEvent5", Nodes = new List<MetafileNode>() };
        var metaFileLocation5 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle5";
        var metaFile6 = new Metafile { Name = "SEvent6", Nodes = new List<MetafileNode>() };
        var metaFileLocation6 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle6";
        var metaFile7 = new Metafile { Name = "SEvent7", Nodes = new List<MetafileNode>() };
        var metaFileLocation7 = ServerSetup.Instance.StoragePath + "\\Quests\\Circle7";

        LoadCircleQuestDescriptions(metaFileLocation1, metaFile1);
        LoadCircleQuestDescriptions(metaFileLocation2, metaFile2);
        LoadCircleQuestDescriptions(metaFileLocation3, metaFile3);
        LoadCircleQuestDescriptions(metaFileLocation4, metaFile4);
        LoadCircleQuestDescriptions(metaFileLocation5, metaFile5);
        LoadCircleQuestDescriptions(metaFileLocation6, metaFile6);
        LoadCircleQuestDescriptions(metaFileLocation7, metaFile7);
    }

    private static void LoadCircleQuestDescriptions(string dir, Metafile metaFile)
    {
        if (!Directory.Exists(dir)) return;
        var loadedNodes = new List<Node>();

        foreach (var file in Directory.GetFiles(dir, "*.txt"))
        {
            var contents = File.ReadAllText(file);
            if (string.IsNullOrEmpty(contents)) continue;
            var nodes = JsonConvert.DeserializeObject<List<Node>>(contents);
            if (nodes == null) continue;
            if (nodes.Count > 0)
                loadedNodes.AddRange(nodes);
        }

        var count = 0;
        foreach (var node in loadedNodes)
        {
            count++;
            var padZero = count.ToString().PadLeft(2, '0');
            var metafileNode = new MetafileNode($"{padZero}_{node.Name}", node.Atoms.ToArray());
            metaFile.Nodes.Add(metafileNode);
        }

        CompileTemplate(metaFile);
        Metafiles.Add(metaFile);
    }

    public static Metafile GetMetaFile(string name) => Metafiles.Find(o => o.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static MetafileCollection GetMetaFiles() => Metafiles;

    protected static void CompileTemplate(Metafile metaFile)
    {
        using (var stream = new MemoryStream())
        {
            metaFile.Save(stream);
            metaFile.InflatedData = stream.ToArray();
        }

        metaFile.Hash = Crc32Provider.Generate32(metaFile.InflatedData);
        metaFile.Compress();
    }

    // Called in ServerSetup.cs
    public static void DecompileTemplates()
    {
        var metaFile = new Metafile();
        var filePath = "Z:\\Zolian\\Data\\ServerData\\metafile";

        if (!Directory.Exists(filePath)) return;

        var files = Directory.GetFiles(filePath);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            metaFile.DeflatedData = File.ReadAllBytes(file);
            metaFile.Hash = Crc32Provider.Generate32(metaFile.DeflatedData);
            metaFile.Name = file;
            metaFile.Decompress();
            var fs = File.Open($"Z:\\Zolian\\Metafiles\\{fileName}", FileMode.Create);
            fs.Write(metaFile.InflatedData);
            fs.Close();
            fs.Dispose();
        }
    }

    private static void CreateFromTemplates()
    {
        GenerateItemInfoMeta();

        HumanMeta.GenerateHumanBezerkerMeta();
        HumanMeta.GenerateHumanDefenderMeta();
        HumanMeta.GenerateHumanAssassinMeta();
        HumanMeta.GenerateHumanClericMeta();
        HumanMeta.GenerateHumanArcanusMeta();
        HumanMeta.GenerateHumanMonkMeta();

        HalfElfMeta.GenerateHalfElfBezerkerMeta();
        HalfElfMeta.GenerateHalfElfDefenderMeta();
        HalfElfMeta.GenerateHalfElfAssassinMeta();
        HalfElfMeta.GenerateHalfElfClericMeta();
        HalfElfMeta.GenerateHalfElfArcanusMeta();
        HalfElfMeta.GenerateHalfElfMonkMeta();

        HighElfMeta.GenerateHighElfBezerkerMeta();
        HighElfMeta.GenerateHighElfDefenderMeta();
        HighElfMeta.GenerateHighElfAssassinMeta();
        HighElfMeta.GenerateHighElfClericMeta();
        HighElfMeta.GenerateHighElfArcanusMeta();
        HighElfMeta.GenerateHighElfMonkMeta();

        DarkElfMeta.GenerateDarkElfBezerkerMeta();
        DarkElfMeta.GenerateDarkElfDefenderMeta();
        DarkElfMeta.GenerateDarkElfAssassinMeta();
        DarkElfMeta.GenerateDarkElfClericMeta();
        DarkElfMeta.GenerateDarkElfArcanusMeta();
        DarkElfMeta.GenerateDarkElfMonkMeta();

        WoodElfMeta.GenerateWoodElfBezerkerMeta();
        WoodElfMeta.GenerateWoodElfDefenderMeta();
        WoodElfMeta.GenerateWoodElfAssassinMeta();
        WoodElfMeta.GenerateWoodElfClericMeta();
        WoodElfMeta.GenerateWoodElfArcanusMeta();
        WoodElfMeta.GenerateWoodElfMonkMeta();

        OrcMeta.GenerateOrcBezerkerMeta();
        OrcMeta.GenerateOrcDefenderMeta();
        OrcMeta.GenerateOrcAssassinMeta();
        OrcMeta.GenerateOrcClericMeta();
        OrcMeta.GenerateOrcArcanusMeta();
        OrcMeta.GenerateOrcMonkMeta();

        DwarfMeta.GenerateDwarfBezerkerMeta();
        DwarfMeta.GenerateDwarfDefenderMeta();
        DwarfMeta.GenerateDwarfAssassinMeta();
        DwarfMeta.GenerateDwarfClericMeta();
        DwarfMeta.GenerateDwarfArcanusMeta();
        DwarfMeta.GenerateDwarfMonkMeta();

        HalflingMeta.GenerateHalflingBezerkerMeta();
        HalflingMeta.GenerateHalflingDefenderMeta();
        HalflingMeta.GenerateHalflingAssassinMeta();
        HalflingMeta.GenerateHalflingClericMeta();
        HalflingMeta.GenerateHalflingArcanusMeta();
        HalflingMeta.GenerateHalflingMonkMeta();

        DragonkinMeta.GenerateDragonkinBezerkerMeta();
        DragonkinMeta.GenerateDragonkinDefenderMeta();
        DragonkinMeta.GenerateDragonkinAssassinMeta();
        DragonkinMeta.GenerateDragonkinClericMeta();
        DragonkinMeta.GenerateDragonkinArcanusMeta();
        DragonkinMeta.GenerateDragonkinMonkMeta();

        HalfBeastMeta.GenerateHalfBeastBezerkerMeta();
        HalfBeastMeta.GenerateHalfBeastDefenderMeta();
        HalfBeastMeta.GenerateHalfBeastAssassinMeta();
        HalfBeastMeta.GenerateHalfBeastClericMeta();
        HalfBeastMeta.GenerateHalfBeastArcanusMeta();
        HalfBeastMeta.GenerateHalfBeastMonkMeta();

        MerfolkMeta.GenerateMerfolkBezerkerMeta();
        MerfolkMeta.GenerateMerfolkDefenderMeta();
        MerfolkMeta.GenerateMerfolkAssassinMeta();
        MerfolkMeta.GenerateMerfolkClericMeta();
        MerfolkMeta.GenerateMerfolkArcanusMeta();
        MerfolkMeta.GenerateMerfolkMonkMeta();
    }

    private static void GenerateItemInfoMeta()
    {
        var i = 0;
        foreach (var batch in ServerSetup.Instance.GlobalItemTemplateCache.OrderBy(v => v.Value.LevelRequired)
                     .BatchesOf(712))
        {
            var metaFile = new Metafile { Name = $"ItemInfo{i}", Nodes = new List<MetafileNode>() };

            foreach (var template in from v in batch select v.Value)
            {
                //if (template.Gender == 0)
                //    continue;

                var meta = template.GetMetaData();
                metaFile.Nodes.Add(new MetafileNode(template.Name, meta));
            }

            CompileTemplate(metaFile);
            Metafiles.Add(metaFile);
            i++;
        }
    }
}