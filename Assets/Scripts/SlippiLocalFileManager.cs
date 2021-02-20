using Slippi;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
public class SlippiLocalFileManager
{
    public List<SlippiFramePlayerInfo> framesForCurrentMatch = new List<SlippiFramePlayerInfo>();

    private readonly Slippi.SlippiPlayer parser;
    private FileSystemWatcher liveMatchWatcher;

    public SlippiLocalFileManager(Slippi.SlippiPlayer parser)
    {
        this.parser = parser;
    }

    public void StartMatch(string path)
    {
        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();
        var directoryInfo = info.GetDirectories();

        FileSystemWatcher watcher = new FileSystemWatcher
        {
            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            NotifyFilter = NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName
        };

        // Add event handlers.
        watcher.Changed += OnChangedDirectory;
        watcher.Created += OnChangedDirectory;
        watcher.Deleted += OnChangedDirectory;
        watcher.Renamed += OnRenamedDirectory;

        // Begin watching.
        watcher.EnableRaisingEvents = true;
    }


    // Define the event handlers.
    private void OnChangedDirectory(object source, FileSystemEventArgs e)
    {
        // Specify what is done when a file is changed, created, or deleted.
        //Debug.Log($"File: {e.FullPath} {e.ChangeType}");
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            Debug.Log("New Match Started");

            framesForCurrentMatch = new List<SlippiFramePlayerInfo>();

            liveMatchWatcher = new FileSystemWatcher();
            liveMatchWatcher.Path = e.FullPath;

            liveMatchWatcher.NotifyFilter = NotifyFilters.LastAccess
            | NotifyFilters.LastWrite
            | NotifyFilters.FileName
            | NotifyFilters.DirectoryName;

            liveMatchWatcher.Created += OnFileCreated;
            liveMatchWatcher.EnableRaisingEvents = true;

            // TODO: Replace with just the Slippi Game types?
            // TODO: Implement actual slp reading.
            SlippiCS.SlippiGame slpGame = new SlippiCS.SlippiGame(e.FullPath);
            var slpSettings = slpGame.GetSettings();
            SlippiGame game = new SlippiGame
            {
                settings = new SlippiSettings
                {
                    players = new List<SlippiPlayer>()
                },
                frames = new List<SlippiFramePlayerInfo>()
            };
            game.settings.stageId = slpSettings.StageId.GetValueOrDefault(0);
            foreach (var slpPlayer in slpSettings.Players)
            {
                var player = new SlippiPlayer();
                player.characterId = slpPlayer.CharacterId.GetValueOrDefault(0);
                player.playerIndex = slpPlayer.PlayerIndex;
                game.settings.players.Add(player);
            }

            
            if (parser.game != null && parser.game.gameFinished)
            {
                parser.nextGame = game;
            }
            else
            {
                parser.game = game;
                parser.StartMatch();
            }
        }
    }

    private void OnFileCreated(object source, FileSystemEventArgs e)
    {
       // Debug.Log($"New JSON Frame File: {e.FullPath} {e.ChangeType}");
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            var text = File.OpenText(e.FullPath);
            SlippiGame game = JsonUtility.FromJson<SlippiGame>(text.ReadToEnd());
            // Debug.Log("Adding " + game.frames.Count);
            // Debug.Log("Total Frames: " + framesForCurrentMatch.Count);
            
            if (parser.game.gameFinished){
                parser.nextGame.frames.AddRange(game.frames);
            } else {
                parser.game.frames.AddRange(game.frames);
            }

            if (e.FullPath.Contains("_FINAL"))
            {
                parser.game.gameFinished = true;
            }
        }
    }

    private static void OnRenamedDirectory(object source, RenamedEventArgs e) =>
        // Specify what is done when a file is renamed.
        Debug.Log($"File: {e.OldFullPath} renamed to {e.FullPath}");
}