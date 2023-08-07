using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;

namespace Darkages.Types;

public class Board
{
    private static readonly string StoragePath = $@"{ServerSetup.Instance.StoragePath}\Community\Boards";

    public List<PostFormat> Posts = new();

    static Board()
    {
        if (!Directory.Exists(StoragePath))
            Directory.CreateDirectory(StoragePath);
    }
    
    public Board(string name, ushort index, bool isMail = false)
    {
        Index = index;
        Subject = name;
        IsMail = isMail;
    }

    public ushort Index { get; set; }
    public bool IsMail { get; set; }
    public string Subject { get; set; }

    public static List<Board> CacheFromStorage(string dir)
    {
        var results = new List<Board>();
        var assetNames = Directory.GetFiles(Path.Combine(StoragePath, dir), "*.json", SearchOption.TopDirectoryOnly);

        if (assetNames.Length == 0) return null;

        foreach (var asset in assetNames)
        {
            var tmp = LoadFromFile(asset);

            if (tmp != null)
                results.Add(tmp);
        }

        return results;
    }

    public static Board LoadFromFile(string path)
    {
        if (!File.Exists(path)) return null;

        using var s = File.OpenRead(path);
        using var f = new StreamReader(s);
        return JsonConvert.DeserializeObject<Board>(f.ReadToEnd(), Settings);
    }

    public void Save(string key)
    {
        var path = Path.Combine(StoragePath, $"{key}\\{Subject}.json");
        var objString = JsonConvert.SerializeObject(this, Settings);
        File.WriteAllText(path, objString);
    }

    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };
}

public class PostFormat
{
    public PostFormat(ushort boardId)
    {
        try
        {
            BoardId = boardId;
        }
        catch (Exception e)
        {
            ServerSetup.Logger("Issue with PostFormat");
            Crashes.TrackError(e);
        }
    }

    public bool HighLighted { get; set; }
    public ushort BoardId { get; set; }
    public DateTime DatePosted { get; init; }
    public string Message { get; init; }
    public string Owner { get; set; }
    public short PostId { get; set; }
    public bool Read { get; set; }
    public string Recipient { get; init; }
    public string Sender { get; init; }
    public string Subject { get; init; }

    public void Associate(string username)
    {
        Owner = username;
    }
}