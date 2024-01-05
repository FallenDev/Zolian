using System.Collections.Concurrent;

using Microsoft.AppCenter.Crashes;

using Microsoft.Data.SqlClient;

namespace Darkages.Templates;

public class BoardTemplate : Template
{
    public long BoardId { get; set; }
    public long Serial { get; set; }
    public bool Private { get; set; }
    public bool IsMail { get; set; }
    public ConcurrentDictionary<long, PostTemplate> Posts { get; set; } = new();
}

public class PostTemplate
{
    public long PostId { get; set; }
    public bool Highlighted { get; set; }
    public DateTime DatePosted { get; set; }
    public string Owner { get; set; }
    public string Sender { get; set; }
    public bool ReadPost { get; set; }
    public string SubjectLine { get; set; }
    public string Message { get; set; }
}

public static class BoardPostStorage
{
    public static void CacheFromDatabase(string conn)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM ZolianBoardsMail.dbo.Boards";
            const string sql2 = "SELECT * FROM ZolianBoardsMail.dbo.Posts";
            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;
            var cmd2 = new SqlCommand(sql2, sConn);
            cmd2.CommandTimeout = 5;
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var temp = new BoardTemplate
                {
                    BoardId = (long)reader["BoardId"],
                    Serial = (long)reader["Serial"],
                    Private = (bool)reader["Private"],
                    IsMail = (bool)reader["IsMail"],
                    Name = reader["Name"].ToString()
                };

                ServerSetup.Instance.GlobalBoardPostCache.TryAdd(temp.BoardId, temp);
            }

            reader.Close();
            var reader2 = cmd2.ExecuteReader();

            while (reader2.Read())
            {
                var boardId = (long)reader2["BoardId"];
                var post = new PostTemplate()
                {
                    PostId = (long)reader2["PostId"],
                    Highlighted = (bool)reader2["Highlighted"],
                    DatePosted = (DateTime)reader2["DatePosted"],
                    Owner = reader2["Owner"].ToString(),
                    Sender = reader2["Sender"].ToString(),
                    ReadPost = (bool)reader2["ReadPost"],
                    SubjectLine = reader2["SubjectLine"].ToString(),
                    Message = reader2["Message"].ToString()
                };

                var boardFetched = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue(boardId, out var board);

                if (boardFetched)
                    board.Posts.TryAdd(post.PostId, post);
            }

            reader2.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}
