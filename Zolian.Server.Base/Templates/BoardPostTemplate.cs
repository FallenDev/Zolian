using System.Collections.Concurrent;
using Chaos.Common.Identity;
using Dapper;

using Darkages.Database;
using Darkages.Network.Client;
using Microsoft.Data.SqlClient;

namespace Darkages.Templates;

public class BoardTemplate : Template
{
    public ushort BoardId { get; set; }
    public long Serial { get; set; }
    public bool Private { get; set; }
    public bool IsMail { get; set; }
    public ConcurrentDictionary<short, PostTemplate> Posts { get; set; } = new();
}

public class PostTemplate
{
    public short PostId { get; set; }
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
                var boardId = (int)reader["BoardId"];
                var temp = new BoardTemplate
                {
                    BoardId = (ushort)boardId,
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
                var boardId = (int)reader2["BoardId"];
                var postId = (int)reader2["PostId"];
                var post = new PostTemplate()
                {
                    PostId = (short)postId,
                    Highlighted = (bool)reader2["Highlighted"],
                    DatePosted = (DateTime)reader2["DatePosted"],
                    Owner = reader2["Owner"].ToString(),
                    Sender = reader2["Sender"].ToString(),
                    ReadPost = (bool)reader2["ReadPost"],
                    SubjectLine = reader2["SubjectLine"].ToString(),
                    Message = reader2["Message"].ToString()
                };

                var boardFetched = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue((ushort)boardId, out var board);

                if (boardFetched)
                    board.Posts.TryAdd(post.PostId, post);
            }

            reader2.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void MailFromDatabase(WorldClient client)
    {
        // ToDo: Do not need this logic once everyone has a defined mailbox
        if (client.Aisling.QuestManager.MailBoxNumber == 0)
        {
            client.Aisling.QuestManager.MailBoxNumber = EphemeralRandomIdGenerator<ushort>.Shared.NextId;
        }

        try
        {
            var sConn = new SqlConnection(AislingStorage.PersonalMailString);
            sConn.Open();
            var sql = $"SELECT * FROM ZolianBoardsMail.dbo.Posts WHERE BoardId = {client.Aisling.QuestManager.MailBoxNumber}";
            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;
            var reader = cmd.ExecuteReader();

            // Clear letters and populate mailbox
            client.Aisling.PersonalLetters.Clear();

            while (reader.Read())
            {
                // Check ownership, just in-case two players end up with the same mailbox number
                var ownerCheck = reader["Owner"].ToString();
                if (!string.Equals(ownerCheck, client.Aisling.Username, StringComparison.InvariantCultureIgnoreCase)) continue;

                var postId = (int)reader["PostId"];
                var post = new PostTemplate()
                {
                    PostId = (short)postId,
                    Highlighted = (bool)reader["Highlighted"],
                    DatePosted = (DateTime)reader["DatePosted"],
                    Owner = ownerCheck,
                    Sender = reader["Sender"].ToString(),
                    ReadPost = (bool)reader["ReadPost"],
                    SubjectLine = reader["SubjectLine"].ToString(),
                    Message = reader["Message"].ToString()
                };

                client.Aisling.PersonalLetters.TryAdd(post.PostId, post);
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }

    public static void DeletePost(PostTemplate post, ushort id)
    {
        var postId = post.PostId;
        var boardId = (int)id;
        var owner = post.Owner;

        try
        {
            var sConn = new SqlConnection(AislingStorage.PersonalMailString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianBoardsMail.dbo.Posts WHERE BoardId = @BoardId AND PostId = @PostId AND Owner = @Owner";
            sConn.Execute(cmd, new { boardId, postId, owner });
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            SentrySdk.CaptureException(e);
        }
    }
}
