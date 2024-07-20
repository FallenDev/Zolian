using Chaos.Common.Definitions;

namespace Darkages.Types;

public sealed class UserOptions
{
    // Customized Options
    public bool Whisper { get; set; } = true;
    public bool GroupChat { get; set; } = true;
    public bool DmgNumbers { get; set; } = true;
    public bool Exchange { get; set; } = true;
    public bool GroundQualities { get; set; } = true;
    public bool GMPort { get; set; } = true;
    public bool Animations { get; set; } = true;

    // Built into client
    public bool GroupWindow { get; set; }
    public bool GraphicsType { get; set; }
    public bool UseAbilitiesType { get; set; }
    public bool ClickOpenProfile { get; set; }
    public bool NpcChatListen { get; set; }
    public bool PartyRequest { get; set; }

    /// <summary>
    ///     Toggles the given UserOption.
    /// </summary>
    /// <param name="opt">Option to toggle.</param>
    public void Toggle(UserOption opt)
    {
        switch (opt)
        {
            case UserOption.Option1:
                Whisper = !Whisper;
                break;
            case UserOption.Option2:
                GroupChat = !GroupChat;
                break;
            case UserOption.Option3:
                GMPort = !GMPort;
                break;
            case UserOption.Option4:
                Animations = !Animations;
                break;
            case UserOption.Option5:
                DmgNumbers = !DmgNumbers;
                break;
            case UserOption.Option6:
                Exchange = !Exchange;
                break;
            case UserOption.Option8:
                GroundQualities = !GroundQualities;
                break;
        }
    }

    public string ToString(UserOption opt)
    {
        const string optionsFormat = "{0,-25}:{1,-3}";

        return opt switch
        {
            UserOption.Request => ToString(),
            UserOption.Option1 => string.Format(optionsFormat, "1Listen to whisper", Whisper ? "ON" : "OFF"),
            UserOption.Option2 => string.Format(optionsFormat, "2World Chat", GroupChat ? "ON" : "OFF"),
            UserOption.Option3 => string.Format(optionsFormat, "3Can GM Port", GMPort ? "ON" : "OFF"),
            UserOption.Option4 => string.Format(optionsFormat, "4Belief in Magic", Animations ? "ON" : "OFF"),
            UserOption.Option5 => string.Format(optionsFormat, "5Damage Numbers", DmgNumbers ? "ON" : "OFF"),
            UserOption.Option6 => string.Format(optionsFormat, "6Exchange", Exchange ? "ON" : "OFF"),
            UserOption.Option7 => string.Format(optionsFormat, "7", GroupWindow ? "ON" : "OFF"),
            UserOption.Option8 => string.Format(optionsFormat, "8Ground Qualities", GroundQualities ? "ON" : "OFF"),
            UserOption.Option9 => string.Format(optionsFormat, "9", GraphicsType ? "ON" : "OFF"),
            UserOption.Option10 => string.Format(optionsFormat, "10", UseAbilitiesType ? "ON" : "OFF"),
            UserOption.Option11 => string.Format(optionsFormat, "11", ClickOpenProfile ? "ON" : "OFF"),
            UserOption.Option12 => string.Format(optionsFormat, "12", NpcChatListen ? "ON" : "OFF"),
            UserOption.Option13 => string.Format(optionsFormat, "13", PartyRequest ? "ON" : "OFF"),
            _ => ""
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