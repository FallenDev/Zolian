using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Networking.Abstractions;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using System.Diagnostics;
using Chaos.Networking.Entities.Server;
using Darkages.Network.Server;

namespace Darkages.Network.Client.Abstractions;

public interface IWorldClient : IConnectedClient
{
    ServerPacketLogger ServerPacketLogger { get; }
    ClientPacketLogger ClientPacketLogger { get; }
    bool MapUpdating { get; set; }
    Aisling Aisling { get; set; }
    DialogSession DlgSession { get; set; }
    bool IsRefreshing { get; }
    bool IsEquipping { get; }
    CastInfo SpellCastInfo { get; set; }
    DateTime LastAssail { get; set; }
    DateTime LastSpellCast { get; set; }
    DateTime LastItemUsed { get; set; }
    DateTime LastSelfProfileRequest { get; set; }
    DateTime LastWorldListRequest { get; set; }
    DateTime LastClientRefresh { get; set; }
    DateTime LastMapUpdated { get; set; }
    DateTime LastMessageSent { get; set; }
    DateTime LastMovement { get; set; }
    DateTime LastEquip { get; set; }
    Stopwatch Latency { get; set; }
    DateTime LastWhisperMessageSent { get; set; }
    uint EntryCheck { get; set; }
    void LoadSkillBook();
    void LoadSpellBook();
    void SendAnimation(ushort targetEffect, Position position = null, uint targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint casterSerial = 0);
    void SendAttributes(StatUpdateType statUpdateType);
    bool SendBoard(BoardTemplate board);
    bool SendMailBox();
    bool SendPost(PostTemplate post, bool isMail, bool enablePrevBtn = true);
    void SendBoardResponse(BoardOrResponseType responseType, string message, bool success);
    void SendBodyAnimation(uint id, BodyAnimation bodyAnimation, ushort speed, byte? sound = null);
    void SendCancelCasting();
    void SendConfirmExit();
    void SendCooldown(bool skill, byte slot, int cooldownSeconds);
    void SendCreatureWalk(uint id, Point startPoint, Direction direction);
    void SendExchangeStart(Aisling fromAisling);
    void SendDisplayGroupInvite(ServerGroupSwitch serverGroupSwitch, string fromName, DisplayGroupBoxInfo groupBoxInfo = null);
    void SendHealthBar(Sprite creature, byte? sound = null);
    void SendLocation();
    void SendMapData();
    void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string name = null);
    void SendProfile(Aisling aisling);
    void SendPublicMessage(uint id, PublicMessageType publicMessageType, string message);
    void SendRefreshResponse();
    void SendRemoveItemFromPane(byte slot);
    void SendSelfProfile();
    void SendServerMessage(ServerMessageType serverMessageType, string message);
    void SendSound(byte sound, bool isMusic);
    void SendWorldList(IEnumerable<Aisling> users);
    WorldClient AislingToGhostForm();
    void ClientRefreshed();
    WorldClient SystemMessage(string message);
    Task<bool> Save();
    WorldClient UpdateDisplay(bool excludeSelf = false);
    void Interrupt();
    WorldClient LoggedIn(bool state);
    void CheckWarpTransitions(WorldClient client);
    void AddToIgnoreListDb(string ignored);
    void RemoveFromIgnoreListDb(string ignored);
}