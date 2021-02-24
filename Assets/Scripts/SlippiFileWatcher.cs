using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;

public class SlippiFileWatcher : IDisposable
{
    private static int MIN_FRAMES_PER_BATCH = 60;

    private readonly IDictionary<string, ManagedGame> gameByPath = new ConcurrentDictionary<string, ManagedGame>();
    // NOTE: CollectedFrames must *only* be accessed in scenarios where thread-safety is guaranteed.
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
        fileSystemWatcher?.Dispose();
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
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.Size,
            Filter = "*.slp",
            IncludeSubdirectories = true
        };
        fileSystemWatcher.Created += HandleUpdate;
        fileSystemWatcher.Changed += HandleUpdate;
        fileSystemWatcher.Renamed += HandleUpdate;
        fileSystemWatcher.EnableRaisingEvents = true;
        Debug.Log($"[SlippiFileWatcher] Watching: {slippiOutputPath}");
    }

    // NOTE: A lot of this is taken from slippi-js's live file reading stuff / SlippiFactory.
    private void HandleUpdate(object sender, FileSystemEventArgs e)
    {
        Debug.Log($"[SlippiFileWatcher] Update: {e.FullPath}");

        // TODO: This should probably go into the game object itself?
        if (!gameByPath.TryGetValue(e.FullPath, out var managedGame))
        {
            Debug.Log($"[SlippiFileWatcher] Create Game for file: {e.FullPath}");
            managedGame = new ManagedGame
            {
                SlpGame = new SlippiCS.SlippiGame(e.FullPath),
                State = new SlpGameState
                {
                    Settings = null
                }
            };

            var parser = managedGame.SlpGame.Parser;
            parser.Settings += (object _, SlippiCS.SettingsEventArgs se) =>
            {
                if (managedGame.State.Settings != null)
                {
                    return;
                }
                managedGame.State.Settings = se.Settings;
                Debug.Log("[SlippiFileWatcher] *Game Start* - New game has started");
                collectedFrames.Clear();
                OnGameStart(SlippiGame.FromSlippiCSGame(managedGame.SlpGame));
            };

            parser.Frame += (object _, SlippiCS.FrameEventArgs ee) =>
            {
                collectedFrames.Add(SlippiGame.FrameFromSlippiCS(ee.Frame));
                if (collectedFrames.Count >= MIN_FRAMES_PER_BATCH)
                {
                    Debug.Log($"[SlippiFileWatcher] Sending {collectedFrames.Count} frames");
                    OnFrames(new List<SlippiFramePlayerInfo>(collectedFrames));
                    collectedFrames.Clear();
                }
            };

            parser.End += (object _, SlippiCS.EndEventArgs ee) =>
            {
                var gameEnd = ee.GameEnd;
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
                OnGameEnd();
            };

            gameByPath[e.FullPath] = managedGame;
        }

        var slpGame = managedGame.SlpGame;
        slpGame.Process();
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