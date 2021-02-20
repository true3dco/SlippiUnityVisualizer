using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlippiCS
{
    public interface DeserializableFromUbjson
    {
        void DeserializeFromUbJson(object payload);
    }

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

    public class GameStartType : DeserializableFromUbjson
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
}
