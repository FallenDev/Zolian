using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Networking.Abstractions;
using Chaos.Packets;
using Darkages.Sprites;
using Darkages.Types;
using Chaos.Geometry.Abstractions.Definitions;
using Darkages.Meta;
using EquipmentSlot = Chaos.Common.Definitions.EquipmentSlot;

namespace Darkages.Network.Client.Abstractions;

public interface IWorldClient : ISocketClient
{
    Aisling Aisling { get; set; }
    void SendAddItemToPane(Item item);
    void SendAddSkillToPane(Skill skill);
    void SendAddSpellToPane(Spell spell);
    void SendAnimation(ushort casterEffect, ushort targetEffect, short speed, uint casterSerial = 0, uint targetSerial = 0, Position position = null);
    void SendAttributes(StatUpdateType statUpdateType);
    //void SendBoard();
    void SendBodyAnimation(uint id, BodyAnimation bodyAnimation, ushort speed, byte? sound = null);
    void SendCancelCasting();
    void SendConfirmClientWalk(Position oldPoint, Direction direction);
    void SendConfirmExit();
    void SendCooldown(bool skill, byte slot, int cooldownSeconds);
    void SendCreatureTurn(uint id, Direction direction);
    void SendCreatureWalk(uint id, Point startPoint, Direction direction);
    void SendDialog(Dialog dialog);
    void SendDisplayAisling(Aisling aisling);
    //void SendDoors(IEnumerable<Door> doors);
    void SendEffect(EffectColor effectColor, byte effectIcon);
    void SendEquipment(Item item);
    void SendExchangeAccepted(bool persistExchange);
    void SendExchangeAddItem(bool rightSide, byte index, Item item);
    void SendExchangeCancel(bool rightSide);
    void SendExchangeRequestAmount(byte slot);
    void SendExchangeSetGold(bool rightSide, int amount);
    void SendExchangeStart(Aisling fromAisling);
    void SendForcedClientPacket(ref ClientPacket clientPacket);
    void SendGroupRequest(GroupRequestType groupRequestType, string fromName);
    void SendHealthBar(Sprite creature, byte? sound = null);
    void SendLightLevel(LightLevel lightLevel);
    void SendLocation();
    void SendMapChangeComplete();
    void SendMapChangePending();
    void SendMapData();
    void SendMapInfo();
    void SendMapLoadComplete();
    void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string? name = null);
    void SendNotepad(byte identifier, NotepadType type, byte height, byte width, string message);
    void SendProfile(Aisling aisling);
    void SendProfileRequest();
    void SendPublicMessage(uint id, PublicMessageType publicMessageType, string message);
    void SendRefreshResponse();
    void SendRemoveItemFromPane(byte slot);
    void SendRemoveObject(uint id);
    void SendRemoveSkillFromPane(byte slot);
    void SendRemoveSpellFromPane(byte slot);
    void SendSelfProfile();
    void SendServerMessage(ServerMessageType serverMessageType, string message);
    void SendSound(byte sound, bool isMusic);
    void SendUnequip(EquipmentSlot equipmentSlot);
    void SendUserId();
    void SendVisibleEntities(List<Sprite> objects);
    void SendWorldList(IEnumerable<Aisling> users);
    void SendWorldMap();
}