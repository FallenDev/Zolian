using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;

using ServiceStack;

namespace Darkages.Templates;

public class MundaneTemplate : Template
{
    public MundaneTemplate()
    {
        Speech = new List<string>();
    }

    public ushort Image { get; set; }
    public string ScriptKey { get; set; }
    public bool EnableWalking { get; set; }
    public bool EnableTurning { get; set; }
    public bool EnableSpeech { get; set; }
    public List<string> Speech { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public int AreaID { get; set; }
    public byte Direction { get; set; }
    public List<Position> Waypoints { get; set; }
    public PathQualifer PathQualifer { get; set; }

    public WorldServerTimer AttackTimer { get; set; }
    public WorldServerTimer SpellTimer { get; set; }
    public WorldServerTimer ChatTimer { get; set; }
    public WorldServerTimer TurnTimer { get; set; }
    public WorldServerTimer WalkTimer { get; set; }

    public int CastRate { get; set; }
    public int ChatRate { get; set; }
    public int TurnRate { get; set; }
    public int WalkRate { get; set; }

    public List<string> Skills { get; set; }
    public List<string> Spells { get; set; }
    public List<string> DefaultMerchantStock { get; set; } = new();
}

public static class MundaneStorage
{
    public static void CacheFromDatabase(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM ZolianMundanes.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var temp = new MundaneTemplate();
                var image = (int)reader["Image"];
                var speech1 = reader["SpeechOne"].ToString();
                var speech2 = reader["SpeechTwo"].ToString();
                var speech3 = reader["SpeechThree"].ToString();
                var speech4 = reader["SpeechFour"].ToString();
                var speech5 = reader["SpeechFive"].ToString();
                var x = (int)reader["X"];
                var y = (int)reader["Y"];
                var direction = (int)reader["Direction"];
                var pathFind = reader["PathFinder"].ConvertTo<PathQualifer>();
                var way1 = WayPointConvert(reader["WayPointOne"].ToString());
                var way2 = WayPointConvert(reader["WayPointTwo"].ToString());
                var way3 = WayPointConvert(reader["WayPointThree"].ToString());
                var way4 = WayPointConvert(reader["WayPointFour"].ToString());
                var way5 = WayPointConvert(reader["WayPointFive"].ToString());
                var stock1 = reader["DefaultStock1"].ToString();
                var stock2 = reader["DefaultStock2"].ToString();
                var stock3 = reader["DefaultStock3"].ToString();
                var stock4 = reader["DefaultStock4"].ToString();
                var stock5 = reader["DefaultStock5"].ToString();
                var stock6 = reader["DefaultStock6"].ToString();
                var stock7 = reader["DefaultStock7"].ToString();
                var stock8 = reader["DefaultStock8"].ToString();
                var stock9 = reader["DefaultStock9"].ToString();
                var stock10 = reader["DefaultStock10"].ToString();
                var stock11 = reader["DefaultStock11"].ToString();
                var stock12 = reader["DefaultStock12"].ToString();
                var stock13 = reader["DefaultStock13"].ToString();
                var stock14 = reader["DefaultStock14"].ToString();
                var stock15 = reader["DefaultStock15"].ToString();
                var stock16 = reader["DefaultStock16"].ToString();
                var stock17 = reader["DefaultStock17"].ToString();
                var stock18 = reader["DefaultStock18"].ToString();
                var stock19 = reader["DefaultStock19"].ToString();
                var stock20 = reader["DefaultStock20"].ToString();
                var stock21 = reader["DefaultStock21"].ToString();
                var stock22 = reader["DefaultStock22"].ToString();
                var stock23 = reader["DefaultStock23"].ToString();
                var stock24 = reader["DefaultStock24"].ToString();
                var stock25 = reader["DefaultStock25"].ToString();
                var stock26 = reader["DefaultStock26"].ToString();
                var stock27 = reader["DefaultStock27"].ToString();
                var stock28 = reader["DefaultStock28"].ToString();
                var stock29 = reader["DefaultStock29"].ToString();
                var stock30 = reader["DefaultStock30"].ToString();


                temp.Image = (ushort)image;
                temp.ScriptKey = reader["ScriptKey"].ToString();
                temp.EnableWalking = (bool)reader["EnableWalking"];
                temp.EnableTurning = (bool)reader["EnableTurning"];
                temp.EnableSpeech = (bool)reader["EnableSpeech"];
                if (speech1 != null)
                    temp.Speech.Add(speech1);
                if (speech2 != null)
                    temp.Speech.Add(speech2);
                if (speech3 != null)
                    temp.Speech.Add(speech3);
                if (speech4 != null)
                    temp.Speech.Add(speech4);
                if (speech5 != null)
                    temp.Speech.Add(speech5);
                temp.X = (ushort)x;
                temp.Y = (ushort)y;
                temp.AreaID = (int)reader["AreaId"];
                temp.Direction = (byte)direction;
                temp.Waypoints = new List<Position>();
                if (way1 != null)
                    temp.Waypoints.Add(way1);
                if (way2 != null)
                    temp.Waypoints.Add(way2);
                if (way3 != null)
                    temp.Waypoints.Add(way3);
                if (way4 != null)
                    temp.Waypoints.Add(way4);
                if (way5 != null)
                    temp.Waypoints.Add(way5);
                temp.PathQualifer = pathFind;
                temp.Name = reader["Name"].ToString();
                if (stock1 != null)
                    temp.DefaultMerchantStock.Add(stock1);
                if (stock2 != null)
                    temp.DefaultMerchantStock.Add(stock2);
                if (stock3 != null)
                    temp.DefaultMerchantStock.Add(stock3);
                if (stock4 != null)
                    temp.DefaultMerchantStock.Add(stock4);
                if (stock5 != null)
                    temp.DefaultMerchantStock.Add(stock5);
                if (stock6 != null)
                    temp.DefaultMerchantStock.Add(stock6);
                if (stock7 != null)
                    temp.DefaultMerchantStock.Add(stock7);
                if (stock8 != null)
                    temp.DefaultMerchantStock.Add(stock8);
                if (stock9 != null)
                    temp.DefaultMerchantStock.Add(stock9);
                if (stock10 != null)
                    temp.DefaultMerchantStock.Add(stock10);
                if (stock11 != null)
                    temp.DefaultMerchantStock.Add(stock11);
                if (stock12 != null)
                    temp.DefaultMerchantStock.Add(stock12);
                if (stock13 != null)
                    temp.DefaultMerchantStock.Add(stock13);
                if (stock14 != null)
                    temp.DefaultMerchantStock.Add(stock14);
                if (stock15 != null)
                    temp.DefaultMerchantStock.Add(stock15);
                if (stock16 != null)
                    temp.DefaultMerchantStock.Add(stock16);
                if (stock17 != null)
                    temp.DefaultMerchantStock.Add(stock17);
                if (stock18 != null)
                    temp.DefaultMerchantStock.Add(stock18);
                if (stock19 != null)
                    temp.DefaultMerchantStock.Add(stock19);
                if (stock20 != null)
                    temp.DefaultMerchantStock.Add(stock20);
                if (stock21 != null)
                    temp.DefaultMerchantStock.Add(stock21);
                if (stock22 != null)
                    temp.DefaultMerchantStock.Add(stock22);
                if (stock23 != null)
                    temp.DefaultMerchantStock.Add(stock23);
                if (stock24 != null)
                    temp.DefaultMerchantStock.Add(stock24);
                if (stock25 != null)
                    temp.DefaultMerchantStock.Add(stock25);
                if (stock26 != null)
                    temp.DefaultMerchantStock.Add(stock26);
                if (stock27 != null)
                    temp.DefaultMerchantStock.Add(stock27);
                if (stock28 != null)
                    temp.DefaultMerchantStock.Add(stock28);
                if (stock29 != null)
                    temp.DefaultMerchantStock.Add(stock29);
                if (stock30 != null)
                    temp.DefaultMerchantStock.Add(stock30);

                if (temp.Name == null) continue;
                ServerSetup.Instance.GlobalMundaneTemplateCache[temp.Name] = temp;
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

    private static Position WayPointConvert(string wayPointString)
    {
        if (wayPointString.IsNullOrEmpty()) return null;
        const char delim = ',';
        var cords = wayPointString.Split(delim);
        var x = 0;
        var y = 0;

        if (cords.Length == 0) return new Position(0, 0);

        foreach (var cord in cords)
        {
            if (cord == cords.FirstOrDefault())
            {
                x = cord.ToInt();
            }

            if (cord == cords.LastOrDefault())
            {
                y = cord.ToInt();
            }
        }

        return new Position(x, y);
    }
}