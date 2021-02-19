using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UbJsharp;

public class SlippiUbjsonTest : MonoBehaviour
{
    private UbJsonReaderDeserializer deserializer; 

    void Awake()
    {
        deserializer = new UbJsonReaderDeserializer();
    }

    // Start is called before the first frame update
    void Start()
    {
        var slpTest = Resources.Load<TextAsset>("Slp/Test.slp");
        var output = deserializer.Deserialize(new MemoryStream(slpTest.bytes));
        Debug.Log(output);
    }
}
