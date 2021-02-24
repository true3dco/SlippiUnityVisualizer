using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using IniParser;
using Slippi;

[RequireComponent(typeof(Slippi.SlippiPlayer))]
public class SlippiVisualizerViewController : MonoBehaviour
{
    private static string PREF_SLIPPI_DOLPHIN_EXE_PATH = "SlippiDolphinExePath";

    private class GetSlippiOutputPathException : ApplicationException
    {
        public GetSlippiOutputPathException()
        {
        }

        public GetSlippiOutputPathException(string message) : base(message)
        {
        }

        public GetSlippiOutputPathException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public Button SlippiFileSelect;
    public Button StartButton;
    private Slippi.SlippiPlayer slippiPlayer;
    private SlippiFileWatcher slippiFileWatcher;
    private string _slippiDolphinExePath = "";
    private string SlippiDolphinExePath {
        get
        {
            return _slippiDolphinExePath;
        }
        set
        {
            _slippiDolphinExePath = value;
            if (_slippiDolphinExePath.Length == 0)
            {
                SlippiFileSelect.GetComponentInChildren<Text>().text = "Click to select Slippi Dolphin Executable";
                StartButton.interactable = false;
                PlayerPrefs.DeleteKey(PREF_SLIPPI_DOLPHIN_EXE_PATH);
            }
            else
            {
                Debug.Log($"Dolphin Executable path set: {_slippiDolphinExePath}");
                SlippiFileSelect.GetComponentInChildren<Text>().text = $"Slippi Executable (click to change): {_slippiDolphinExePath}";
                StartButton.interactable = true;
                // Clear out any error messages
                StartButton.GetComponentInChildren<Text>().text = "Start!";
                PlayerPrefs.SetString(PREF_SLIPPI_DOLPHIN_EXE_PATH, _slippiDolphinExePath);
            }
        }
    } 

    void Awake()
    {
        slippiPlayer = GetComponent<Slippi.SlippiPlayer>();
        slippiFileWatcher = new SlippiFileWatcher();
    }

    private void OnDestroy()
    {
        slippiFileWatcher.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (StartButton == null || SlippiFileSelect == null)
        {
            Debug.LogWarning("UI Controls for local file manager not found. Bailing.");
            return;
        }
#if UNITY_EDITOR
        if (slippiPlayer.TestMode)
        {
            SlippiFileSelect.gameObject.SetActive(false);
            StartButton.gameObject.SetActive(false);
        }
#endif

        SlippiDolphinExePath = TryLoadSlippiExePathFromSettings();

        SlippiFileSelect.onClick.AddListener(OnSlippiExeFileSelect);
        StartButton.onClick.AddListener(OnStartButtonClick);

        slippiFileWatcher.GameStart += (object sender, GameStartEventArgs e) =>
        {
            var game = e.Game;
            if (slippiPlayer.game?.gameFinished ?? false)
            {
                slippiPlayer.nextGame = game;
            }
            else
            {
                slippiPlayer.game = game;
                slippiPlayer.StartMatch();
            }
        };

        slippiFileWatcher.Frames += (object sender, FramesEventArgs e) =>
        {
            var frames = e.Frames;
            if (slippiPlayer.game.gameFinished)
            {
                slippiPlayer.nextGame.frames.AddRange(frames);
            }
            else
            {
                slippiPlayer.game.frames.AddRange(frames);
            }
        };

        slippiFileWatcher.GameEnd += (object sender, EventArgs e) =>
        {
            slippiPlayer.game.gameFinished = true;
        };
    }

    private string TryLoadSlippiExePathFromSettings()
    {
        var savedExePath = PlayerPrefs.GetString(PREF_SLIPPI_DOLPHIN_EXE_PATH, "");
        if (savedExePath.Length == 0)
        {
            return savedExePath;
        }

        var dolphinStillExistsAtLocation = File.Exists(savedExePath);
        if (!dolphinStillExistsAtLocation)
        {
            return "";
        }
        return savedExePath;
    }

    private void OnGUI()
    {
        if (slippiPlayer.TestMode)
        {
            GUILayout.Label("Test Mode Active. UI Hidden.");
        }
    }

    private void OnStartButtonClick()
    {
        // TODO: Handle errors if Slippi Moved or Output path not set.
        if (SlippiDolphinExePath.Length == 0)
        {
            return;
        }

        string slippiOutputPath;
        try
        {
            slippiOutputPath = GetSlippiOutputPath();
        }
        catch (GetSlippiOutputPathException ex)
        {
            Debug.LogWarning("SLIPPI OUTPUT PATH EXCEPTION!");
            Debug.LogWarning(ex);
            StartButton.GetComponentInChildren<Text>().text = $"Error getting slippi output path (click to try again): {ex.Message}";
            return;
        }

        SlippiFileSelect.gameObject.SetActive(false);
        StartButton.gameObject.SetActive(false);
        slippiFileWatcher.BeginWatchingAtPath(slippiOutputPath);
    }

    private string GetSlippiOutputPath()
    {
        if (SlippiDolphinExePath.Length == 0)
        {
            throw new GetSlippiOutputPathException("Slippi Dolphin Path not set");
        }

        var configPath = Path.Combine(Path.GetDirectoryName(SlippiDolphinExePath), "User", "Config", "Dolphin.ini");
        if (!File.Exists(configPath))
        {
            throw new GetSlippiOutputPathException($"Could not find expected Dolphin configuration file {configPath} - Please let the True3D team know about this :(");
        }

        IniData config;
        try
        {
            var configIni = File.ReadAllText(configPath);
            config = new IniDataParser().Parse(configIni);
        }
        catch (Exception ex)
        {
            throw new GetSlippiOutputPathException($"Could not parse dolphin configuration file: {ex.Message} - Please report this to True3D :(", ex);
        }

        if (config["Core"] == null || config["Core"]["SlippiReplayDir"] == null)
        {
            throw new GetSlippiOutputPathException("Could not find Core.SlippiReplayDir in user config. Please report this to True3D :(");
        }

        Debug.Log($"Found Slippi output path: {config["Core"]["SlippiReplayDir"]}");
        return config["Core"]["SlippiReplayDir"];
    }

    private void OnSlippiExeFileSelect()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Slippi Dolphin Executable", "", "exe", false);
        if (paths.Length == 0)
        {
            return;
        }
        SlippiDolphinExePath = paths[0];
    }
}
