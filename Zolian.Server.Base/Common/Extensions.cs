using Darkages.Enums;
using Darkages.Sprites;

using System.Numerics;
using System.Text;

namespace Darkages.Common;

public static class Extensions
{
    // Korean Encoding
    private static readonly Encoding Encoding = Encoding.GetEncoding(949);

    /// <summary>
    /// Prevents an integer from going below or above a set value
    /// </summary>
    public static int IntClamp(this int value, int min, int max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }

    /// <summary>
    /// Prevents a double from going below or above a set value
    /// </summary>
    public static double DoubleClamp(this double value, double min, double max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }

    /// <summary>
    /// Takes a byte and reverses it by adding 2, if above 4 it removes 4.
    /// 1 + 2 = 3 || 2 + 2 = 4 || (3 + 2) - 4 = 1 || (4 + 2) - 4 = 2
    /// </summary>
    public static byte ReverseDirection(this byte direction)
    {
        direction += 2;

        if (direction >= 4)
            direction -= 4;

        return direction;
    }

    /// <summary>
    /// Checks whether or not an integer is within two numbers
    /// </summary>
    public static bool IntIsWithin(this int value, int minimum, int maximum) => value >= minimum && value <= maximum;

    /// <summary>
    /// Converts a string to a byte array
    /// </summary>
    public static byte[] ToByteArray(this string str) => Encoding.GetBytes(str);

    /// <summary>
    /// Compares two strings whether or not they're equal, case ignored
    /// </summary>
    public static bool StringEquals(this string str1, string str2) => StringComparer.OrdinalIgnoreCase.Equals(str1, str2);

    /// <summary>
    /// Checks within a string for a string -- Used to check for keywords or prohibited words, case ignored
    /// </summary>
    public static bool StringContains(this string str1, string str2) => str1.IndexOf(str2, StringComparison.OrdinalIgnoreCase) != -1;

    /// <summary>
    /// Replaces a value within a string with case ignored
    /// </summary>
    /// <param name="str">Input string</param>
    /// <param name="oldValue">Value to change</param>
    /// <param name="newValue">New value, if no value is chosen, defaults to deleting the old value</param>
    public static string StringReplace(this string str, string oldValue, string newValue = "") => str.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Safely check if a list of strings contains a string with case ignored
    /// </summary>
    public static bool ListContains(this IEnumerable<string> sList, string str) => sList.Contains(str, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Deconstruction method for key value pair
    /// </summary>
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> entry, out TKey key, out TValue value)
    {
        key = entry.Key;
        value = entry.Value;
    }

    /// <summary>
    /// Checks if Player can see another Player, based on race, party, skill / spell
    /// </summary>
    /// <param name="source">If not a Player, return false</param>
    /// <param name="other">If not a Player, return true</param>
    public static bool CanSeeSprite(this Sprite source, Sprite other)
    {
        if (source is null) return false;
        if (other is null) return false;
        if (source == other) return true;
        if (!other.WithinRangeOf(source)) return false;
        if (other is not Aisling otherAisling) return true;
        if (!otherAisling.Dead && !otherAisling.IsInvisible) return true;
        if (source is not Aisling self) return false;
        if (otherAisling.Dead) return self.CanSeeGhosts();
        if (!otherAisling.IsInvisible) return false;
        if (self.GroupId != 0 && otherAisling.GroupId != 0 && otherAisling.GroupId == self.GroupId) return true;
        if (self.GameMaster || self.CanSeeInvisible || self.Path is Class.Assassin || self.PastClass is Class.Assassin
            || self.Race is Race.DarkElf or Race.WoodElf or Race.Dwarf) return true;
        return otherAisling.GameMaster || otherAisling.CanSeeInvisible || self.Path is Class.Assassin || self.PastClass is Class.Assassin
               || self.Race is Race.DarkElf or Race.WoodElf or Race.Dwarf;
    }

    /// <summary>
    ///     Finds the next highest number in a sequence from a given value
    /// </summary>
    /// <param name="enumerable">The sequence to search</param>
    /// <param name="seed">The starting value</param>
    /// <typeparam name="T">A numeric type</typeparam>
    public static T NextHighest<T>(this IEnumerable<T> enumerable, T seed) where T : INumber<T>
    {
        var current = seed;

        foreach (var number in enumerable)
        {
            //dont consider any numbers that are less than or equal to the seed
            if (number <= seed)
                continue;

            //if the current number is the seed, take the first number that reaches this statement
            //only numbers that are greater than the seed will reach this statement
            if (current == seed)
                current = number;
            //otherwise, if the number is less than the current number, take it
            //all numbers that reach this statement are greater than the seed
            else if (number < current)
                current = number;
        }

        return current;
    }

    /// <summary>
    ///     Finds the next lowest number in a sequence from a given value
    /// </summary>
    /// <param name="enumerable">The sequence to search</param>
    /// <param name="seed">The starting value</param>
    /// <typeparam name="T">A numeric type</typeparam>
    public static T NextLowest<T>(this IEnumerable<T> enumerable, T seed) where T : INumber<T>
    {
        var current = seed;

        foreach (var number in enumerable)
        {
            //dont consider any numbers that are greater than or equal to the seed
            if (number >= seed)
                continue;

            //if the current number is the seed, take the first number that reaches this statement
            //only numbers that are less than the seed will reach this statement
            if (current == seed)
                current = number;
            //otherwise, if the number is greater than the current number, take it
            //all numbers that reach this statement are lower than the seed
            else if (number > current)
                current = number;
        }

        return current;
    }
}