using Chaos.Common.Definitions;
using Chaos.Models.Data;

namespace Chaos.Services.Servers.Options;

public interface IWorldOptions
{
    /// <summary>
    ///     All creatures have an AssailInterval. AssailInterval is essentially the cooldown in milliseconds for skills marked
    ///     as assails. This
    ///     value ismodified by the AtkSpeedPct attribute and can be modified to be 3x faster or slower than the base value.
    ///     This property sets the
    ///     base interval for all aislings. <br />
    ///     <br />
    ///     A good starting value is 1500
    /// </summary>
    int AislingAssailIntervalMs { get; }

    /// <summary>
    ///     This is a collection of channel names that new characters will join by default. These channels are also registered
    ///     with the channel
    ///     service during startup.
    /// </summary>
    ChannelSettings[] DefaultChannels { get; }
    /// <summary>
    ///     This is the maximum distance from a player that they can drop items or gold on the ground. <br />
    ///     A value of -1 would effectively disable dropping items <br />
    ///     A value of 0 would only allow players to drop items directly beneath them  <br />
    ///     A value of 12 would allow players to drop items anywhere in their viewport
    /// </summary>
    int DropRange { get; }
    /// <summary>
    ///     This is the template key of the merchant to display when a player presses F1. This is generally some kind of help
    ///     npc
    /// </summary>
    string F1MerchantTemplateKey { get; }
    /// <summary>
    ///     The number of minutes that ground items will remain on the ground before despawning. This includes items dropped by
    ///     players for any reason
    /// </summary>
    int GroundItemDespawnTimeMins { get; }

    /// <summary>
    ///     When whispering a target name in the client, this is the name that will redirect to group chat
    /// </summary>
    string GroupChatName { get; }

    /// <summary>
    ///     The default message color used for group chat
    /// </summary>
    MessageColor GroupMessageColor { get; }

    /// <summary>
    ///     When whispering a target name in the client, this is the name that will redirect to guild chat
    /// </summary>
    string GuildChatName { get; }

    /// <summary>
    ///     The default message color used for guild chat
    /// </summary>
    MessageColor GuildMessageColor { get; }
    /// <summary>
    ///     Default null.<br />If specified, locks loot drops to the reward target for this many seconds.<br />If null, loot
    ///     drops are not locked to the reward target.
    /// </summary>
    int? LootDropsLockToRewardTargetSecs { get; }
    /// <summary>
    ///     It would be bad to allow players to perform an infinite number of actions per second. Anything without a cooldown
    ///     could become a huge
    ///     burden on the server. This value is used to control the maximum number of actions a player can take in a second. An
    ///     action is defined
    ///     as using any spell, skill, or item. This includes equipping items.<br />
    ///     <br />
    ///     A good range of values for this would be 4 - 10<br />
    ///     If desired, this value can be set to 3 to emulate the original game
    /// </summary>
    int MaxActionsPerSecond { get; }
    /// <summary>
    ///     When players cast spells, each spell line takes approximately 1000ms to chant. The amount of time a spell will take
    ///     to cast can be
    ///     predicted to be 1000ms * (NumSpellLines). <br />
    ///     <br />
    ///     Due to latency and jitter, players will often cast spells for slightly more or less than the expected amount of
    ///     time. With big latency
    ///     spikes, the observed amount of time could be far off than the expected value. To be able to tolerate this while
    ///     also prohibiting "speed
    ///     casting", the server will allow spell casts that occur too quickly and add up a time burden. <br />
    ///     <br />
    ///     Each time a player casts a spell that completes faster than expected, the difference in time will be added to the
    ///     time burden. This
    ///     burden will accumulate with every consecutive spell that occurs too quickly, and be subtracted from if a spell
    ///     completes too slowly.
    ///     The time burden will also decrease while not casting spells.<br />
    ///     <br />
    ///     If the time burden exceeds MaxChantTimeBurdenMs, the spell cast will be ignored. <br />
    ///     A good range of values for this setting would be 500 - 1500, with lower values being more strict.
    /// </summary>
    int MaxChantTimeBurdenMs { get; }
    /// <summary>
    ///     This is the maximum amount of gold a player can hold in their inventory.
    /// </summary>
    int MaxGoldHeld { get; }
    /// <summary>
    ///     The maximum number of players that can be in a group together. If a group reaches this size, invites and invite
    ///     accepts will fail.
    /// </summary>
    int MaxGroupSize { get; }
    /// <summary>
    ///     This is the maximum amount of AC a player can have. damage formulas can be changed, but with the default damage
    ///     formula, higher AC =
    ///     more damage taken.
    /// </summary>
    int MaximumAislingAc { get; }
    /// <summary>
    ///     This is the maximum amount of AC a monster can have. damage formulas can be changed, but with the default damage
    ///     formula, higher AC =
    ///     more damage taken.
    /// </summary>
    int MaximumMonsterAc { get; }
    /// <summary>
    ///     The maximum number of items a player can use in a second
    /// </summary>
    int MaxItemsPerSecond { get; }
    /// <summary>
    ///     This is the level cap for players. Level formulas can be changed, but with the default level formula, if you reach
    ///     this level you will
    ///     stop gaining experience.
    /// </summary>
    int MaxLevel { get; }
    /// <summary>
    ///     The maximum number of skills a player can use in a second
    /// </summary>
    int MaxSkillsPerSecond { get; }
    /// <summary>
    ///     The maximum number of spells a player can use in a second
    /// </summary>
    int MaxSpellsPerSecond { get; }
    /// <summary>
    ///     This is the minimum amount of AC a player can have. damage formulas can be changed, but with the default damage
    ///     formula, lower AC =
    ///     less damage taken. <br />
    ///     With the default damage formula, AC is a percentile, so -100 AC would make you invulnerable.
    /// </summary>
    int MinimumAislingAc { get; }
    /// <summary>
    ///     This is the minimum amount of AC a monster can have. damage formulas can be changed, but with the default damage
    ///     formula, lower AC =
    ///     less damage taken. <br />
    ///     With the default damage formula, AC is a percentile, so -100 AC would make you invulnerable.
    /// </summary>
    int MinimumMonsterAc { get; }
    /// <summary>
    ///     This is the maximum distance from a player that they pick up items or money from the ground.<br />
    ///     A value of -1 would effectively disable picking up items<br />
    ///     A value of 0 would only allow players to pick up items from directly beneath them<br />
    ///     A value of 12 would allow players to pick up items from anywhere in their viewport<br />
    /// </summary>
    int PickupRange { get; }
    /// <summary>
    ///     When this is enabled it will prevent players from utilizing refreshing(F5) to walk faster.
    /// </summary>
    bool ProhibitF5Walk { get; }
    /// <summary>
    ///     When this is enabled it will prevent players from utilizing item switching to walk faster.
    /// </summary>
    bool ProhibitItemSwitchWalk { get; }
    /// <summary>
    ///     When this is enabled it will prevent players from utilizing more nefarious methods to walk faster.
    /// </summary>
    bool ProhibitSpeedWalk { get; }
    /// <summary>
    ///     This is the quickest interval in milliseconds that players will be allowed to refresh their client. This will not
    ///     include refreshes
    ///     utilized by the server, such as for refreshing a player's position if they walk into a wall. A good value here
    ///     would be 1000
    /// </summary>
    int RefreshIntervalMs { get; }
    /// <summary>
    ///     This is the amount of time in minutes between global character saves. This is on top of players automatically
    ///     saving when they log out.
    ///     <br />
    ///     <br />
    ///     Do not set this value too low or it will become a burden on the server. <br />
    ///     A good range of values would be anywhere from 0.5 - 10
    /// </summary>
    double SaveIntervalMins { get; }
    /// <summary>
    ///     This is the maximum distance from a player that they can initiate a trade with another player<br />
    ///     A value of -1 would effectively disable trading<br />
    ///     A value of 0 would only allow players to only trade with players on the same tile, which is generally impossible
    ///     <br />
    ///     A value of 12 would allow players to trade with anyone in their viewport<br />
    /// </summary>
    int TradeRange { get; }

    /// <summary>
    ///     This is the number of times per second that the server will update the game state. .NET isn't great for this kind
    ///     of workload due
    ///     garbage collection and JIT recompilation/OSR. <br />
    ///     <br />
    ///     I suggest a hard cap of 60 with a good default value of 30 <br />
    ///     If desired you can set this to 3 to emulate the original game
    /// </summary>
    int UpdatesPerSecond { get; }
}