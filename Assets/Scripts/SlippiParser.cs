﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Animations;

namespace Slippi
{
    public class SlippiParser : MonoBehaviour
    {
        // Start is called before the first frame update
        public Transform player1;
        public Transform player2;
        public Transform world;

        public Text counterText;
        public Text frameText;
        public Text player1Action;
        public Text player2Action;


        public bool manualMode = false;

        private Transform p1RotationToReset;
        private Transform p2RotationToReset;
//JOBJ_2
        private int player1Index;
        private int player2Index;


        private int player1Stock = 0;
        private int player2Stock = 0;

        private List<GameObject> player1Stocks = new List<GameObject>();
        private List<GameObject> player2Stocks = new List<GameObject>();

        private int player1ASID = -1;
        private int player2ASID = -1;

        public int counter = 1;
        SlippiGame game;

        private Animation p1Animation;
        private Animation p2Animation;
        public string filePath = "Json/Game_FoxVFox1";

        private GameObject stockHolder;

        void Start()
        {
            TextAsset gameJson = Resources.Load(filePath) as TextAsset;
            Debug.Log(gameJson);
            game = JsonUtility.FromJson<SlippiGame>(gameJson.text);
            //Debug.Log(game.settings.stageId);

            player1Index = game.settings.players[0].playerIndex;
            player2Index = game.settings.players[1].playerIndex;

            string stageName = SlippiLookupTable.GetStageName(game.settings.stageId);
            string p1Name = SlippiLookupTable.GetCharacterName(game.settings.players[0].characterId);
            string p2Name = SlippiLookupTable.GetCharacterName(game.settings.players[1].characterId);
            Debug.Log(stageName);
            Debug.Log(p1Name);
            Debug.Log(p2Name);

            // Load Stage
            UnityEngine.Object stagePrefab = Resources.Load("StagePrefabs/" + stageName + "/" + stageName);
            GameObject stage = Instantiate(stagePrefab) as GameObject;
            stage.transform.parent = world;
            world.transform.position = new Vector3(0, -25, 150);
            stage.transform.localPosition = new Vector3(0, 0, 0);
            stage.transform.eulerAngles = new Vector3(0, 180, 0);
            // Load Characters
            Debug.Log("p1 Index:" + player1Index);
            Debug.Log("p2 Index:" + player2Index);


            UnityEngine.Object p1Prefab = Resources.Load("CharacterPrefabs/" + p1Name + "/" + p1Name);
            GameObject p1 = Instantiate(p1Prefab) as GameObject;
            Animation _1Animation = p1.AddComponent(typeof(Animation)) as Animation;
            p1Animation = _1Animation;

            p1.transform.parent = player1;
            p1.transform.localPosition = new Vector3(0, 0, 0);
            p1.transform.localScale = new Vector3(100, 100, 100);

            // Apply Red Material to P1
            Material p1Material = Resources.Load("Materials/Player1Material") as Material;
            foreach (Transform child in p1.transform)
            {
                SkinnedMeshRenderer meshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }
                meshRenderer.sharedMaterial = p1Material;
            }


            UnityEngine.Object p2Prefab = Resources.Load("CharacterPrefabs/" + p2Name + "/" + p2Name);
            GameObject p2 = Instantiate(p2Prefab) as GameObject;
            Animation _2Animation = p2.AddComponent(typeof(Animation)) as Animation;
            p2Animation = _2Animation;

            p2.transform.parent = player2;
            p2.transform.localPosition = new Vector3(0, 0, 0);
            p2.transform.localScale = new Vector3(100, 100, 100);

            // Apply Blue Material to P2
            Material p2Material = Resources.Load("Materials/Player2Material") as Material;
            foreach (Transform child in p2.transform)
            {
                SkinnedMeshRenderer meshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer == null)
                {
                    continue;
                }
                meshRenderer.sharedMaterial = p2Material;
            }


            DirectoryInfo p1Dir = new DirectoryInfo("Assets/Resources/CharacterPrefabs/" + p1Name + "/Animation/");
            FileInfo[] p1files = p1Dir.GetFiles("*.fbx");


            foreach (FileInfo f in p1files)
            {
                string animationName = Path.GetFileNameWithoutExtension(f.ToString());
                AnimationClip clip = Resources.Load<AnimationClip>("CharacterPrefabs/" + p1Name + "/Animation/" + animationName);
                clip.legacy = true;
                p1Animation.AddClip(clip, animationName);
            }


            // Prepare Animations
            DirectoryInfo p2Dir = new DirectoryInfo("Assets/Resources/CharacterPrefabs/" + p2Name + "/Animation/");
            FileInfo[] p2files = p2Dir.GetFiles("*.fbx");


            foreach (FileInfo f in p2files)
            {
                string animationName = Path.GetFileNameWithoutExtension(f.ToString());
                AnimationClip clip = Resources.Load<AnimationClip>("CharacterPrefabs/" + p2Name + "/Animation/" + animationName);
                clip.legacy = true;
                p2Animation.AddClip(clip, animationName);
            }


            // Lock Animations for offending objects that change entire model location
            foreach (Transform child in p1.transform.GetComponentsInChildren<Transform>())
            {
                if (child.name == "JOBJ_1")
                {

                    PositionConstraint pc = child.gameObject.AddComponent(typeof(PositionConstraint)) as PositionConstraint;
                    ConstraintSource constraintSource = new ConstraintSource();
                    constraintSource.sourceTransform = child.parent;
                    pc.AddSource(constraintSource);
                    pc.constraintActive = true;
                }
                if (child.name == "JOBJ_2") {
                    p1RotationToReset = child;
                }
            };
            foreach (Transform child in p2.transform.GetComponentsInChildren<Transform>())
            {
                if (child.name == "JOBJ_1")
                {
                    PositionConstraint pc = child.gameObject.AddComponent(typeof(PositionConstraint)) as PositionConstraint;
                    ConstraintSource constraintSource = new ConstraintSource();
                    constraintSource.sourceTransform = child.parent;
                    pc.AddSource(constraintSource);
                    pc.constraintActive = true;
                }
                if (child.name == "JOBJ_2") {
                    p2RotationToReset = child;
                }
            };


            SlippiPost post1 = game.frames[0].players[player1Index].post;
            stockHolder = new GameObject("Stocks");
            stockHolder.transform.parent = world.transform;


            InstantiateStocks(post1.stocksRemaining, 1, p1Material, player1Stocks);
            InstantiateStocks(post1.stocksRemaining, 2, p2Material, player2Stocks);
            stockHolder.transform.localScale = new Vector3(5,5,2);
            stockHolder.transform.localPosition = new Vector3(0,0,-80);
            player1Stock = post1.stocksRemaining;
            player2Stock = post1.stocksRemaining;
        }



        void FixedUpdate()
        {
            if (manualMode)
            {
                if (!Input.GetKey(KeyCode.RightArrow))
                {
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        if (counter > 0)
                        {
                            counter = counter - 1;
                            counterText.text = "Counter: " + counter;

                        }
                    }
                    return;
                }
            }

            counterText.text = "Counter: " + counter;
            if (game.frames.Count <= counter)
            {
                return;
            }
            SlippiPre pre1 = game.frames[counter].players[player1Index].pre;
            SlippiPre pre2 = game.frames[counter].players[player2Index].pre;
            SlippiPost post1 = game.frames[counter].players[player1Index].post;
            SlippiPost post2 = game.frames[counter].players[player2Index].post;

            player1.position = new Vector3((float)pre1.positionX, (float)pre1.positionY - 25.0f, player1.position.z);
            player2.position = new Vector3((float)pre2.positionX, (float)pre2.positionY - 25.0f, player1.position.z);

            player1.eulerAngles = new Vector3(
                player1.eulerAngles.x,
                90 * pre1.facingDirection,
                player1.eulerAngles.x);

            player2.eulerAngles = new Vector3(
                player2.eulerAngles.x,
                 90 * pre2.facingDirection,
                player2.eulerAngles.x);

            if (player1ASID != pre1.actionStateId)
            {
                player1ASID = pre1.actionStateId;
                string p1AnimationClip = SlippiLookupTable.GetAnimationName(player1ASID);
                //Debug.Log("P1: " + p1AnimationClip + "|| " + pre1.actionStateId);
                var anim = p1Animation[p1AnimationClip];
                if (anim != null)
                {
                    p1RotationToReset.localRotation = new Quaternion(0, p1RotationToReset.localRotation.y, p1RotationToReset.localRotation.z, p1RotationToReset.localRotation.w);
                    p1Animation.Play(p1AnimationClip);
                    player1Action.text = "P1(Red) Animation: " + p1AnimationClip;
                }
                else
                {
                    Debug.LogWarning("Missing Animation: " + p1AnimationClip + " - p1");
                }



                //Debug.Log("P1ASID: " + player1ASID);
            }

            if (player2ASID != pre2.actionStateId)
            {
                player2ASID = pre2.actionStateId;
                string p2AnimationClip = SlippiLookupTable.GetAnimationName(player2ASID);
                // Debug.Log("P2: " + p2AnimationClip + "|| " + pre2.actionStateId);
                var anim = p2Animation[p2AnimationClip];
                if (anim != null)
                {
                    p2RotationToReset.localRotation = new Quaternion(0, p2RotationToReset.localRotation.y, p2RotationToReset.localRotation.z, p2RotationToReset.localRotation.w);
                    p2Animation.Play(p2AnimationClip);
                    player2Action.text = "P2(Blue) Animation: " + p2AnimationClip;
                }
                else
                {
                    Debug.LogWarning("Missing Animation: " + p2AnimationClip + " - p2");

                }


            }

            // Adjust Stock Count
            if (player1Stock != post1.stocksRemaining)
            {
                AdjustStockCount(post1.stocksRemaining, player1Stocks);
            }
            if (player2Stock != post2.stocksRemaining)
            {
                AdjustStockCount(post2.stocksRemaining, player2Stocks);
            }

            frameText.text = "Frame: " + pre1.frame;



            counter++;

        }


        void LateUpdate()
        {
            // Prevent position changes from animations
            //p1PositionToLock.localPosition = new Vector3(0,0,0);
            //p2PositionToLock.localPosition = new Vector3(0,0,0);

        }


        void InstantiateStocks(int stockCount, int playerNumber, Material material, List<GameObject> emptyStocks)
        {
            var i = 0;
            while (i < stockCount)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = new Vector3(i + playerNumber * 5 - 9, -2f, 10);
                sphere.transform.parent = stockHolder.transform;
                MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
                meshRenderer.material = material;
                emptyStocks.Add(sphere);
                i++;
            }

        }

        void AdjustStockCount(int newStockCount, List<GameObject> stocks)
        {

            var i = 0;
            while (i < stocks.Count)
            {
                if (i >= newStockCount)
                {
                    stocks[i].SetActive(false);
                }
                else
                {
                    stocks[i].SetActive(true);
                }
                i++;
            }
        }
    }
}