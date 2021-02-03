using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SlippiModels 
{

}


[Serializable]
public class SlippiGame
{
    public SlippiSettings settings;
    public List<SlippiFramePlayerInfo> frames;

    [NonSerialized]
    public bool gameFinished = false;
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