using Slippi;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SlippiLocalFileManager : MonoBehaviour
{
    public SlippiParser parser;
    public InputField SlippiFileInputField;
    public Button StartButton;

    [HideInInspector]
    public List<SlippiFramePlayerInfo> framesForCurrentMatch = new List<SlippiFramePlayerInfo>();

    private FileSystemWatcher liveMatchWatcher;

    void Start()
    {
        if (StartButton == null || SlippiFileInputField == null)
        {
            Debug.LogWarning("UI Controls for local file manager not found. Bailing.");
            return;
        }

        StartButton.onClick.AddListener(OnStartButtonClick);
    }

    private void StartMatch(string path)
    {
        var info = new DirectoryInfo(path);
        var fileInfo = info.GetFiles();
        var directoryInfo = info.GetDirectories();


        // Watch the top level directory
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = path;

        // Watch for changes in LastAccess and LastWrite times, and
        // the renaming of files or directories.
        watcher.NotifyFilter = NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.FileName
                                | NotifyFilters.DirectoryName;

        // Add event handlers.
        watcher.Changed += OnChangedDirectory;
        watcher.Created += OnChangedDirectory;
        watcher.Deleted += OnChangedDirectory;
        watcher.Renamed += OnRenamedDirectory;

        // Begin watching.
        watcher.EnableRaisingEvents = true;
    }

    private void OnStartButtonClick()
    {
        var slippiOutputPath = SlippiFileInputField.text;
        if (slippiOutputPath.Length == 0)
        {
            return;
        }

        SlippiFileInputField.gameObject.SetActive(false);
        StartButton.gameObject.SetActive(false);
        StartMatch(slippiOutputPath);
    }

    void LoadFile(string fileToLoad)
    {
        //w = new WWW ("file://D:/");
    }

    void Update()
    {
        //  if(w.isDone)
        //  {
        //     //  Debug.Log("done");
        //     //  tex = w.texture;
        //  }
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

            var text = File.OpenText(e.FullPath + "/init.json");
            SlippiGame game = JsonUtility.FromJson<SlippiGame>(text.ReadToEnd());

            
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