using System.Collections.Concurrent;
using Chaos.Common.Identity;

using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Types;

public class Party : ObjectManager
{
    private int Id { get; set; }
    public string LeaderName { get; private set; }
    public readonly ConcurrentDictionary<uint, Aisling> PartyMembers = [];

    public string PartyMemberString
    {
        get
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.Append($"Group members\n");

            foreach (var member in PartyMembers.Values)
            {
                var leader = " ";
                if (string.Equals(LeaderName, member.Username, StringComparison.InvariantCultureIgnoreCase))
                {
                    leader = "*";
                }

                stringBuilder.Append($"{leader} {member.Username}\n");
            }

            stringBuilder.Append($"Total {PartyMembers.Count}number of people");

            return stringBuilder.ToString();
        }
    }

    public static bool AddPartyMember(Aisling partyMember, Aisling playerToAdd)
    {
        if (playerToAdd == null) return false;
        if (partyMember.GroupId == 0)
            CreateParty(partyMember);

        if (!ServerSetup.Instance.GlobalGroupCache.TryGetValue(partyMember.GroupId, out var party)) return false;

        if (playerToAdd.GroupId == party.Id)
        {
            RemovePartyMember(playerToAdd);
            return false;
        }

        if (party.PartyMembers.Count >= 13)
        {
            partyMember.Client.SystemMessage($"Unable to add {playerToAdd.Username}. Your party is full.");
            playerToAdd.Client.SystemMessage($"{partyMember.Username}'s party is full.");
            return false;
        }

        if (playerToAdd.GroupId != 0 && playerToAdd.GroupId != partyMember.GroupId)
        {
            partyMember.Client.SystemMessage($"{playerToAdd.Username} belongs to another party.");
            playerToAdd.Client.SystemMessage($"{partyMember.Username}'s requested you to join his party. However you belong to another.");
            return false;
        }

        playerToAdd.GroupId = party.Id;
        party.PartyMembers.TryAdd(playerToAdd.Serial, playerToAdd);
        partyMember.Client.SystemMessage($"{playerToAdd.Username} has joined your party.");
        playerToAdd.Client.SystemMessage($"You have joined {party.LeaderName}'s party.");

        return true;
    }

    private static void CreateParty(Aisling partyLeader)
    {
        var party = new Party { LeaderName = partyLeader.Username };
        var pendingId = EphemeralRandomIdGenerator<int>.Shared.NextId;
        party.Id = pendingId;
        party.LeaderName = partyLeader.Username;
        partyLeader.GroupId = party.Id;
        party.PartyMembers.TryAdd(partyLeader.Serial, partyLeader);

        if (!ServerSetup.Instance.GlobalGroupCache.ContainsKey(party.Id))
        {
            ServerSetup.Instance.GlobalGroupCache.TryAdd(party.Id, party);
        }
        else
        {
            var pendingId2 = EphemeralRandomIdGenerator<int>.Shared.NextId;
            party.Id = pendingId2;
            partyLeader.GroupId = party.Id;
            ServerSetup.Instance.GlobalGroupCache.TryAdd(party.Id, party);
        }

        partyLeader.Client.SystemMessage("You have formed a party.");
    }

    public static void DisbandParty(Party group)
    {
        foreach (var player in group.PartyMembers.Values)
        {
            player.GroupId = 0;
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The party has now been disbanded.");
        }

        if (!ServerSetup.Instance.GlobalGroupCache.ContainsKey(group.Id)) return;
        if (!ServerSetup.Instance.GlobalGroupCache.TryRemove(group.Id, out _)) return;
    }

    public static void RemovePartyMember(Aisling playerToRemove)
    {
        if (playerToRemove == null) return;
        if (!ServerSetup.Instance.GlobalGroupCache.TryGetValue(playerToRemove.GroupId, out var group)) return;

        foreach (var player in group.PartyMembers.Values)
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{playerToRemove.Username} has left the party.");

        playerToRemove.GroupId = 0;
        group.PartyMembers.TryRemove(playerToRemove.Serial, out _);

        if (group.PartyMembers.Count <= 1)
        {
            DisbandParty(group);
        }
        else
        {
            if (playerToRemove.Username != group.LeaderName) return;

            var nextPlayer = group.PartyMembers.Values.FirstOrDefault();
            if (nextPlayer == null) return;

            group.LeaderName = nextPlayer.Username;

            foreach (var player in group.PartyMembers.Values)
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{nextPlayer.Username} is now the party leader.");
        }
    }

    public bool Has(Aisling that)
    {
        return Id == that.GroupId;
    }
}