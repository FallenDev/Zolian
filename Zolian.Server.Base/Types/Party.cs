﻿using System.Collections.Concurrent;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;

using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Types;

public class Party : ObjectManager
{
    private int Id { get; set; }
    public string LeaderName { get; set; }
    public ConcurrentDictionary<uint, Aisling> PartyMembers = [];

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

        if (partyMember.GroupId != 0)
        {
            if (partyMember.PartyMembers.Count >= 13)
            {
                partyMember.Client.SystemMessage($"Unable to add {playerToAdd.Username}. Your party is full.");
                playerToAdd.Client.SystemMessage($"{partyMember.Username}'s party is full.");
                return false;
            }

            if (playerToAdd.GroupId != 0 && playerToAdd.GroupId != partyMember.GroupId)
            {
                partyMember.Client.SystemMessage($"{playerToAdd.Username} belongs to another party, and was not able to join your party.");
                playerToAdd.Client.SystemMessage($"{partyMember.Username}'s requested you to join his party. However you belong to another party.");
                return false;
            }

            if (playerToAdd.GroupId != 0 || partyMember.GroupId == 0) return false;

            playerToAdd.GroupId = partyMember.GroupId;
            partyMember.PartyMembers.TryAdd(playerToAdd.Serial, playerToAdd);
            partyMember.Client.SystemMessage($"{playerToAdd.Username} has joined your party.");
            playerToAdd.Client.SystemMessage($"You have joined {partyMember.Username}'s party.");

            return true;
        }

        if (playerToAdd.GroupId != 0 && partyMember.GroupId == 0)
        {
            playerToAdd.Client.SystemMessage($"{partyMember.Username} belongs to another party, and was not able to join your party.");
            partyMember.Client.SystemMessage($"{playerToAdd}'s requested you to join his party. However you belong to another party.");
            return false;
        }

        if (playerToAdd.GroupId != 0 || partyMember.GroupId != 0) return false;

        var party = CreateParty(partyMember);
        playerToAdd.GroupId = party.Id;

        foreach (var player in party.PartyMembers.Values)
            player.Client.SystemMessage($"{playerToAdd.Username} has joined the party.");

        playerToAdd.Client.SystemMessage($"You have joined {partyMember.Username}'s party.");
        playerToAdd.GroupId = party.Id;
        partyMember.PartyMembers.TryAdd(partyMember.Serial, partyMember);
        party.PartyMembers.TryAdd(playerToAdd.Serial, playerToAdd);

        return true;
    }

    private static Party CreateParty(Aisling partyLeader)
    {
        if (partyLeader.GroupId != 0) return null;

        var party = new Party { LeaderName = partyLeader.Username };
        var pendingId = EphemeralRandomIdGenerator<int>.Shared.NextId;
        party.Id = pendingId;
        party.LeaderName = partyLeader.Username;
        partyLeader.GroupId = party.Id;

        if (!ServerSetup.Instance.GlobalGroupCache.ContainsKey(party.Id))
            ServerSetup.Instance.GlobalGroupCache.TryAdd(party.Id, party);

        return party;
    }

    public static void DisbandParty(Party group)
    {
        if (!ServerSetup.Instance.GlobalGroupCache.ContainsKey(group.Id)) return;
        if (!ServerSetup.Instance.GlobalGroupCache.TryRemove(group.Id, out _)) return;

        foreach (var player in group.PartyMembers.Values)
        {
            player.GroupId = 0;
            player.PartyMembers.TryRemove(player.Serial, out _);
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The party has now been disbanded.");
        }
    }

    public static void RemovePartyMember(Aisling playerToRemove)
    {
        if (playerToRemove == null) return;
        if (!ServerSetup.Instance.GlobalGroupCache.TryGetValue(playerToRemove.GroupId, out var group)) return;
        if (group == null) return;

        foreach (var player in group.PartyMembers.Values)
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{playerToRemove.Username} has left the party.");

        playerToRemove.GroupId = 0;
        playerToRemove.PartyMembers.TryRemove(playerToRemove.Serial, out _);

        if (group.PartyMembers.Count <= 1)
        {
            DisbandParty(group);
        }
        else
        {
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