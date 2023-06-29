using Chaos.Common.Definitions;
using Chaos.Models.Data;
using Chaos.Networking.Options;

namespace Chaos.Services.Servers.Options;

public sealed record WorldOptions : ServerOptions, IWorldOptions
{
    /// <inheritdoc />
    public required int AislingAssailIntervalMs { get; init; }
    /// <inheritdoc />
    public required ChannelSettings[] DefaultChannels { get; set; } = Array.Empty<ChannelSettings>();
    /// <inheritdoc />
    public required int DropRange { get; init; }
    /// <inheritdoc />
    public required string F1MerchantTemplateKey { get; init; }
    /// <inheritdoc />
    public required int GroundItemDespawnTimeMins { get; init; }
    /// <inheritdoc />
    public required string GroupChatName { get; init; }
    /// <inheritdoc />
    public required MessageColor GroupMessageColor { get; init; }
    /// <inheritdoc />
    public required string GuildChatName { get; init; }
    /// <inheritdoc />
    public required MessageColor GuildMessageColor { get; init; }
    /// <inheritdoc />
    public override string HostName { get; set; } = string.Empty;
    public static IWorldOptions Instance { get; set; } = null!;
    public required ConnectionInfo LoginRedirect { get; init; }
    /// <inheritdoc />
    public int? LootDropsLockToRewardTargetSecs { get; init; }
    /// <inheritdoc />
    public required int MaxActionsPerSecond { get; init; }
    /// <inheritdoc />
    public required int MaxChantTimeBurdenMs { get; init; }
    /// <inheritdoc />
    public required int MaxGoldHeld { get; init; }
    /// <inheritdoc />
    public required int MaxGroupSize { get; init; }
    /// <inheritdoc />
    public required int MaximumAislingAc { get; init; }
    /// <inheritdoc />
    public required int MaximumMonsterAc { get; init; }
    /// <inheritdoc />
    public required int MaxItemsPerSecond { get; init; }
    /// <inheritdoc />
    public required int MaxLevel { get; init; }
    /// <inheritdoc />
    public required int MaxSkillsPerSecond { get; init; }
    /// <inheritdoc />
    public required int MaxSpellsPerSecond { get; init; }
    /// <inheritdoc />
    public required int MinimumAislingAc { get; init; }
    /// <inheritdoc />
    public required int MinimumMonsterAc { get; init; }
    /// <inheritdoc />
    public required int PickupRange { get; init; }
    /// <inheritdoc />
    public required bool ProhibitF5Walk { get; init; }
    /// <inheritdoc />
    public required bool ProhibitItemSwitchWalk { get; init; }
    /// <inheritdoc />
    public required bool ProhibitSpeedWalk { get; init; }
    /// <inheritdoc />
    public required int RefreshIntervalMs { get; init; }
    /// <inheritdoc />
    public required double SaveIntervalMins { get; init; }
    /// <inheritdoc />
    public required int TradeRange { get; init; }
    /// <inheritdoc />
    public required int UpdatesPerSecond { get; init; }
}