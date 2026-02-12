using System.Buffers;

namespace Darkages.CommandSystem;

public readonly struct Token
{
    public readonly int Start;
    public readonly int Length;

    public Token(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> source) => source.Slice(Start, Length);
}

public static class Tokenizer
{
    /// <summary>
    /// Tokenizes command line into tokens, supporting quoted strings with "..."
    /// Writes into a pooled array and returns token count.
    /// If token count exceeds capacity, it grows by renting a larger array and copying.
    ///
    /// IMPORTANT: tokens is passed by ref so the caller keeps the grown reference.
    /// </summary>
    public static int Tokenize(ReadOnlySpan<char> input, ref Token[] tokens)
    {
        int count = 0;
        int i = 0;

        while (i < input.Length)
        {
            while (i < input.Length && char.IsWhiteSpace(input[i]))
                i++;

            if (i >= input.Length)
                break;

            int start = i;

            if (input[i] == '"')
            {
                i++; // skip opening quote
                start = i;

                while (i < input.Length && input[i] != '"')
                    i++;

                int len = i - start;

                EnsureCapacity(ref tokens, count);
                tokens[count++] = new Token(start - 1, len + 2);

                if (i < input.Length && input[i] == '"')
                    i++; // skip closing quote
            }
            else
            {
                while (i < input.Length && !char.IsWhiteSpace(input[i]))
                    i++;

                int len = i - start;

                EnsureCapacity(ref tokens, count);
                tokens[count++] = new Token(start, len);
            }
        }

        return count;
    }

    public static ReadOnlySpan<char> Unquote(ReadOnlySpan<char> s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s.Slice(1, s.Length - 2);
        return s;
    }

    private static void EnsureCapacity(ref Token[] arr, int neededIndex)
    {
        if (neededIndex < arr.Length)
            return;

        var bigger = ArrayPool<Token>.Shared.Rent(arr.Length * 2);
        Array.Copy(arr, bigger, arr.Length);
        ArrayPool<Token>.Shared.Return(arr);
        arr = bigger;
    }
}