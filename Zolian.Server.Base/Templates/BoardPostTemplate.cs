using System.Collections.Concurrent;
using Chaos.Common.Identity;
using Dapper;

using Darkages.Database;
using Darkages.Network.Client;
using Microsoft.AppCenter.Crashes;

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
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }

    public static void MailFromDatabase(WorldClient client)
    {
        if (client.Aisling.QuestManager.MailBoxNumber == 0)
        {
            client.Aisling.QuestManager.MailBoxNumber = EphemeralRandomIdGenerator<ushort>.Shared.NextId;
            CreateMailBox(client);
        }

        try
        {
            var sConn = new SqlConnection(AislingStorage.PersonalMailString);
            sConn.Open();
            const string sql = "SELECT * FROM ZolianBoardsMail.dbo.Posts";
            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;
            var reader = cmd.ExecuteReader();

            // Clear letters and populate mailbox
            client.Aisling.PersonalLetters.Clear();

            while (reader.Read())
            {
                var boardId = (int)reader["BoardId"];
                if ((ushort)boardId != client.Aisling.QuestManager.MailBoxNumber) continue;
                var postId = (int)reader["PostId"];

                var post = new PostTemplate()
                {
                    PostId = (short)postId,
                    Highlighted = (bool)reader["Highlighted"],
                    DatePosted = (DateTime)reader["DatePosted"],
                    Owner = reader["Owner"].ToString(),
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
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }

    public static void DeletePost(PostTemplate post, ushort boardId)
    {
        var postId = post.PostId;
        var owner = post.Owner;

        try
        {
            var sConn = new SqlConnection(AislingStorage.PersonalMailString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianBoardsMail.dbo.Posts WHERE BoardId = @boardId AND PostId = @postId AND Owner = @owner";
            sConn.Execute(cmd, new { boardId, postId, owner });
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }

    private static void CreateMailBox(WorldClient client)
    {
        try
        {
            var sConn = new SqlConnection(AislingStorage.PersonalMailString);
            sConn.Open();
            // Player Mail
            var playerMailBox =
                "INSERT INTO ZolianBoardsMail.dbo.Boards (BoardId, Serial, Private, IsMail, Name) VALUES " +
                $"('{client.Aisling.QuestManager.MailBoxNumber}','{(long)client.Aisling.Serial}','{1}','{1}','Mail')";

            var cmd4 = new SqlCommand(playerMailBox, sConn);
            var adapter = new SqlDataAdapter();
            cmd4.CommandTimeout = 5;
            adapter.InsertCommand = cmd4;
            adapter.InsertCommand.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}
