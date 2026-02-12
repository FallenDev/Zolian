using Darkages.CommandSystem;

using Xunit;

namespace ZolianTest.Unit.CommandSystem;

public sealed class TokenizerTests
{
    [Fact]
    public void Tokenize_SplitsOnWhitespace()
    {
        Token[] tokens = [];
        tokens = System.Buffers.ArrayPool<Token>.Shared.Rent(4);
        try
        {
            var input = "a b   c".AsSpan();
            var count = Tokenizer.Tokenize(input, ref tokens);

            Assert.Equal(3, count);
            Assert.Equal("a", tokens[0].Slice(input).ToString());
            Assert.Equal("b", tokens[1].Slice(input).ToString());
            Assert.Equal("c", tokens[2].Slice(input).ToString());
        }
        finally
        {
            System.Buffers.ArrayPool<Token>.Shared.Return(tokens);
        }
    }

    [Fact]
    public void Tokenize_PreservesQuotes_InTokenSlice()
    {
        Token[] tokens = System.Buffers.ArrayPool<Token>.Shared.Rent(4);
        try
        {
            var input = "say \"hello world\"".AsSpan();
            var count = Tokenizer.Tokenize(input, ref tokens);

            Assert.Equal(2, count);
            Assert.Equal("say", tokens[0].Slice(input).ToString());

            var raw = tokens[1].Slice(input);
            Assert.Equal("\"hello world\"", raw.ToString());
            Assert.Equal("hello world", Tokenizer.Unquote(raw).ToString());
        }
        finally
        {
            System.Buffers.ArrayPool<Token>.Shared.Return(tokens);
        }
    }

    [Fact]
    public void Tokenize_GrowsTokenArray_WhenCapacityExceeded()
    {
        // Start intentionally tiny, then force growth.
        Token[] tokens = System.Buffers.ArrayPool<Token>.Shared.Rent(1);
        var initial = tokens;

        try
        {
            var input = "a b c d e f g h".AsSpan();
            var count = Tokenizer.Tokenize(input, ref tokens);

            Assert.Equal(8, count);
            Assert.True(tokens.Length >= 8);

            // If growth happened, ref should change.
            Assert.True(ReferenceEquals(initial, tokens) == false || initial.Length >= 8);
        }
        finally
        {
            System.Buffers.ArrayPool<Token>.Shared.Return(tokens);
        }
    }
}
