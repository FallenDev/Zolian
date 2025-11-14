using JetBrains.Annotations;

namespace Darkages.Common;

/// <summary>
///     Provides extensions methods for <see cref="System.String" />
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     Calculates the Levenshtein Distance between 2 strings
    /// </summary>
    /// <param name="str1">
    ///     The first string
    /// </param>
    /// <param name="str2">
    ///     The second string
    /// </param>
    /// <param name="caseSensitive">
    ///     Whether or not the comparison is case sensitive
    /// </param>
    /// <returns>
    ///     A value indicating the number of changes that would be required to make the 2 strings equal
    /// </returns>
    public static int CalculateLevenshteinDistance(this string str1, string str2, bool caseSensitive = false)
    {
        var span1 = str1.AsSpan();
        var span2 = str2.AsSpan();

        var length1 = span1.Length;
        var length2 = span2.Length;
        Span<int> indexes = stackalloc int[(length1 + 1) * (length2 + 1)];

        // fast paths
        if (length1 == 0)
            return length2;

        if (length2 == 0)
            return length1;

        // init indexes
        for (var i = 0; i <= length1; i++)
            indexes[MapIndex(i, 0)] = i;

        for (var j = 0; j <= length2; j++)
            indexes[MapIndex(0, j)] = j;

        for (var i = 1; i <= length1; i++)
        {
            for (var j = 1; j <= length2; j++)
            {
                int cost;

                //if the values are not equal, cost = 1
                if (caseSensitive)
                    cost = span2[j - 1] == span1[i - 1] ? 0 : 1;
                else
                    cost = char.ToLowerInvariant(span2[j - 1]) == char.ToLowerInvariant(span1[i - 1]) ? 0 : 1;

                // pick the minimum cost between changing either string (+1),
                // or the cost of the last checkpoint + current cost(1 or 0)
                // set the current checkpoint to the lowest of these costs
                // at the end, the last checkpoint will be the lowest cost to make the strings equal
                indexes[MapIndex(i, j)] = Math.Min(
                    Math.Min(indexes[MapIndex(i - 1, j)] + 1, indexes[MapIndex(i, j - 1)] + 1),
                    indexes[MapIndex(i - 1, j - 1)] + cost);
            }
        }

        // Return cost.
        return indexes[MapIndex(length1, length2)];

        // map 2d coordinates to 1d
        int MapIndex(int i, int j) => i * (length2 + 1) + j;
    }

    /// <summary>
    ///     Calculates the Sørensen–Dice coefficient for 2 strings
    /// </summary>
    /// <param name="string1">
    ///     The string to check
    /// </param>
    /// <param name="string2">
    ///     The string to check against
    /// </param>
    /// <param name="caseSensitive">
    ///     Whether or not the calculation is case sensitive
    /// </param>
    /// <returns>
    ///     A value quantifying the same-ness of the 2 strings. Higher is better.
    /// </returns>
    public static decimal CalculateSorensenCoefficient(this string string1, string string2, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(string1) || string.IsNullOrEmpty(string2))
            return 0m;

        var span1 = string1.AsSpan();
        var span2 = string2.AsSpan();
        var intersections = 0;
        var totalBigrams = string1.Length + string2.Length - 2;
        Span<int> targetBigrams = stackalloc int[span2.Length - 1];

        //encode all target bigrams as single values
        for (var i = 0; i < (span2.Length - 1); i++)
        {
            var bigram = span2.Slice(i, 2);
            var encodedBigram = EncodeBigram(bigram, caseSensitive);
            targetBigrams[i] = encodedBigram;
        }

        //for each bigram in the source, see if it exists in the target
        for (var i = 0; i < (span1.Length - 1); i++)
        {
            var bigram = span1.Slice(i, 2);
            var encodedBigram = EncodeBigram(bigram, caseSensitive);
            var index = targetBigrams.IndexOf(encodedBigram);

            //if the bigram was found, increase the intersection count and remove the bigram from targetBigrams
            if (index >= 0)
            {
                intersections++;
                targetBigrams[index] = 0;
            }
        }

        return 2m * intersections / totalBigrams;

        //encode a bigram as a single value (a bigram is just 2 chars, each char is 16bits (2 bytes))
        //so we must encode into 32bits (4bytes) to be lossless (int = 4bytes)
        static int EncodeBigram(ReadOnlySpan<char> chars, bool caseSensitive = false)
        {
            if (caseSensitive)
                return (chars[0] << 16) | chars[1];

            return (char.ToLowerInvariant(chars[0]) << 16) | char.ToLowerInvariant(chars[1]);
        }
    }

    /// <summary>
    ///     Center aligns a string in a field of a specified width.
    /// </summary>
    /// <param name="str1">
    ///     The string to center-align
    /// </param>
    /// <param name="width">
    ///     The width of the field
    /// </param>
    public static string CenterAlign(this string str1, int width)
    {
        if (str1.Length > width)
            return str1[..width];

        var leftPadding = (width - str1.Length) / 2;
        var rightPadding = width - str1.Length - leftPadding;

        return new string(' ', leftPadding) + str1 + new string(' ', rightPadding);
    }

    /// <summary>
    ///     Returns a value indicating whether a specified string occurs within this string when compared case insensitively.
    /// </summary>
    /// <param name="str1">
    /// </param>
    /// <param name="str2">
    ///     The string to seek
    /// </param>
    /// <returns>
    ///     <c>
    ///         true
    ///     </c>
    ///     if the value parameter occurs within this string, or if value is the empty string (""); otherwise,
    ///     <c>
    ///         false
    ///     </c>
    /// </returns>
    public static bool ContainsI(this string str1, string str2) => str1.Contains(str2, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Determines whether the end of this string instance matches the specified string when compared case insensitively.
    /// </summary>
    /// <param name="str1">
    /// </param>
    /// <param name="str2">
    ///     The string to compare to the substring at the end of this instance
    /// </param>
    /// <returns>
    ///     <c>
    ///         true
    ///     </c>
    ///     if the value parameter matches the end of this string; otherwise,
    ///     <c>
    ///         false
    ///     </c>
    /// </returns>
    public static bool EndsWithI(this string str1, string str2) => str1.EndsWith(str2, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Determines whether this string and a specified String object have the same value when compared case insensitively.
    /// </summary>
    /// <param name="str1">
    /// </param>
    /// <param name="str2">
    ///     The string to compare to this instance
    /// </param>
    /// <returns>
    ///     <c>
    ///         true
    ///     </c>
    ///     if the value of the value parameter is the same as this string; otherwise,
    ///     <c>
    ///         false
    ///     </c>
    /// </returns>
    public static bool EqualsI(this string str1, string str2) => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Capitalizes the first letter in a string
    /// </summary>
    /// <exception cref="ArgumentNullException">
    ///     input is null
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     input is empty
    /// </exception>
    public static string FirstUpper(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input switch
        {
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => string.Concat(char.ToUpper(input[0]), input[1..])
        };
    }

    /// <summary>
    ///     Fixes line endings so that they match the line endings in DarkAges
    /// </summary>
    /// <param name="str">
    ///     The string whose line endings to fix
    /// </param>
    public static string FixLineEndings(this string str)
        => str.ReplaceLineEndings("\n")
              .TrimEnd('\n');

    /// <summary>
    ///     Determines if any of the strings approximately match the specified string
    /// </summary>
    /// <param name="strings">
    ///     The strings to search
    /// </param>
    /// <param name="str">
    ///     The string being looked for
    /// </param>
    /// <param name="minCoefficient">
    ///     The lowest acceptable dice coefficient. Higher values mean the strings are more similar
    /// </param>
    /// <param name="maxDistancePct">
    ///     This value is used to calculate a maxDistance based on the length of the best matching string
    /// </param>
    /// <param name="maxDistance">
    ///     The max levenshtein distance between strings. Lower values mean the strings are more similar
    /// </param>
    /// <param name="caseSensitive">
    ///     Whether or not the search is case sensitive
    /// </param>
    /// <returns>
    ///     <c>
    ///         true
    ///     </c>
    ///     if any of the given strings approximately match the specified string, otherwise
    ///     <c>
    ///         false
    ///     </c>
    /// </returns>
    public static bool FuzzyContains(
        this IEnumerable<string> strings,
        string str,
        decimal minCoefficient = 0.666m,
        decimal maxDistancePct = 0.333m,
        int? maxDistance = null,
        bool caseSensitive = false)
        => FuzzySearch(
            strings,
            str,
            minCoefficient,
            maxDistancePct,
            maxDistance,
            caseSensitive) is not null;

    /// <summary>
    ///     Performs a fuzzy search on a sequence of strings and returns the best match
    /// </summary>
    /// <param name="strings">
    ///     The strings to search
    /// </param>
    /// <param name="str">
    ///     The string being looked for
    /// </param>
    /// <param name="minCoefficient">
    ///     The lowest acceptable dice coefficient. Higher values mean the strings are more similar
    /// </param>
    /// <param name="maxDistancePct">
    ///     This value is used to calculate a maxDistance based on the length of the best matching string
    /// </param>
    /// <param name="maxDistance">
    ///     The max levenshtein distance between strings. Lower values mean the strings are more similar
    /// </param>
    /// <param name="caseSensitive">
    ///     Whether or not the search is case sensitive
    /// </param>
    /// <returns>
    ///     If the constraints are passed, the best matched string, otherwise null
    /// </returns>
    public static string? FuzzySearch(
        this IEnumerable<string> strings,
        string str,
        decimal minCoefficient = 0.666m,
        decimal maxDistancePct = 0.333m,
        int? maxDistance = null,
        bool caseSensitive = false)
    {
        try
        {
            strings = caseSensitive ? strings.Distinct() : strings.Distinct(StringComparer.OrdinalIgnoreCase);

            //calculate dice coefficients for each string
            //filter out strings with coefficients below the minCoefficient
            //then calculate levenshtein distance for each string
            //filter out strings with distances above the maxDistance
            var possibleMatches = strings.Select(s => (String: s, Coefficient: s.CalculateSorensenCoefficient(str, caseSensitive)))
                                         .Where(x => x.Coefficient > minCoefficient)
                                         .Select(
                                             x => (x.String, x.Coefficient,
                                                 Distance: x.String.CalculateLevenshteinDistance(str, caseSensitive)))
                                         .Where(
                                             x =>
                                             {
                                                 //calculate the max acceptable levenshtein distance
                                                 var length = Math.Max(x.String.Length, str.Length);

                                                 var localMaxDistance = maxDistance ??= Math.Max(
                                                     1,
                                                     Convert.ToInt32(length * maxDistancePct));

                                                 return x.Distance <= localMaxDistance;
                                             })
                                         .ToList();

            return possibleMatches.Count switch
            {
                //if no good match was found, return null
                0 => null,

                //if there's only 1 match, return it
                1 => possibleMatches[0].String,
                _ => possibleMatches.MaxBy(CalculateHeuristic)
                                    .String
            };

            //based on the 2 values, calculate a heuristic to determine the best match
            decimal CalculateHeuristic((string String, decimal Coefficient, int Distance) x)
            {
                //get the length of the longer string
                var length = Math.Max(x.String.Length, str.Length);

                //1 - the levenshtein distance as a percentage of the length of the longer string
                //aka, lower levenshtein distances generate a higher heuristic
                var distanceHeuristic = 1 - (decimal)x.Distance / length;

                //the higher the dice coefficient, the better
                var coefficientHeuristic = x.Coefficient;

                return distanceHeuristic * coefficientHeuristic;
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Performs a fuzzy search on a sequence of strings and returns the best match
    /// </summary>
    /// <param name="items">
    ///     A sequence of items convertable to string to fuzzy search
    /// </param>
    /// <param name="selector">
    ///     A selector function to convert an item to a string
    /// </param>
    /// <param name="str">
    ///     The string being looked for
    /// </param>
    /// <param name="minCoefficient">
    ///     The lowest acceptable dice coefficient. Higher values mean the strings are more similar
    /// </param>
    /// <param name="maxDistancePct">
    ///     This value is used to calculate a maxDistance based on the length of the best matching string
    /// </param>
    /// <param name="maxDistance">
    ///     The max levenshtein distance between strings. Lower values mean the strings are more similar
    /// </param>
    /// <param name="caseSensitive">
    ///     Whether or not the search is case sensitive
    /// </param>
    /// <returns>
    ///     If the constraints are passed, the item with the best matched string, otherwise null
    /// </returns>
    public static T? FuzzySearchBy<T>(
        this IEnumerable<T> items,
        Func<T, string> selector,
        string str,
        decimal minCoefficient = 0.666m,
        decimal maxDistancePct = 0.333m,
        int? maxDistance = null,
        bool caseSensitive = false)
    {
        items = caseSensitive ? items.DistinctBy(selector) : items.DistinctBy(selector, StringComparer.OrdinalIgnoreCase);

        var lookup = items.ToDictionary(selector);

        if (lookup.Count == 0)
            return default;

        var match = lookup.Keys.FuzzySearch(
            str,
            minCoefficient,
            maxDistancePct,
            maxDistance,
            caseSensitive);

        if (match is null)
            return default;

        return lookup[match];
    }

    /// <summary>
    ///     Injects the parameters into the string, replacing the placeholders with the parameters
    /// </summary>
    /// <param name="str1">
    ///     The string in which to replace placeholders with parameters
    /// </param>
    /// <param name="parameters">
    ///     The parameters to inject into the string
    /// </param>
    /// <returns>
    ///     A string where the placeholders are replaced with the given parameters
    /// </returns>
    public static string Inject([StructuredMessageTemplate] this string str1, params ReadOnlySpan<object> parameters)
    {
        var paramsTotalLength = 0;
        var paramStrs = new string[parameters.Length];

        //convert all parameters to strings and aggregate the length of the parameters
        for (var i = 0; i < parameters.Length; i++)
        {
            var paramStr = parameters[i]
                               .ToString()
                           ?? string.Empty;
            paramsTotalLength += paramStr.Length;
            paramStrs[i] = paramStr;
        }

        //initialize indexes, allocate result span of max possible length
        var paramIndex = 0;
        var strIndex = 0;
        var resIndex = 0;
        Span<char> result = stackalloc char[str1.Length + paramsTotalLength];
        var startPhIndex = -1;

        while (strIndex < str1.Length)
        {
            var nextIndex = strIndex + 1;

            switch (str1[strIndex])
            {
                //handle double curly braces
                case '{' when (nextIndex < str1.Length) && (str1[nextIndex] == '{'):
                    result[resIndex++] = '{';
                    strIndex += 2;

                    break;

                //handle start of placeholder
                case '{':
                    startPhIndex = strIndex;
                    strIndex += 1;

                    break;

                //handle double curly braces
                case '}' when (nextIndex < str1.Length) && (str1[nextIndex] == '}'):
                    result[resIndex++] = '}';
                    strIndex += 2;

                    break;

                //handle end of placeholder
                case '}' when startPhIndex >= 0:
                    if (paramIndex >= parameters.Length)
                        throw new ArgumentException(
                            "The number of parameters is less than the number of placeholders in the string",
                            nameof(parameters));

                    var parameterValue = paramStrs[paramIndex++];

                    parameterValue.AsSpan()
                                  .CopyTo(result[resIndex..]);
                    resIndex += parameterValue.Length;
                    strIndex += 1;
                    startPhIndex = -1;

                    break;

                //handle normal character
                default:
                    if (startPhIndex < 0)
                        result[resIndex++] = str1[strIndex];

                    strIndex++;

                    break;
            }
        }

        return result[..resIndex]
            .ToString();
    }

    /// <summary>
    ///     Returns a new string in which all occurrences of a specified string in the current instance are replaced with
    ///     another specified string when compared case insensitively
    /// </summary>
    /// <param name="str1">
    /// </param>
    /// <param name="oldValue">
    ///     The string to be replaced
    /// </param>
    /// <param name="newValue">
    ///     The string to replace all occurrences of oldValue
    /// </param>
    /// <returns>
    ///     A string that is equivalent to the current string except that all instances of <paramref name="oldValue" /> are
    ///     replaced with <paramref name="newValue" />. If <paramref name="oldValue" /> is not found in the current instance,
    ///     the method returns the current instance unchanged
    /// </returns>
    public static string ReplaceI(this string str1, string oldValue, string newValue)
        => str1.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Determines whether the beginning of this string instance matches the specified string when compared case
    ///     insensitively.
    /// </summary>
    /// <param name="str1">
    /// </param>
    /// <param name="str2">
    ///     The string to compare
    /// </param>
    /// <returns>
    ///     <c>
    ///         true
    ///     </c>
    ///     if this instance begins with value; otherwise,
    ///     <c>
    ///         false
    ///     </c>
    /// </returns>
    public static bool StartsWithI(this string str1, string str2) => str1.StartsWith(str2, StringComparison.OrdinalIgnoreCase);
}