using UnityEngine;
using UnityEngine.UI;
using SFB;

public class SlippiVisualizerUI : MonoBehaviour
{
    public Button SlippiFileSelect;
    public Button StartButton;
    //private SlippiLocalFileManager fileManager;
    private string _slippiDolphinExePath = "";
    private string slippiDolphinExePath {
        get
        {
            return _slippiDolphinExePath;
        }
        set
        {
            _slippiDolphinExePath = value;
            Debug.Log(_slippiDolphinExePath);
            if (_slippiDolphinExePath.Length == 0)
            {
                SlippiFileSelect.GetComponentInChildren<Text>().text = "Click to select Slippi Dolphin Executable";
                StartButton.interactable = false;
            }
            else
            {
                SlippiFileSelect.GetComponentInChildren<Text>().text = $"Slippi Executable (click to change): {_slippiDolphinExePath}";
                StartButton.interactable = true;
            }
        }
    } 

    void Awake()
    {
        // TODO: InitFileManager
        //fileManager = new SlippiLocalFileManager();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (StartButton == null || SlippiFileSelect == null)
        {
            Debug.LogWarning("UI Controls for local file manager not found. Bailing.");
            return;
        }

        // TODO: Try and read SlippiOutputPath from settings?
        // Also: Check that GetLocalFilePath is still present.

        SlippiFileSelect.onClick.AddListener(OnSlippiExeFileSelect);

        StartButton.interactable = slippiDolphinExePath.Length > 0;
        StartButton.onClick.AddListener(OnStartButtonClick);
    }

    private void OnStartButtonClick()
    {
        // TODO: Handle errors if Slippi Moved or Output path not set.
        if (slippiDolphinExePath.Length == 0)
        {
            return;
        }

        SlippiFileSelect.gameObject.SetActive(false);
        StartButton.gameObject.SetActive(false);
        //fileManager.StartMatch(slippiOutputPath);
    }

    private void OnSlippiExeFileSelect()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Slippi Dolphin Executable", "", "exe", false);
        if (paths.Length == 0)
        {
            return;
        }
        slippiDolphinExePath = paths[0];
    }
}
