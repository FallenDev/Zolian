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
            case UserOption.Setting1:
                Whisper = !Whisper;
                break;
            case UserOption.Setting2:
                GroupChat = !GroupChat;
                break;
            case UserOption.Setting3:
                GMPort = !GMPort;
                break;
            case UserOption.Setting4:
                Animations = !Animations;
                break;
            case UserOption.Setting5:
                DmgNumbers = !DmgNumbers;
                break;
            case UserOption.Setting6:
                Exchange = !Exchange;
                break;
            case UserOption.Setting8:
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
            UserOption.Setting1 => string.Format(optionsFormat, "1Listen to whisper", Whisper ? "ON" : "OFF"),
            UserOption.Setting2 => string.Format(optionsFormat, "2Group Chat", GroupChat ? "ON" : "OFF"),
            UserOption.Setting3 => string.Format(optionsFormat, "3Can GM Port", GMPort ? "ON" : "OFF"),
            UserOption.Setting4 => string.Format(optionsFormat, "4Belief in Magic", Animations ? "ON" : "OFF"),
            UserOption.Setting5 => string.Format(optionsFormat, "5Damage Numbers", DmgNumbers ? "ON" : "OFF"),
            UserOption.Setting6 => string.Format(optionsFormat, "6Exchange", Exchange ? "ON" : "OFF"),
            UserOption.Setting7 => string.Format(optionsFormat, "7", GroupWindow ? "ON" : "OFF"),
            UserOption.Setting8 => string.Format(optionsFormat, "8Ground Qualities", GroundQualities ? "ON" : "OFF"),
            UserOption.Setting9 => string.Format(optionsFormat, "9", GraphicsType ? "ON" : "OFF"),
            UserOption.Setting10 => string.Format(optionsFormat, "10", UseAbilitiesType ? "ON" : "OFF"),
            UserOption.Setting11 => string.Format(optionsFormat, "11", ClickOpenProfile ? "ON" : "OFF"),
            UserOption.Setting12 => string.Format(optionsFormat, "12", NpcChatListen ? "ON" : "OFF"),
            UserOption.Setting13 => string.Format(optionsFormat, "13", PartyRequest ? "ON" : "OFF")
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