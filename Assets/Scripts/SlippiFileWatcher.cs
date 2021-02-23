using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SlippiFileWatcher : IDisposable
{
    private static int MIN_FRAMES_PER_BATCH = 60;

    private readonly Dictionary<string, ManagedGame> gameByPath = new Dictionary<string, ManagedGame>();
    private readonly List<SlippiFramePlayerInfo> collectedFrames = new List<SlippiFramePlayerInfo>();
    private FileSystemWatcher fileSystemWatcher;

    public event EventHandler<FramesEventArgs> Frames;
    protected virtual void OnFrames(List<SlippiFramePlayerInfo> frames)
    {
        Frames?.Invoke(this, new FramesEventArgs { Frames = frames });
    }

    public event EventHandler<GameStartEventArgs> GameStart;
    protected virtual void OnGameStart(SlippiGame game)
    {
        GameStart?.Invoke(this, new GameStartEventArgs { Game = game });
    }

    public event EventHandler GameEnd;
    protected virtual void OnGameEnd()
    {
        GameEnd?.Invoke(this, new EventArgs());
    }

    public void Dispose()
    {
        if (fileSystemWatcher != null)
        {
            fileSystemWatcher.Dispose();
        }
    }

    public void BeginWatchingAtPath(string slippiOutputPath)
    {
        if (fileSystemWatcher != null)
        {
            throw new InvalidOperationException("Cannot watch when already watching!");
        }

        fileSystemWatcher = new FileSystemWatcher
        {
            Path = slippiOutputPath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess,
            // FIXME: Maybe this is the problem??
            Filter = "*.slp",
            IncludeSubdirectories = true
        };
        fileSystemWatcher.Created += HandleUpdate;
        fileSystemWatcher.Changed += HandleUpdate;
        fileSystemWatcher.EnableRaisingEvents = true;
        Debug.Log($"[SlippiFileWatcher] Watching: {slippiOutputPath}");
    }

    // NOTE: A lot of this is taken from slippi-js's live file reading stuff / SlippiFactory.
    private void HandleUpdate(object sender, FileSystemEventArgs e)
    {
        SlpGameState gameState;
        SlippiCS.GameStartType settings;
        Dictionary<int, SlippiCS.FrameEntryType> frames;
        SlippiCS.GameEndType gameEnd;

        if (!gameByPath.TryGetValue(e.FullPath, out var managedGame))
        {
            managedGame = new ManagedGame
            {
                SlpGame = new SlippiCS.SlippiGame(e.FullPath),
                State = new SlpGameState
                {
                    Settings = null
                }
            };
            gameByPath[e.FullPath] = managedGame;
        }

        var slpGame = managedGame.SlpGame;
        gameState = managedGame.State;
        settings = slpGame.GetSettings();
        frames = slpGame.GetFrames() ?? new Dictionary<int, SlippiCS.FrameEntryType>();
        gameEnd = slpGame.GetGameEnd();

        if (gameState.Settings == null && settings != null)
        {
            Debug.Log("[SlippiFileWatcher] *Game Start* - New game has started");
            gameState.Settings = settings;
            collectedFrames.Clear();
            OnGameStart(SlippiGame.FromSlippiCSGame(slpGame));
        }

        collectedFrames.AddRange(SlippiGame.FramesFromSlippiCS(frames));
        if (collectedFrames.Count >= MIN_FRAMES_PER_BATCH)
        {
            Debug.Log($"[SlippiFileWatcher] Sending {collectedFrames.Count} frames");
            OnFrames(new List<SlippiFramePlayerInfo>(collectedFrames));
            collectedFrames.Clear();
        }

        if (gameEnd != null)
        {
            string endMessage;
            switch (gameEnd.GameEndMethod ?? 0)
            {
                case 1:
                    endMessage = "TIME!";
                    break;
                case 2:
                    endMessage = "GAME!";
                    break;
                case 7:
                    endMessage = "No Contest";
                    break;
                default:
                    endMessage = "Unknown";
                    break;
            }
            var lrasText = gameEnd.GameEndMethod == 7 ? $" | Quitter Index: {gameEnd.LrasInitiatorIndex ?? -1}" : "";
            Debug.Log($"[SlippiFileWatcher] *Game Complete* - Type: {endMessage}{lrasText}");
        }
    }

    private class ManagedGame
    {
        public SlippiCS.SlippiGame SlpGame;
        public SlpGameState State;
    }

    private class SlpGameState
    {
        public SlippiCS.GameStartType Settings;
    }
}

public class GameStartEventArgs
{
    public SlippiGame Game;
}

public class FramesEventArgs
{
    public List<SlippiFramePlayerInfo> Frames;
}