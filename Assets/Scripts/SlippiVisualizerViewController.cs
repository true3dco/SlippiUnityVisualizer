using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using IniParser;
using System.Runtime.Serialization;

[RequireComponent(typeof(Slippi.SlippiPlayer))]
[RequireComponent(typeof(HyperSDKController))]
public class SlippiVisualizerViewController : MonoBehaviour
{
    private static string PREF_SLIPPI_DOLPHIN_EXE_PATH = "SlippiDolphinExePath";
    private static string PREF_MELEE_ST_EXE_PATH = "MeleeSTExePath";

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

    public Text SlippiFileSelectLabel;
    public Button SlippiFileSelect;
    public Text MeleeSTSelectLabel;
    public Button MeleeSTSelect;
    public Button InitButton;
    private Slippi.SlippiPlayer slippiPlayer;
    private SlippiFileWatcher slippiFileWatcher;
    private MeleeMetadataWatcher meleeMetadataWatcher;
    private HyperSDKController hsdk;
    private readonly Queue<Action> runInUpdate = new Queue<Action>();
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
                InitButton.interactable = false;
                PlayerPrefs.DeleteKey(PREF_SLIPPI_DOLPHIN_EXE_PATH);
            }
            else
            {
                Debug.Log($"Dolphin Executable path set: {_slippiDolphinExePath}");
                SlippiFileSelect.GetComponentInChildren<Text>().text = $"Slippi Executable (click to change): {_slippiDolphinExePath}";
                InitButton.interactable = true;
                // Clear out any error messages
                InitButton.GetComponentInChildren<Text>().text = "Initialize";
                PlayerPrefs.SetString(PREF_SLIPPI_DOLPHIN_EXE_PATH, _slippiDolphinExePath);
            }
        }
    } 

    private string _meleeSTExePath = "";
    private string MeleeSTExePath
    {
        get => _meleeSTExePath;

        set
        {
            _meleeSTExePath = value;
            if (string.IsNullOrEmpty(_meleeSTExePath))
            {
                MeleeSTSelect.GetComponentInChildren<Text>().text = "(Optional) Click to select Melee ST Executable";
                PlayerPrefs.DeleteKey(PREF_MELEE_ST_EXE_PATH);
            } else
            {
                Debug.Log($"Melee ST Exe path set to {_meleeSTExePath}");
                MeleeSTSelect.GetComponentInChildren<Text>().text = $"ST Executable (click to change): {_meleeSTExePath}";
                PlayerPrefs.SetString(PREF_MELEE_ST_EXE_PATH, _meleeSTExePath);
            }
        }
    }


    void Awake()
    {
        slippiPlayer = GetComponent<Slippi.SlippiPlayer>();
        hsdk = GetComponent<HyperSDKController>();
        slippiFileWatcher = new SlippiFileWatcher();
        meleeMetadataWatcher = new MeleeMetadataWatcher();

        hsdk.ShowGUI = false;
    }

    private void OnDestroy()
    {
        slippiFileWatcher.Dispose();
        meleeMetadataWatcher.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (InitButton == null || SlippiFileSelect == null)
        {
            Debug.LogWarning("UI Controls for local file manager not found. Bailing.");
            return;
        }
#if UNITY_EDITOR
        if (slippiPlayer.TestMode)
        {
            SlippiFileSelectLabel.gameObject.SetActive(false);
            SlippiFileSelect.gameObject.SetActive(false);
            MeleeSTSelectLabel.gameObject.SetActive(false);
            MeleeSTSelect.gameObject.SetActive(false);
            InitButton.gameObject.SetActive(false);
        }
#endif

        SlippiDolphinExePath = TryLoadSlippiExePathFromSettings();
        MeleeSTExePath = TryLoadMeleeSTExePathFromSettings();

        SlippiFileSelect.onClick.AddListener(OnSlippiExeFileSelect);
        MeleeSTSelect.onClick.AddListener(OnMeleeSTExeFileSelect);
        InitButton.onClick.AddListener(OnInitButtonClick);

        slippiFileWatcher.GameStart += (object sender, GameStartEventArgs e) =>
        {
            var game = e.Game;
            runInUpdate.Clear();
            runInUpdate.Enqueue(() =>
            {
                if (slippiPlayer.game?.gameFinished ?? false)
                {
                    slippiPlayer.nextGame = game;
                }
                else
                {
                    slippiPlayer.game = game;
                    slippiPlayer.StartMatch();
                }
            });
        };

        slippiFileWatcher.Frames += (object sender, FramesEventArgs e) =>
        {
            var frames = e?.Frames;
            runInUpdate.Enqueue(() =>
            {
                if (slippiPlayer.game.gameFinished)
                {
                    slippiPlayer.nextGame.frames.AddRange(frames);
                }
                else
                {
                    slippiPlayer.game.frames.AddRange(frames);
                }
            });
        };

        slippiFileWatcher.GameEnd += (object sender, EventArgs e) =>
        {
            runInUpdate.Clear();
            runInUpdate.Enqueue(() =>
            {
                slippiPlayer.game.gameFinished = true;
            });
        };

        meleeMetadataWatcher.Changed += (object sender, MeleeMetadataChangedEventArgs e) =>
        {
            hsdk.UpdateMetadata(new HyperSDK.MetadataUpdate
            {
                P1Name = e.Metadata.p1Name,
                P2Name = e.Metadata.p2Name
            });
        };
    }

    private void OnMeleeSTExeFileSelect()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Melee ST Executable", "", new[] { new ExtensionFilter("Applications", new[] { "exe", "app" }) }, false);
        if (paths.Length == 0)
        {
            return;
        }
        MeleeSTExePath = paths[0];
    }

    void FixedUpdate()
    {
        while (runInUpdate.Count > 0)
        {
            var action = runInUpdate.Dequeue();
            action.Invoke();
        }
        slippiPlayer.Tick();
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
    private string TryLoadMeleeSTExePathFromSettings()
    {
        var savedExePath = PlayerPrefs.GetString(PREF_MELEE_ST_EXE_PATH, "");
        if (savedExePath.Length == 0)
        {
            return savedExePath;
        }

        var meleeSTStillExistsAtLocation = File.Exists(savedExePath);
        if (!meleeSTStillExistsAtLocation)
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

    private void OnInitButtonClick()
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
            InitButton.GetComponentInChildren<Text>().text = $"Error getting slippi output path (click to try again): {ex.Message}";
            return;
        }
        var meleeMetadataPath = "";
        try
        {
            meleeMetadataPath = GetMeleeMetadataPath();
        } catch (GetMeleeMetadataPathException ex)
        {
            Debug.LogWarning("MELEE OUTPUT PATH EXCEPTION!");
            Debug.LogWarning(ex);
            MeleeSTSelect.GetComponentInChildren<Text>().text = $"!! Metadata will not be streamed: {ex.Message}";
        }
        // TODO: Clear path (probs won't implement this time around)

        SlippiFileSelectLabel.gameObject.SetActive(false);
        SlippiFileSelect.gameObject.SetActive(false);
        MeleeSTSelectLabel.gameObject.SetActive(false);
        MeleeSTSelect.gameObject.SetActive(false);
        InitButton.gameObject.SetActive(false);
        slippiFileWatcher.BeginWatchingAtPath(slippiOutputPath);
        if (!string.IsNullOrEmpty(meleeMetadataPath))
        {
            meleeMetadataWatcher.BeginWatchingAtPath(meleeMetadataPath);
        }
        hsdk.ShowGUI = true;
    }

    private string GetMeleeMetadataPath()
    {
        if (string.IsNullOrEmpty(MeleeSTExePath))
        {
            return "";
        }

        if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.WindowsPlayer)
        {
            Debug.LogWarning("For now, we only support Melee ST on Windows");
            return "";
        }

        var jsonPath = Path.Combine(Path.GetDirectoryName(MeleeSTExePath), "Resources", "Texts", "ScoreboardInfo.json");
        if (!File.Exists(jsonPath))
        {
            throw new GetMeleeMetadataPathException($"Path does not exist: {jsonPath}");
        }
        return jsonPath;
    }

    private string GetSlippiOutputPath()
    {
        if (SlippiDolphinExePath.Length == 0)
        {
            throw new GetSlippiOutputPathException("Slippi Dolphin Path not set");
        }

        string configPath;
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                configPath = Path.Combine(Path.GetDirectoryName(SlippiDolphinExePath), "User", "Config", "Dolphin.ini");
                break;
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                // On OSX, Applications are folders which contain package contents, housing the configuration and other info.
                configPath = Path.Combine(SlippiDolphinExePath, "Contents", "Resources", "User", "Config", "Dolphin.ini");
                break;
            default:
                throw new PlatformNotSupportedException($"Runtime platform {Application.platform} not supported.");
        }
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
        var paths = StandaloneFileBrowser.OpenFilePanel("Slippi Dolphin Executable", "", new[] { new ExtensionFilter("Applications", new[] { "exe", "app" }) }, false);
        if (paths.Length == 0)
        {
            return;
        }
        SlippiDolphinExePath = paths[0];
    }

    [Serializable]
    private class GetMeleeMetadataPathException : Exception
    {
        public GetMeleeMetadataPathException()
        {
        }

        public GetMeleeMetadataPathException(string message) : base(message)
        {
        }

        public GetMeleeMetadataPathException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GetMeleeMetadataPathException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
