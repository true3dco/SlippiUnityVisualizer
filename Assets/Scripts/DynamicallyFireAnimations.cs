using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class DynamicallyFireAnimations : MonoBehaviour
{
    //Animator plAnimator;

    protected List<AnimationClip> clips = new List<AnimationClip>();
    protected List<string> animationNames = new List<string>();
    Animation animation;

    private float nextActionTime = 0.0f;
    private float period = 1.5f;
    private int animationIndex = 0;

    void Start()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/Resources/CharacterPrefabs/Luigi/Animation/");
        FileInfo[] info = dir.GetFiles("*.fbx");
        animation = GetComponent<Animation>();
        string lastAnimation = "asdas";
        foreach (FileInfo f in info)
        {
            string animationName = Path.GetFileNameWithoutExtension(f.ToString());
            animationNames.Add(animationName);
            AnimationClip clip = Resources.Load<AnimationClip>("CharacterPrefabs/Luigi/Animation/" + animationName);
            clip.legacy = true;
            animation.AddClip(clip, animationName);
            lastAnimation = animationName;

        }
        



        foreach (AnimationState state in animation)
        {
         //   Debug.Log(state.name);
        }


    }

    // Update is called once per frame
    void Update()
    {

        if (Time.time > nextActionTime)
        {
            Debug.Log("Playing: " + animationNames[animationIndex]);
            animation.Play(animationNames[animationIndex]);
            animationIndex += 1;

            nextActionTime += period;
            // execute block of code here
        }
    }
}
