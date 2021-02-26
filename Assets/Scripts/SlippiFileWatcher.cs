using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using UnityEngine;
using Timer = System.Timers.Timer;

public class SlippiFileWatcher : IDisposable
{
    private static readonly int MIN_FRAMES_PER_BATCH = 60;
    private static readonly int POLL_UPDATE_INTERVAL_MS = 750;

    private readonly Mutex fsWatchMtx = new Mutex();
    private readonly List<SlippiFramePlayerInfo> collectedFrames = new List<SlippiFramePlayerInfo>();
    private readonly FileSystemWatcher gameCreationWatcher = new FileSystemWatcher
    {
        NotifyFilter = NotifyFilters.CreationTime,
        Filter = "*.slp",
        IncludeSubdirectories = true
    };
    private ManagedGame currentManagedGame;
    private Timer gameUpdatePoller;

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
        gameCreationWatcher.Dispose();
        fsWatchMtx.Dispose();
        gameUpdatePoller?.Stop();
        gameUpdatePoller?.Dispose();
    }

    public void BeginWatchingAtPath(string slippiOutputPath)
    {
        if (gameCreationWatcher.EnableRaisingEvents)
        {
            throw new InvalidOperationException("Cannot watch when already watching!");
        }

        if (!Directory.Exists(slippiOutputPath))
        {
            Debug.LogWarning("[SlippiFileWatcher] Slippi output path does not exist. This could mean the user has not yet streamed. Creating.");
            Directory.CreateDirectory(slippiOutputPath);
        }

        Debug.Log($"[SlippiFileWatcher] Watching: {slippiOutputPath}");
        gameCreationWatcher.Path = slippiOutputPath; 
        gameCreationWatcher.Created += HandleNewGameCreated;
        gameCreationWatcher.Changed += HandleChangeForPotentiallyInProgressGame;
        gameCreationWatcher.EnableRaisingEvents = true;
    }

    // NOTE: A lot of this is taken from slippi-js's live file reading stuff / SlippiFactory.
    private void HandleNewGameCreated(object sender, FileSystemEventArgs e)
    {
        fsWatchMtx.WaitOne();
        InitGame(e.FullPath);
        fsWatchMtx.ReleaseMutex();
    }

    private void HandleChangeForPotentiallyInProgressGame(object sender, FileSystemEventArgs e)
    {
        fsWatchMtx.WaitOne();
        // This would mean there's a change after a file has been created.
        var alreadyInProgressGameNeedsWatching = currentManagedGame == null;
        if (alreadyInProgressGameNeedsWatching)
        {
            InitGame(e.FullPath); 
        }
        fsWatchMtx.ReleaseMutex();
    }

    private void InitGame(string slpFilePath)
    {
        Debug.Log($"[SlippiFileWatcher] New Game Created: {slpFilePath}");

        currentManagedGame = new ManagedGame(slpFilePath);

        var parser = currentManagedGame.SlpGame.Parser;
        parser.Settings += (object _, SlippiCS.SettingsEventArgs se) =>
        {
            if (currentManagedGame.State.Settings != null)
            {
                return;
            }
            currentManagedGame.State.Settings = se.Settings;
            Debug.Log("[SlippiFileWatcher] *Game Start* - New game has started");
            collectedFrames.Clear();
            OnGameStart(SlippiGame.FromSlippiCSGame(currentManagedGame.SlpGame));
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


        gameUpdatePoller?.Stop();
        gameUpdatePoller?.Dispose();
        gameUpdatePoller = new Timer(POLL_UPDATE_INTERVAL_MS)
        {
            AutoReset = true
        };
        gameUpdatePoller.Elapsed += PollUpdate;
        gameUpdatePoller.Enabled = true;
    }

    private void PollUpdate(object sender, ElapsedEventArgs e)
    {
        if (currentManagedGame == null)
        {
            throw new InvalidProgramException("PollUpdate called when the currently managed game is not set!");
        }
        Debug.Log($"[SlippiFileWatcher] Poll update for {currentManagedGame.Path}");
        currentManagedGame.SlpGame.Process();
    }

    private class ManagedGame
    {
        public readonly string Path;
        public readonly SlippiCS.SlippiGame SlpGame;
        public readonly SlpGameState State;

        public ManagedGame(string path)
        {
            Path = path;
            SlpGame = new SlippiCS.SlippiGame(Path);
            State = new SlpGameState
            {
                Settings = null
            };
        }
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