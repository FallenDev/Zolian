using System.Net.Sockets;
using Chaos.Common.Definitions;
using Chaos.Extensions.Networking;
using Chaos.Cryptography.Abstractions;
using Chaos.Geometry;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Sprites;
using Darkages.Types;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Packets.Abstractions.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client.Abstractions;
using BodyColor = Chaos.Common.Definitions.BodyColor;
using EquipmentSlot = Chaos.Common.Definitions.EquipmentSlot;
using Gender = Chaos.Common.Definitions.Gender;
using LanternSize = Chaos.Common.Definitions.LanternSize;
using RestPosition = Chaos.Common.Definitions.RestPosition;
using BodySprite = Chaos.Common.Definitions.BodySprite;

namespace Darkages.Network.Client
{
    public class WorldClient : SocketClientBase, IWorldClient
    {
        private readonly IWorldServer<WorldClient> _server;
        public Aisling Aisling { get; set; }

        public WorldClient([NotNull] IWorldServer<WorldClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
            [NotNull] ILogger<SocketClientBase> logger) : base(socket, crypto, packetSerializer, logger)
        {
            _server = server;
        }

        protected override ValueTask HandlePacketAsync(Span<byte> span)
        {
            var opCode = span[3];
            var isEncrypted = Crypto.ShouldBeEncrypted(opCode);
            var packet = new ClientPacket(ref span, isEncrypted);

            if (isEncrypted)
                Crypto.Decrypt(ref packet);

            return _server.HandlePacketAsync(this, in packet);
        }

        /// <summary>
        /// 0x0F - Add Inventory
        /// </summary>
        public void SendAddItemToPane([NotNull]Item item)
        {
            var args = new AddItemToPaneArgs
            {
                Item = new ItemInfo
                {
                    Color = (DisplayColor)item.Color,
                    Cost = (int?)item.Template.Value,
                    Count = item.Stacks,
                    CurrentDurability = (int)item.Durability,
                    MaxDurability = (int)item.MaxDurability,
                    Name = item.Name,
                    Slot = item.Slot,
                    Sprite = item.DisplayImage,
                    Stackable = item.Template.CanStack
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x2C - Add Skill
        /// </summary>
        public void SendAddSkillToPane([NotNull]Skill skill)
        {
            var args = new AddSkillToPaneArgs
            {
                Skill = new SkillInfo
                {
                    Name = skill.SkillName,
                    PanelName = skill.Name,
                    Slot = skill.Slot,
                    Sprite = skill.Icon
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x17 - Add Spell
        /// </summary>
        public void SendAddSpellToPane([NotNull]Spell spell)
        {
            var args = new AddSpellToPaneArgs
            {
                Spell = new SpellInfo
                {
                    Name = spell.SpellName,
                    PanelName = spell.Name,
                    Slot = spell.Slot,
                    Sprite = spell.Icon,
                    CastLines = (byte)spell.Lines,
                    Prompt = string.Empty,
                    SpellType = (SpellType)spell.Template.TargetType
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x29 - Animation
        /// </summary>
        public void SendAnimation(ushort casterEffect, ushort targetEffect, short speed, uint casterSerial = 0, uint targetSerial = 0, [CanBeNull]Position position = null)
        {
            Point? point;

            if (position is null)
                point = null;
            else
                point = new Point(position.X, position.Y);

            var args = new AnimationArgs
            {
                AnimationSpeed = (ushort)speed,
                SourceAnimation = casterEffect,
                SourceId = casterSerial,
                TargetAnimation = targetEffect,
                TargetId = targetSerial,
                TargetPoint = point
            };

            Send(args);
        }

        /// <summary>
        /// 0x08 - Attributes
        /// </summary>
        public void SendAttributes(StatUpdateType statUpdateType)
        {
            var args = new AttributesArgs
            {
                Ability = (byte)Aisling.AbpLevel,
                Ac = (sbyte)Aisling.Ac,
                Blind = Aisling.IsBlind,
                Con = (byte)Aisling.Con,
                CurrentHp = (uint)Aisling.CurrentHp,
                CurrentMp = (uint)Aisling.CurrentMp,
                CurrentWeight = (short)Aisling.CurrentWeight,
                DefenseElement = (Element)Aisling.DefenseElement,
                Dex = (byte)Aisling.Dex,
                Dmg = Aisling.Dmg,
                GamePoints = Aisling.GamePoints,
                Gold = Aisling.GoldPoints,
                Hit = Aisling.Hit,
                Int = (byte)Aisling.Int,
                IsAdmin = Aisling.GameMaster,
                Level = (byte)Aisling.ExpLevel,
                MagicResistance = Aisling.Mr,
                MailFlags = MailFlag.None,
                MaximumHp = (uint)Aisling.MaximumHp,
                MaximumMp = (uint)Aisling.MaximumMp,
                MaxWeight = (short)Aisling.MaximumWeight,
                OffenseElement = (Element)Aisling.OffenseElement,
                StatUpdateType = statUpdateType,
                Str = (byte)Aisling.Str,
                ToNextAbility = Aisling.AbpNext,
                ToNextLevel = (uint)Aisling.ExpNext,
                TotalAbility = Aisling.AbpTotal,
                TotalExp = Aisling.ExpTotal,
                UnspentPoints = (byte)Aisling.StatPoints,
                Wis = (byte)Aisling.Wis
            };

            Send(args);
        }

        /// <summary>
        /// 0x31 - Show Board
        /// </summary>
        public void SendBoard()
        {
            //var packet = ServerPacketEx.FromData(
            //    ServerOpCode.BulletinBoard,
            //    PacketSerializer.Encoding,
            //    1,
            //    0);

            //Send(ref packet);
        }

        /// <summary>
        /// 0x1A - Player Body Animation
        /// </summary>
        public void SendBodyAnimation(uint id, BodyAnimation bodyAnimation, ushort speed, byte? sound = null)
        {
            if (bodyAnimation is BodyAnimation.None) return;

            var args = new BodyAnimationArgs
            {
                SourceId = id,
                BodyAnimation = bodyAnimation,
                Sound = sound,
                AnimationSpeed = speed
            };

            Send(args);
        }

        /// <summary>
        /// 0x48 - Cancel Casting
        /// </summary>
        public void SendCancelCasting()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.CancelCasting, PacketSerializer.Encoding);
            Send(ref packet);
        }

        /// <summary>
        /// 0x0B - Player Move
        /// </summary>
        public void SendConfirmClientWalk(Position oldPoint, Direction direction)
        {
            var args = new ConfirmClientWalkArgs
            {
                Direction = direction,
                OldPoint = new Point(oldPoint.X, oldPoint.Y)
            };

            Send(args);
        }

        public void SendConfirmExit()
        {
            var args = new ConfirmExitArgs
            {
                ExitConfirmed = true
            };

            Send(args);
        }

        /// <summary>
        /// 0x3F - Cooldown
        /// </summary>
        public void SendCooldown(bool skill, byte slot, int cooldownSeconds)
        {
            var args = new CooldownArgs
            {
                IsSkill = skill,
                Slot = slot,
                CooldownSecs = (uint)cooldownSeconds
            };

            Send(args);
        }

        /// <summary>
        /// 0x11 - Sprite Direction
        /// </summary>
        public void SendCreatureTurn(uint id, Direction direction)
        {
            var args = new CreatureTurnArgs
            {
                SourceId = id,
                Direction = direction
            };

            Send(args);
        }

        /// <summary>
        /// 0x0C - NPC Move
        /// </summary>
        public void SendCreatureWalk(uint id, Point startPoint, Direction direction)
        {
            var args = new CreatureWalkArgs
            {
                SourceId = id,
                OldPoint = startPoint,
                Direction = direction
            };

            Send(args);
        }

        /// <summary>
        /// 0x2F - Send Dialog
        /// </summary>
        public void SendDialog(Dialog dialog)
        {
            if (dialog == null) return;
            var args = new DialogArgs
            {
                Color = DisplayColor.Default,
                DialogId = 0,
                DialogType = DialogType.Normal,
                EntityType = (EntityType)0,
                HasNextButton = false,
                HasPreviousButton = false,
                Name = null,
                Options = null,
                PursuitId = null,
                SourceId = null,
                Sprite = 0,
                Text = null,
                TextBoxLength = null
            };

            Send(args);
        }

        /// <summary>
        /// 0x33 - Display Player
        /// </summary>
        public void SendDisplayAisling(Aisling aisling)
        {
            var args = new DisplayAislingArgs
            {
                AccessoryColor1 = (DisplayColor)aisling.Accessory1Color,
                AccessoryColor2 = (DisplayColor)aisling.Accessory2Color,
                AccessoryColor3 = (DisplayColor)aisling.Accessory3Color,
                AccessorySprite1 = (ushort)aisling.Accessory1Img,
                AccessorySprite2 = (ushort)aisling.Accessory2Img,
                AccessorySprite3 = (ushort)aisling.Accessory3Img,
                ArmorSprite1 = (ushort)aisling.ArmorImg,
                ArmorSprite2 = (ushort)aisling.ArmorImg,
                BodyColor = (BodyColor)aisling.BodyColor,
                BodySprite = (BodySprite)aisling.BodySprite,
                BootsColor = (DisplayColor)aisling.BootColor,
                BootsSprite = (byte)aisling.BootsImg,
                Direction = (Direction)aisling.Direction,
                FaceSprite = aisling.FaceSprite,
                Gender = (Gender)aisling.Gender,
                GroupBoxText = null, // Not built out
                HeadColor = (DisplayColor)aisling.HairColor,
                HeadSprite = aisling.HairStyle,
                Id = (uint)aisling.Serial,
                IsDead = aisling.IsDead(),
                IsHidden = false, // Not sure the difference between hidden and transparent (perhaps GM hide?)
                IsTransparent = aisling.Invisible,
                LanternSize = (LanternSize)aisling.Lantern,
                Name = aisling.Username,
                NameTagStyle = (NameTagStyle)aisling.NameStyle,
                OvercoatColor = (DisplayColor)aisling.OverCoatColor,
                OvercoatSprite = (ushort)aisling.OverCoatImg,
                RestPosition = (RestPosition)aisling.Resting,
                ShieldSprite = (byte)aisling.ShieldImg,
                Sprite = aisling.MonsterForm,
                WeaponSprite = (ushort)aisling.WeaponImg,
                X = aisling.X,
                Y = aisling.Y
            };

            //we can always see ourselves, and we're never hostile to ourself
            if (!Aisling.Equals(aisling))
            {
                if (Aisling.Map.Flags.MapFlagIsSet(Darkages.Enums.MapFlags.PlayerKill))
                    args.NameTagStyle = NameTagStyle.Hostile;
                //if we're not an admin, and the aisling is not visible
                if (!Aisling.GameMaster && aisling.Invisible)
                {
                    //remove the name
                    args.Name = string.Empty;

                    //if we cant see the aisling, hide it (it is otherwise transparent)
                    if (!Aisling.CanSeeInvisible)
                        args.IsHidden = true;
                }
            }

            Send(args);
        }

        //public void SendDoors(IEnumerable<Door> doors)
        //{
        //    var args = new DoorArgs
        //    {
        //        Doors = Mapper.MapMany<DoorInfo>(doors).ToList()
        //    };

        //    if (args.Doors.Any())
        //        Send(args);
        //}

        /// <summary>
        /// 0x3A - Effect Duration
        /// </summary>
        public void SendEffect(EffectColor effectColor, byte effectIcon)
        {
            var args = new EffectArgs
            {
                EffectColor = effectColor,
                EffectIcon = effectIcon
            };

            Send(args);
        }

        /// <summary>
        /// 0x37 - Add Equipment
        /// </summary>
        public void SendEquipment(Item item)
        {
            var args = new EquipmentArgs
            {
                Slot = (EquipmentSlot)item.Slot,
                Item = new ItemInfo
                {
                    Color = (DisplayColor)item.Color,
                    Cost = (int?)item.Template.Value,
                    Count = item.Stacks,
                    CurrentDurability = (int)item.Durability,
                    MaxDurability = (int)item.MaxDurability,
                    Name = item.Name,
                    Slot = item.Slot,
                    Sprite = item.DisplayImage,
                    Stackable = item.Template.CanStack
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Accept Exchange 
        /// </summary>
        public void SendExchangeAccepted(bool persistExchange)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.Accept,
                PersistExchange = persistExchange
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Add Item to Exchange 
        /// </summary>
        public void SendExchangeAddItem(bool rightSide, byte index, Item item)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.AddItem,
                RightSide = rightSide,
                ExchangeIndex = index,
                ItemSprite = item.Template.DisplayImage,
                ItemColor = (DisplayColor?)item.Template.Color,
                ItemName = item.DisplayName
            };

            if (item.Stacks > 1)
                args.ItemName = $"{item.DisplayName} [{item.Stacks}]";

            Send(args);
        }

        /// <summary>
        /// 0x42 - Cancel Exchange 
        /// </summary>
        public void SendExchangeCancel(bool rightSide)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.Cancel,
                RightSide = rightSide
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Request Gold Exchange 
        /// </summary>
        public void SendExchangeRequestAmount(byte slot)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.RequestAmount,
                FromSlot = slot
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Add Gold to Exchange 
        /// </summary>
        public void SendExchangeSetGold(bool rightSide, int amount)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.SetGold,
                RightSide = rightSide,
                GoldAmount = amount
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Start Exchange 
        /// </summary>
        public void SendExchangeStart(Aisling fromAisling)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.StartExchange,
                OtherUserId = (uint?)fromAisling.Serial,
                OtherUserName = fromAisling.Username
            };

            Send(args);
        }

        public void SendForcedClientPacket(ref ClientPacket clientPacket) => throw new NotImplementedException();

        /// <summary>
        /// 0x63 - Group Request
        /// </summary>
        public void SendGroupRequest(GroupRequestType groupRequestType, string fromName)
        {
            var args = new GroupRequestArgs
            {
                GroupRequestType = groupRequestType,
                SourceName = fromName
            };

            Send(args);
        }

        /// <summary>
        /// 0x13 - Health Bar
        /// </summary>
        public void SendHealthBar(Sprite creature, byte? sound = null)
        {
            var args = new HealthBarArgs
            {
                SourceId = (uint)creature.Serial,
                HealthPercent = (byte)((double)100 * creature.CurrentHp / creature.MaximumHp),
                Sound = sound
            };

            Send(args);
        }

        /// <summary>
        /// 0x20 - Change Hour (Night - Day)
        /// </summary>
        /// <param name="lightLevel">
        /// Darkest = 0,
        /// Darker = 1,
        /// Dark = 2,
        /// Light = 3,
        /// Lighter = 4,
        /// Lightest = 5
        /// </param>
        public void SendLightLevel(LightLevel lightLevel)
        {
            var args = new LightLevelArgs
            {
                LightLevel = lightLevel
            };

            Send(args);
        }

        /// <summary>
        /// 0x04 - Location
        /// </summary>
        public void SendLocation()
        {
            var args = new LocationArgs
            {
                X = Aisling.X,
                Y = Aisling.Y
            };

            Send(args);
        }

        /// <summary>
        /// 0x1F - Map Change Complete
        /// </summary>
        public void SendMapChangeComplete()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.MapChangeComplete, PacketSerializer.Encoding, new byte[2]);

            Send(ref packet);
        }

        /// <summary>
        /// 0x67 - Map Change Pending
        /// </summary>
        public void SendMapChangePending()
        {
            var packet = ServerPacketEx.FromData(
                ServerOpCode.MapChangePending,
                PacketSerializer.Encoding,
                3,
                0,
                0,
                0,
                0,
                0);

            Send(ref packet);
        }

        /// <summary>
        /// 0x3C - Map Data
        /// </summary>
        public void SendMapData()
        {
            var mapTemplate = Aisling.Map;

            for (byte y = 0; y < mapTemplate.Rows; y++)
            {
                var args = new MapDataArgs
                {
                    CurrentYIndex = y,
                    Width = (byte)mapTemplate.Cols,
                    MapData = Enumerable.ToArray(mapTemplate.GetRowData(y))
                };

                Send(args);
            }
        }

        /// <summary>
        /// 0x15 - Map Information
        /// </summary>
        public void SendMapInfo()
        {
            var args = new MapInfoArgs
            {
                CheckSum = Aisling.Map.Hash,
                Flags = (byte)Aisling.Map.Flags,
                Height = (byte)Aisling.Map.Cols,
                MapId = (short)Aisling.Map.ID,
                Name = Aisling.Map.Name,
                Width = (byte)Aisling.Map.Rows
            };

            Send(args);
        }

        /// <summary>
        /// 0x58 - Map Load Complete
        /// </summary>
        public void SendMapLoadComplete()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.MapLoadComplete, PacketSerializer.Encoding, 0);

            Send(ref packet);
        }

        /// <summary>
        /// 0x6F - MapData Send
        /// </summary>
        public void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore,
            string? name = null)
        {
            var args = new MetaDataArgs
            {
                MetaDataRequestType = metaDataRequestType
            };

            switch (metaDataRequestType)
            {
                case MetaDataRequestType.DataByName:
                {
                    ArgumentNullException.ThrowIfNull(name);
                    var metaData = metaDataStore.GetMetaFile(name);
                    args.MetaDataInfo = new MetaDataInfo
                    {
                        Name = metaData.Name,
                        Data = metaData.DeflatedData,
                        CheckSum = metaData.Hash
                    };
                    break;
                }
                case MetaDataRequestType.AllCheckSums:
                {
                    args.MetaDataCollection = metaDataStore.GetMetaFiles().Select(i => new MetaDataInfo
                    {
                        Name = i.Name,
                        Data = i.DeflatedData,
                        CheckSum = i.Hash
                    }).ToList();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(metaDataRequestType), metaDataRequestType,
                        "Unknown enum value");
            }

            Send(args);
        }

        public void SendNotepad(byte identifier, NotepadType type, byte height, byte width, string? message)
        {
            var args = new NotepadArgs
            {
                Slot = identifier,
                NotepadType = type,
                Height = height,
                Width = width,
                Message = message ?? string.Empty
            };

            Send(args);
        }

        /// <summary>
        /// 0x34 - Player Profile
        /// </summary>
        /// <param name="aisling">Target Player</param>
        public void SendProfile(Aisling aisling)
        {
            var playerClass = aisling.Path switch
            {
                Class.Peasant => 0,
                Class.Berserker => 1,
                Class.Defender => 1,
                Class.Assassin => 2,
                Class.Cleric => 3,
                Class.Arcanus => 4,
                Class.Monk => 5
            };

            var equipment = new Dictionary<EquipmentSlot, ItemInfo>();
            var partyOpen = aisling.PartyStatus == (GroupStatus)1;
            var legendMarks = new List<LegendMarkInfo>();

            foreach (var slot in aisling.EquipmentManager.Equipment.Values)
            {
                if (slot == null) continue;
                var item = new ItemInfo
                {
                    Color = (DisplayColor)slot.Item.Color,
                    Cost = (int?)slot.Item.Template.Value,
                    Count = slot.Item.Stacks,
                    CurrentDurability = (int)slot.Item.Durability,
                    MaxDurability = (int)slot.Item.MaxDurability,
                    Name = slot.Item.Name,
                    Slot = slot.Item.Slot,
                    Sprite = slot.Item.DisplayImage,
                    Stackable = slot.Item.Template.CanStack
                };

                equipment.Add((EquipmentSlot)slot.Slot, item);
            }

            foreach (var legendItem in aisling.LegendBook.LegendMarks)
            {
                if (legendItem == null) continue;
                var legends = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendItem.Value
                };

                legendMarks.Add(legends);
            }

            var args = new ProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)playerClass,
                Equipment = equipment,
                GroupOpen = partyOpen,
                GuildName = aisling.Clan,
                GuildRank = aisling.ClanRank,
                Id = (uint)aisling.Serial,
                LegendMarks = legendMarks,
                Name = aisling.Username,
                Nation = Nation.Mileth,
                Portrait = aisling.PictureData,
                ProfileText = aisling.ProfileMessage,
                SocialStatus = (SocialStatus)aisling.ActiveStatus,
                Title = aisling.ClanTitle
            };

            Send(args);
        }

        /// <summary>
        /// 0x49 - Request Portrait
        /// </summary>
        public void SendProfileRequest()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.ProfileRequest, PacketSerializer.Encoding);

            Send(ref packet);
        }

        /// <summary>
        /// 0x0A - Message
        /// </summary>
        /// <param name="sourceId">Sprite Serial</param>
        /// <param name="publicMessageType">Message Type</param>
        /// <param name="message">Value</param>
        public void SendPublicMessage(uint sourceId, PublicMessageType publicMessageType, string message)
        {
            var args = new PublicMessageArgs
            {
                SourceId = sourceId,
                PublicMessageType = publicMessageType,
                Message = message
            };

            Send(args);
        }

        /// <summary>
        /// 0x22 - Client Refresh
        /// </summary>
        public void SendRefreshResponse()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.RefreshResponse, PacketSerializer.Encoding);

            Send(ref packet);
        }

        /// <summary>
        /// 0x10 - Remove Item from Inventory
        /// </summary>
        /// <param name="slot"></param>
        public void SendRemoveItemFromPane(byte slot)
        {
            var args = new RemoveItemFromPaneArgs
            {
                Slot = slot
            };

            Send(args);
        }

        /// <summary>
        /// 0x0E - Remove World Object
        /// </summary>
        /// <param name="id"></param>
        public void SendRemoveObject(uint id)
        {
            var args = new RemoveObjectArgs
            {
                SourceId = id
            };

            Send(args);
        }

        /// <summary>
        /// 0x2D - Remove Skill
        /// </summary>
        /// <param name="slot"></param>
        public void SendRemoveSkillFromPane(byte slot)
        {
            var args = new RemoveSkillFromPaneArgs
            {
                Slot = slot
            };

            Send(args);
        }

        /// <summary>
        /// 0x18 - Remove Spell
        /// </summary>
        /// <param name="slot"></param>
        public void SendRemoveSpellFromPane(byte slot)
        {
            var args = new RemoveSpellFromPaneArgs
            {
                Slot = slot
            };

            Send(args);
        }

        /// <summary>
        /// 0x39 - Self Profile
        /// </summary>
        public void SendSelfProfile()
        {
            if (Aisling.ProfileOpen) return;

            var playerClass = Aisling.Path switch
            {
                Class.Peasant => 0,
                Class.Berserker => 1,
                Class.Defender => 1,
                Class.Assassin => 2,
                Class.Cleric => 3,
                Class.Arcanus => 4,
                Class.Monk => 5
            };

            var equipment = new Dictionary<EquipmentSlot, ItemInfo>();
            var partyOpen = Aisling.PartyStatus == (GroupStatus)1;
            var legendMarks = new List<LegendMarkInfo>();

            foreach (var slot in Aisling.EquipmentManager.Equipment.Values)
            {
                if (slot == null) continue;
                var item = new ItemInfo
                {
                    Color = (DisplayColor)slot.Item.Color,
                    Cost = (int?)slot.Item.Template.Value,
                    Count = slot.Item.Stacks,
                    CurrentDurability = (int)slot.Item.Durability,
                    MaxDurability = (int)slot.Item.MaxDurability,
                    Name = slot.Item.Name,
                    Slot = slot.Item.Slot,
                    Sprite = slot.Item.DisplayImage,
                    Stackable = slot.Item.Template.CanStack
                };

                equipment.Add((EquipmentSlot)slot.Slot, item);
            }

            foreach (var legendItem in Aisling.LegendBook.LegendMarks)
            {
                if (legendItem == null) continue;
                var legends = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendItem.Value
                };

                legendMarks.Add(legends);
            }
            

            var args = new SelfProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)playerClass,
                Equipment = equipment,
                GroupOpen = partyOpen,
                GroupString = Aisling.PartyMembers.ToString(),
                GuildName = Aisling.Clan,
                GuildRank = Aisling.ClanRank,
                IsMaster = Aisling.Stage >= ClassStage.Master,
                LegendMarks = legendMarks,
                Name = Aisling.Username,
                Nation = Nation.Mileth,
                Portrait = Aisling.PictureData,
                ProfileText = Aisling.ProfileMessage,
                SocialStatus = (SocialStatus)Aisling.ActiveStatus,
                SpouseName = null,
                Title = Aisling.ClanTitle
            };

            Send(args);
        }


        public void SendServerMessage(ServerMessageType serverMessageType, string message)
        {
            var args = new ServerMessageArgs
            {
                ServerMessageType = serverMessageType,
                Message = message
            };

            Send(args);
        }

        /// <summary>
        /// 0x19 - Send Sound
        /// </summary>
        /// <param name="sound">Sound Number</param>
        /// <param name="isMusic">Whether or not the sound is a song</param>
        public void SendSound(byte sound, bool isMusic)
        {
            var args = new SoundArgs
            {
                Sound = sound,
                IsMusic = isMusic
            };

            Send(args);
        }

        /// <summary>
        /// 0x38 - Remove Equipment
        /// </summary>
        /// <param name="equipmentSlot"></param>
        public void SendUnequip(EquipmentSlot equipmentSlot)
        {
            var args = new UnequipArgs
            {
                EquipmentSlot = equipmentSlot
            };

            Send(args);
        }

        /// <summary>
        /// 0x05 - UserID, Direction, Rogue Map, Gender
        /// </summary>
        public void SendUserId()
        {
            var args = new UserIdArgs
            {
                BaseClass = BaseClass.Rogue,
                Direction = (Direction)Aisling.Direction,
                Gender = (Gender)Aisling.Gender,
                Id = (uint)Aisling.Serial
            };

            Send(args);
        }

        /// <summary>
        /// 0x07 - Add World Objects
        /// </summary>
        /// <param name="objects">Objects that are visible to a player</param>
        public void SendVisibleEntities(List<Sprite> objects)
        {
            if (objects.Count <= 0) return;

            //split this into chunks so as not to crash the client
            foreach (var chunk in objects.OrderBy(o => o.AbandonedDate).Chunk(500))
            {
                var args = new DisplayVisibleEntitiesArgs();
                var visibleArgs = new List<VisibleEntityInfo>();
                args.VisibleObjects = visibleArgs;

                foreach (var obj in chunk)
                    switch (obj)
                    {
                        case Item groundItem:
                            var groundItemInfo = new GroundItemInfo
                            {
                                Id = (uint)groundItem.Serial,
                                Sprite = groundItem.Image,
                                X = groundItem.X,
                                Y = groundItem.Y,
                                Color = (DisplayColor)groundItem.Template.Color
                            };

                            //non visible item that can be seen
                            //if (groundItem.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(groundItem)))
                            //{
                            //    groundItemInfo.Sprite = 11978;
                            //    groundItemInfo.Color = DisplayColor.Black;
                            //}

                            visibleArgs.Add(groundItemInfo);

                            break;
                        case Money money:
                            var moneyInfo = new GroundItemInfo
                            {
                                Id = (uint)money.Serial,
                                Sprite = money.Image,
                                X = money.X,
                                Y = money.Y,
                                Color = DisplayColor.Default
                            };

                            //non visible money that can be seen
                            //if (money.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(money)))
                            //    moneyInfo.Sprite = 138;

                            visibleArgs.Add(moneyInfo);

                            break;
                        case Monster creature:
                            var creatureInfo = new CreatureInfo
                            {
                                Id = (uint)creature.Serial,
                                Sprite = creature.Template.Image,
                                X = creature.X,
                                Y = creature.Y,
                                CreatureType = CreatureType.Normal,
                                /*
                                 * Normal = 0
                                 * WalkThrough = 1
                                 * Merchant = 2
                                 * WhiteSquare = 3
                                 * User = 4 
                                 */
                                Direction = (Direction)creature.Direction,
                                Name = creature.Template.BaseName
                            };

                            //none visible creature that can be seen
                            //if (creature.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(creature)))
                            //    creatureInfo.Sprite = 405;

                            visibleArgs.Add(creatureInfo);

                            break;
                        case Mundane npc:
                            var npcInfo = new CreatureInfo
                            {
                                Id = (uint)npc.Serial,
                                Sprite = (ushort)npc.Template.Image,
                                X = npc.X,
                                Y = npc.Y,
                                CreatureType = CreatureType.Merchant,
                                Direction = (Direction)npc.Direction,
                                Name = npc.Template.Name
                            };

                            //none visible creature that can be seen
                            //if (creature.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(creature)))
                            //    creatureInfo.Sprite = 405;

                            visibleArgs.Add(npcInfo);
                            break;
                    }

                Send(args);
            }
        }

        /// <summary>
        /// 0x36 - World User List
        /// </summary>
        /// <param name="aislings"></param>
        public void SendWorldList(IEnumerable<Aisling> aislings)
        {
            var worldList = new List<WorldListMemberInfo>();
            var orderedAislings = aislings.OrderByDescending(aisling => aisling.BaseMp * 2 + aisling.BaseHp);

            var args = new WorldListArgs
            {
                WorldList = worldList
            };

            foreach (var aisling in orderedAislings)
            {
                var classList = aisling.Path switch
                {
                    Class.Peasant => 0,
                    Class.Berserker => 1,
                    Class.Assassin => 2, 
                    Class.Arcanus => 3, 
                    Class.Cleric => 4,
                    Class.Defender => 5,
                    Class.Monk => 6,
                    _ => 0
                };

                var arg = new WorldListMemberInfo
                {
                    BaseClass = (BaseClass)classList,
                    Color = (WorldListColor)GetUserColor(aisling),
                    IsMaster = aisling.Stage >= ClassStage.Master,
                    Name = aisling.Username,
                    SocialStatus = (SocialStatus)aisling.ActiveStatus,
                    Title = aisling.ClanTitle
                };

                worldList.Add(arg);
            }

            Send(args);
        }

        private ListColor GetUserColor(Player user)
        {
            var color = ListColor.White;
            if (Aisling.ExpLevel > user.ExpLevel)
                if (Aisling.ExpLevel - user.ExpLevel < 15)
                    color = ListColor.Orange;
            if (!string.IsNullOrEmpty(user.Clan) && user.Clan == Aisling.Clan)
                color = ListColor.Clan;
            if (user.GameMaster)
                color = ListColor.Red;
            if (user.Ranger)
                color = ListColor.Green;
            if (user.Knight)
                color = ListColor.Green;
            if (user.ArenaHost)
                color = ListColor.Teal;
            return color;
        }

        /// <summary>
        /// 0x2E - Send Field Map
        /// </summary>
        /// <param name="worldMap"></param>
        public void SendWorldMap()
        {
            var portal = ServerSetup.Instance.GlobalWorldMapTemplateCache[Aisling.World];
            var warpsList = new List<WorldMapNodeInfo>();

            foreach (var warp in portal.Portals.Where(warps => warps?.Destination != null))
            {
                var addWarp = new WorldMapNodeInfo
                {
                    Destination = new Location
                    {
                        Map = warp.Destination.AreaID.ToString(),
                        X = warp.Destination.Location.X,
                        Y = warp.Destination.Location.Y
                    },
                    ScreenPosition = new Point(warp.PointX, warp.PointY),
                    Text = warp.DisplayName,
                    UniqueId = (ushort)Generator.GenerateNumber()
                };

                warpsList.Add(addWarp);
            }

            var args = new WorldMapArgs
            {
                FieldIndex = (byte)portal.FieldNumber,
                FieldName = portal.Name,
                Nodes = warpsList
            };

            Send(args);
        }
    }
}
