using System.Security.Cryptography;

namespace Darkages.Common;

public static class Generator
{
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

    /// <summary>
    /// Extension method for IEnumerables, pulls a random element from the collection
    /// </summary>
    /// <returns>Random element from collection</returns>
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