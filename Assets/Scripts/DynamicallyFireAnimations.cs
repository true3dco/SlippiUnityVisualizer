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
    public bool manualMode = false;
    public string animationToPlay ="";
    void Start()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/Resources/CharacterPrefabs/"+ gameObject.name +"/Animation/");
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


            // Apply Red Material to Model
            Material p1Material = Resources.Load("Materials/Player1Material") as Material;
            foreach (Transform child in gameObject.transform)
            {
                SkinnedMeshRenderer meshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }
                meshRenderer.sharedMaterial = p1Material;
            }
        



        foreach (AnimationState state in animation)
        {
         //   Debug.Log(state.name);
        }


    }

    // Update is called once per frame
    void Update()
    {

        if (!manualMode && Time.time > nextActionTime)
        {
            Debug.Log("Playing: " + animationNames[animationIndex]);
            animation.Play(animationNames[animationIndex]);
            animationIndex += 1;

            nextActionTime += period;
            // execute block of code here
            return;
        }
        if (manualMode && Time.time > nextActionTime){
                var anim = animation[animationToPlay];
                if (anim != null)
                {
                   animation.Play(animationToPlay);

                }
        }
    }
}
