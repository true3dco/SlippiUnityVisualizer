using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlippiGame
{
    public SlippiSettings settings;
    public List<SlippiFramePlayerInfo> frames;
    public bool gameFinished = false;

    public static SlippiGame FromSlippiCSGame(SlippiCS.SlippiGame slippiCsGame, bool consumeFrames = false) =>
        new SlippiGame
        {
            settings = SettingsFromSlippiCS(slippiCsGame.GetSettings()),
            frames = consumeFrames ? FramesFromSlippiCS(slippiCsGame.GetFrames()) : new List<SlippiFramePlayerInfo>()
        };

    public static List<SlippiFramePlayerInfo> FramesFromSlippiCS(Dictionary<int, SlippiCS.FrameEntryType> slpFrames)
    {
        if (slpFrames == null)
        {
            Debug.Log("Initializing game from slippi game with no frames; this may imply live game. If not, check the file.");
            return new List<SlippiFramePlayerInfo>();
        }

        var slpFramesInOrder = new SortedDictionary<int, SlippiCS.FrameEntryType>(slpFrames);
        return slpFramesInOrder.Select(kvp => FrameFromSlippiCS(kvp.Value)).ToList();
    }

    public static SlippiFramePlayerInfo FrameFromSlippiCS(SlippiCS.FrameEntryType slpFrame)
    {
        var players = new List<SlippiFrame>();
        foreach (var kvp in slpFrame.Players)
        {
            var playerIndex = kvp.Key;
            var slpPlayer = kvp.Value;
            var frame = new SlippiFrame
            {
                pre = new SlippiPre(),
                post = new SlippiPost()
            };

            var slpPre = slpPlayer.Pre;
            if (!slpPre.Frame.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Pre missing frame property. Assigning 0");
                frame.pre.frame = 0;
            }
            else
            {
                frame.pre.frame = slpPre.Frame.Value;
            }

            if (!slpPre.ActionStateId.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Pre missing actionStateId property. Assigning 0");
                frame.pre.actionStateId = 0;
            }
            else
            {
                frame.pre.actionStateId = slpPre.ActionStateId.Value;
            }

            if (!slpPre.FacingDirection.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Pre missing facingDirection property. Assigning 0");
                frame.pre.facingDirection = 0;
            }
            else
            {
                frame.pre.facingDirection = (int)slpPre.FacingDirection.Value;
            }

            if (!slpPre.PositionX.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Pre missing positionX property. Assigning 0");
                frame.pre.positionX = 0;
            }
            else
            {
                frame.pre.positionX = slpPre.PositionX.Value;
            }

            if (!slpPre.PositionY.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Pre missing positionY property. Assigning 0");
                frame.pre.positionY = 0;
            }
            else
            {
                frame.pre.positionY = slpPre.PositionY.Value;
            }

            var slpPost = slpPlayer.Post;
            if (!slpPost.ActionStateId.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Post missing actionStateId property. Assigning 0");
                frame.post.actionStateId = 0;
            }
            else
            {
                frame.post.actionStateId = slpPost.ActionStateId.Value;
            }

            if (!slpPost.FacingDirection.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Post missing facingDirection property. Assigning 0");
                frame.post.facingDirection = 0;
            }
            else
            {
                frame.post.facingDirection = (int)slpPost.FacingDirection.Value;
            }

            if (!slpPost.PositionX.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Post missing positionX property. Assigning 0");
                frame.post.positionX = 0;
            }
            else
            {
                frame.post.positionX = slpPost.PositionX.Value;
            }

            if (!slpPost.PositionY.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Post missing positionY property. Assigning 0");
                frame.post.positionY = 0;
            }
            else
            {
                frame.post.positionY = slpPost.PositionY.Value;
            }

            if (!slpPost.StocksRemaining.HasValue)
            {
                Debug.LogWarning($"[Player index {playerIndex}] Post missing stocksRemaining property. Assigning 0");
                frame.post.stocksRemaining = 0;
            }
            else
            {
                frame.post.stocksRemaining = slpPost.StocksRemaining.Value;
            }

            players.Add(frame);
        }

        return new SlippiFramePlayerInfo
        {
            players = players
        };
    }

    private static SlippiSettings SettingsFromSlippiCS(SlippiCS.GameStartType slpSettings)
    {
        var settings = new SlippiSettings
        {
            players = new List<SlippiPlayer>()
        };
        if (slpSettings == null)
        {
            Debug.LogWarning("Initializing game from SlippiCS Game without settings");
            return settings;
        }

        if (!slpSettings.StageId.HasValue)
        {
            Debug.LogWarning("Missing stageId. Setting to 0.");
            settings.stageId = 0;
        }
        else
        {
            settings.stageId = slpSettings.StageId.Value;
        }

        foreach (var slpPlayer in slpSettings.Players)
        {
            var player = new SlippiPlayer();
            if (!slpPlayer.CharacterId.HasValue)
            {
                Debug.LogWarning($"Missing character ID for player {slpPlayer.PlayerIndex}. Setting to 0.");
                player.characterId = 0;
            }
            else
            {
                player.characterId = slpPlayer.CharacterId.Value;
            }
            player.playerIndex = slpPlayer.PlayerIndex;
            settings.players.Add(player);
        }

        return settings;
    }
}

[Serializable]
public class SlippiSettings
{
    public int stageId;
    public List<SlippiPlayer> players;
}


[Serializable]
public class SlippiPlayer
{
    public int characterId;
    public int playerIndex;
}



[Serializable]
public class SlippiFrame
{
    public SlippiPre pre;
    public SlippiPost post;
}

[Serializable]
public class SlippiFramePlayerInfo
{
    public List<SlippiFrame> players;
}


[Serializable]
public class SlippiPre
{
    public double positionX;
    public double positionY;
    public int facingDirection;
    public int actionStateId;
    public int frame;
}

[Serializable]
public class SlippiPost
{
    public double positionX;
    public double positionY;
    public int facingDirection;
    public int actionStateId;
    public int stocksRemaining;
}