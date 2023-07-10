using Chaos.Common.Definitions;

namespace Darkages.Types;

public sealed class UserOptions
{
    public bool Exchange { get; set; }

    public bool FastMove { get; set; }

    public bool Group { get; set; } = true;

    public bool GuildChat { get; set; } = true;

    public bool Magic { get; set; } = true;

    public bool Shout { get; set; } = true;
    public bool Whisper { get; set; } = true;

    public bool Wisdom { get; set; } = true;

    /// <summary>
    ///     Toggles the given UserOption.
    /// </summary>
    /// <param name="opt">Option to toggle.</param>
    public void Toggle(UserOption opt)
    {
        switch (opt)
        {
            case UserOption.Whisper:
                Whisper = !Whisper;

                break;
            case UserOption.Group:
                Group = !Group;

                break;
            case UserOption.Shout:
                Shout = !Shout;

                break;
            case UserOption.Wisdom:
                Wisdom = !Wisdom;

                break;
            case UserOption.Magic:
                Magic = !Magic;

                break;
            case UserOption.Exchange:
                Exchange = !Exchange;

                break;
            case UserOption.FastMove:
                FastMove = !FastMove;

                break;
            case UserOption.GuildChat:
                GuildChat = !GuildChat;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opt), opt, "Unknown enum value");
        }
    }

    public string ToString(UserOption opt)
    {
        const string OPTIONS_FORMAT = "{0,-25}:{1,-3}";

        return opt switch
        {
            UserOption.Request => ToString(),
            UserOption.Whisper => string.Format(OPTIONS_FORMAT, "1Listen to whisper", Whisper ? "ON" : "OFF"),
            UserOption.Group => string.Format(OPTIONS_FORMAT, "2Join a group", Group ? "ON" : "OFF"),
            UserOption.Shout => string.Format(OPTIONS_FORMAT, "3Listen to shout", Shout ? "ON" : "OFF"),
            UserOption.Wisdom => string.Format(OPTIONS_FORMAT, "4Believe in wisdom", Wisdom ? "ON" : "OFF"),
            UserOption.Magic => string.Format(OPTIONS_FORMAT, "5Believe in magic", Magic ? "ON" : "OFF"),
            UserOption.Exchange => string.Format(OPTIONS_FORMAT, "6Exchange", Exchange ? "ON" : "OFF"),
            UserOption.FastMove => string.Format(OPTIONS_FORMAT, "7Fast Move", FastMove ? "ON" : "OFF"),
            UserOption.GuildChat => string.Format(OPTIONS_FORMAT, "8Guild Chat", GuildChat ? "ON" : "OFF"),
            _ => throw new ArgumentOutOfRangeException(nameof(opt), opt, "Unknown enum value")
        };
    }

    public override string ToString()
    {
        var options = new string[8];

        for (var i = 0; i < 8; i++)
            options[i] = ToString((UserOption)i + 1).Remove(0, 1);

        return $"0{string.Join("\t", options)}";
    }
}