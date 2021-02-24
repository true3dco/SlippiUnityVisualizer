using System.IO;
using UnityEngine;

public class SlippiCSTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var game = new SlippiCS.SlippiGame("C:\\Users\\Travis\\true3d\\SlippiFactory\\Slippi\\Game_FoxVCap.slp");
        var jsonStr = JsonUtility.ToJson(game.GetSettings());
        Debug.Log(jsonStr);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
