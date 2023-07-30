using System.Security.Cryptography;
using System.Text;

namespace Darkages.Common;

public static class Generator
{
    private static readonly List<string> GeneratedStrings;

    static Generator()
    {
        GeneratedStrings = new List<string>();
    }

    public static string GenerateString(int size)
    {
        string str;

        do
        {
            str = CreateString(size);
        } while (GeneratedStrings.Contains(str));

        GeneratedStrings.Add(str);

        return str;
    }

    private static string CreateString(int size)
    {
        var value = new StringBuilder();

        for (var i = 0; i < size; i++)
        {
            var binary = Random.Shared.Next(0, 2);

            switch (binary)
            {
                case 0:
                    value.Append(Convert.ToChar(Random.Shared.Next(65, 91)));
                    break;

                case 1:
                    value.Append(Random.Shared.Next(1, 10));
                    break;
            }
        }

        return value.ToString();
    }

    public static int GenerateDeterminedNumberRange(int min, int max) => Random.Shared.Next(min, max + 1);

    public static int GenerateMapLocation(int rowCol) => RandomNumberGenerator.GetInt32(rowCol + 1);

    public static int RandNumGen3() => RandomNumberGenerator.GetInt32(3);
    public static int RandNumGen10() => RandomNumberGenerator.GetInt32(10);
    public static int RandNumGen20() => RandomNumberGenerator.GetInt32(20);
    public static int RandNumGen100() => RandomNumberGenerator.GetInt32(100);

    public static double RandomNumPercentGen() => Random.Shared.NextDouble();

    public static T RandomEnumValue<T>()
    {
        var v = Enum.GetValues(typeof(T));
        return (T)v.GetValue(RandomNumberGenerator.GetInt32(v.Length));
    }

    public static T RandomIEnum<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.RandomElementUsing<T>(new Random());
    }

    private static T RandomElementUsing<T>(this IEnumerable<T> enumerable, Random rand)
    {
        var enumerable1 = enumerable as T[] ?? enumerable.ToArray();
        var index = rand.Next(0, enumerable1.Count());
        return enumerable1.ElementAt(index);
    }

    public static int RandomMonsterStatVariance(int value)
    {
        var variance = RandomNumberGenerator.GetInt32(9);
        var percent = variance switch
        {
            1 => 0.02,
            2 => 0.04,
            3 => 0.06,
            4 => 0.08,
            5 => 0.1,
            6 => 0.12,
            7 => 0.14,
            8 => 0.16,
            _ => 0.18
        };

        var bonusValue = (int)(value * percent);
        value += bonusValue;

        return value;
    }
}