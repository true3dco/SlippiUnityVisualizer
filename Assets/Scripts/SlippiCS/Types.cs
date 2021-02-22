using System;
using System.Collections.Generic;

namespace SlippiCS
{
    public enum Command
    {
        MESSAGE_SIZES = 0x35,
        GAME_START = 0x36,
        PRE_FRAME_UPDATE = 0x37,
        POST_FRAME_UPDATE = 0x38,
        GAME_END = 0x39,
        ITEM_UPDATE = 0x3b,
        FRAME_BOOKEND = 0x3c,
    }

    public interface DeserializableFromUbjson
    {
        void DeserializeFromUbJson(object payload);
    }

    public interface IEventPayloadType { }

    // FIXME: Remove DeserializeFromUbJson except for Metadata type?
    public class PlayerType : DeserializableFromUbjson
    {
        public int PlayerIndex;
        public int Port;
        public int? CharacterId;
        public int? CharacterColor;
        public int? StartStocks;
        public int? Type;
        public int? TeamId;
        public string ControllerFix;
        public string NameTag;

        public void DeserializeFromUbJson(object payload)
        {
            if (!(payload is Dictionary<string, object>))
            {
                throw new ArgumentException($"Bad payload {payload}");
            }

            var data = payload as Dictionary<string, object>;
            PlayerIndex = (data["playerIndex"] as int?).Value;
            Port = (data["port"] as int?).Value;
            CharacterId = data["characterId"] as int?;
            CharacterColor = data["characterColor"] as int?;
            StartStocks = data["startStocks"] as int?;
            Type = data["type"] as int?;
            TeamId = data["teamId"] as int?;
            ControllerFix = data["controllerFix"] as string;
            NameTag = data["nameTag"] as string;
        }
    }

    public enum GameMode
    {
        VS = 0x02,
        ONLINE = 0x08,
    }

    public class GameStartType : IEventPayloadType, DeserializableFromUbjson
    {
        public string SlpVersion;
        public bool? IsTeams;
        public bool? IsPAL;
        public int? StageId;
        public List<PlayerType> Players;
        public int? Scene;
        public GameMode? GameMode;

        public void DeserializeFromUbJson(object payload)
        {
            if (!(payload is Dictionary<string, object>))
            {
                throw new ArgumentException($"Bad payload {payload}");
            }

            var data = payload as Dictionary<string, object>;
            if (data.ContainsKey("slpVersion"))
            {
                SlpVersion = data["slpVersion"] as string;
            }
            if (data.ContainsKey("isTeams"))
            {
                IsTeams = data["isTeams"] as bool?;
            }
            if (data.ContainsKey("isPAL"))
            {
                IsPAL = data["isPAL"] as bool?;
            }
            if (data.ContainsKey("stageId"))
            {
                StageId = data["stageId"] as int?;
            }

            if (Players == null)
            {
                Players = new List<PlayerType>();
            }
            Players.Clear();
            var playersData = data["players"] as object[];
            for (var i = 0; i < playersData.Length; i++)
            {
                var playerData = playersData[i];
                if (!(playerData is Dictionary<string, object>))
                {
                    throw new ArgumentException($"Malformed players data at index {i}: {playerData}");
                }
                var playerType = new PlayerType();
                playerType.DeserializeFromUbJson(playerData);
                Players.Add(playerType);
            }

            if (data.ContainsKey("scene"))
            {
                Scene = data["scene"] as int?;
            }
            if (data.ContainsKey("gameMode"))
            {
                var rawGameMode = data["gameMode"] as int?;
                // gameMode should be set if it has a key(?)
                GameMode = (GameMode)rawGameMode.Value;
            }
        }
    }

    public class PreFrameUpdateType : IEventPayloadType
    {
        // NOTE: Some of these are probs floats and will have to change.
        public int? Frame;
        public int? PlayerIndex;
        public bool? IsFollower;
        public int? Seed;
        public int? ActionStateId;
        public int? PositionX;
        public int? PositionY;
        public int? FacingDirection;
        public int? JoystickX;
        public int? JoystickY;
        public int? CStickX;
        public int? CStickY;
        public int? Trigger;
        public int? Buttons;
        public int? PhysicalButtons;
        public int? PhysicalLTrigger;
        public int? PhysicalRTrigger;
        public int? Percent;
    }

    public class PostFrameUpdateType : IEventPayloadType
    {
        // NOTE: Same as above
        public int? Frame;
        public int? PlayerIndex;
        public int? IsFollower;
        public int? InternalCharacterId;
        public int? ActionStateId;
        public int? PositionX;
        public int? PositionY;
        public int? FacingDirection;
        public int? Percent;
        public int? ShieldSize;
        public int? LastAttackLanded;
        public int? CurrentComboCount;
        public int? LastHitBy;
        public int? StocksRemaining;
        public int? ActionStateCounter;
        public int? MiscActionState;
        public bool? IsAirborne;
        public int? LastGroundId;
        public int? JumpsRemaining;
        public int? LCancelStatus;
        public int? HurtboxCollisionState;
        public SelfInducedSpeedsType selfInducedSpeeds; 
    }

    public class SelfInducedSpeedsType
    {
        public int? AirX;
        public int? Y;
        public int? AttackX;
        public int? AttackY;
        public int? GroundX;
    }

    public class ItemUpdateType : IEventPayloadType
    {
        public int? Frame;
        public int? TypeId;
        public int? State;
        public int? FacingDirection;
        public int? VelocityX;
        public int? VelocityY;
        public int? PositionX;
        public int? PositionY;
        public int? DamageTaken;
        public int? ExpirationTimer;
        public int? SpawnId;
        public int? MissileType;
        public int? TurnipFace;
        public int? ChargeShotLaunched;
        public int? ChargePower;
        public int? Owner;
    }

    public class FrameBookendType : IEventPayloadType
    {
        public int? Frame;
        public int? LatestFinalizedFrame;
    }

    public class GameEndType : IEventPayloadType, DeserializableFromUbjson
    {
        public int? GameEndMethod;
        public int? LrasInitiatorIndex;

        public void DeserializeFromUbJson(object payload)
        {
            if (!(payload is Dictionary<string, object>))
            {
                throw new ArgumentException($"Bad payload {payload}");
            }

            var data = payload as Dictionary<string, object>;
            if (data.ContainsKey("gameEndMethod"))
            {
                GameEndMethod = data["gameEndMethod"] as int?;
            }
            if (data.ContainsKey("lrasInitiatorIndex"))
            {
                LrasInitiatorIndex = data["lrasInitiatorIndex"] as int?;
            }
        }
    }
}
