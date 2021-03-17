using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Slippi
{
    [RequireComponent(typeof(HyperSDKController))]
    public class SlippiPlayer : MonoBehaviour
    {
        private static readonly ISet<string> STAGES_NEEDING_POINT8_REDUCTION = new HashSet<string> { "Fountain_Of_Dreams", "DietBattlefield" };
        private static Vector3 DEFAULT_CHARACTER_SCALE = new Vector3(100, 100, 100);
        private static readonly Dictionary<string, Vector3> SPECIAL_CHARACTER_SCALES = new Dictionary<string, Vector3>
        {
            {"Pichu", new Vector3(45, 45, 45)}
        };

        public Transform world;
        public Text frameText;
        public Text player1Action;
        public Text player2Action;
        public bool ManuallyAdvanceFrames = false;
        public bool TestMode = false;
        public string TestModeFile = "Hello";
        [HideInInspector]
        public int counter = 1;
        [HideInInspector]
        public SlippiGame game = null;
        [HideInInspector]
        public SlippiGame nextGame = null;


        private Transform player1;
        private Transform player2;
        private int sceneID = 0;
        public Text counterText;
        private GameObject player1Shield;
        private GameObject player2Shield;
        private float worldScale = .1f;
        private Transform p1RotationToReset;
        private Transform p2RotationToReset;
        private int player1Index;
        private int player2Index;
        private int player1ASID = -1;
        private int player2ASID = -1;
        private HyperSDKController hsdkc;
        private Animation p1Animation;
        private Animation p2Animation;
        private bool matchStarted = false;

        private Stocks stocks;

        void Start()
        {
#if UNITY_EDITOR
            if (TestMode)
            {
                var slpGame = new SlippiCS.SlippiGame(TestModeFile);
                game = SlippiGame.FromSlippiCSGame(slpGame, consumeFrames: true);
                StartMatch();
            }
#endif

            // DONT TOUCH THIS
            Time.fixedDeltaTime = .01666666f;
        }

        public void StartMatch()
        {
            foreach (Transform child in world.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            if (game == null)
            {
                Debug.LogWarning("You need to set a game in order to play it back");
            }
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
            world.transform.position = new Vector3(0, -1, 7);
            stage.transform.localPosition = new Vector3(0, 0, 0);
            stage.transform.eulerAngles = new Vector3(0, 180, 0);
            // Fountain of Dreams needs to be reduced to look good
            if (STAGES_NEEDING_POINT8_REDUCTION.Contains(stageName))
            {
                Debug.LogWarning($"Scaling {stageName} down to .8 - If this is no longer needed, remove this code");
                stage.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            }

            // Load Characters
            GameObject p1 = InstantiateCharacter(p1Name);
            Animation _1Animation = p1.AddComponent(typeof(Animation)) as Animation;
            p1Animation = _1Animation;
            GameObject p1G = new GameObject();
            player1 = p1G.transform;
            GameObject p2G = new GameObject();
            player2 = p2G.transform;
            player1.parent = world;
            player2.parent = world;
            p1.transform.parent = player1;
            p1.transform.localPosition = new Vector3(0, 0, 0);

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


            var p2 = InstantiateCharacter(p2Name);
            Animation _2Animation = p2.AddComponent(typeof(Animation)) as Animation;
            p2Animation = _2Animation;
            p2.transform.parent = player2;
            p2.transform.localPosition = new Vector3(0, 0, 0);

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

            // ================= Initiate Shield Stuff
            Object shieldPrefab = Resources.Load("CharacterPrefabs/" + p2Name + "/" + p2Name);

            player1Shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player2Shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Material p1ShieldMaterial = Resources.Load("Materials/Player1ShieldMaterial") as Material;
            Material p2ShieldMaterial = Resources.Load("Materials/Player2ShieldMaterial") as Material;

            player1Shield.GetComponent<MeshRenderer>().material = p1ShieldMaterial;
            player2Shield.GetComponent<MeshRenderer>().material = p2ShieldMaterial;

            player1Shield.transform.parent = p1.transform;
            player2Shield.transform.parent = p2.transform;
            player1Shield.name = "Shield";
            player2Shield.name = "Shield";
            player1Shield.transform.localPosition = new Vector3(0, .07f, 0);
            player2Shield.transform.localPosition = new Vector3(0, .07f, 0);
            player1Shield.transform.localScale = new Vector3(.12f, .12f, .12f);
            player2Shield.transform.localScale = new Vector3(.12f, .12f, .12f);
            player1Shield.SetActive(false);
            player2Shield.SetActive(false);

            // ================ End Shield Stuff

            // Prepare Animations
            LoadAnimationClips(p1Name, p1Animation);
            Debug.Log("Player1 Animation Clip Count" + p1Animation.GetClipCount());
            LoadAnimationClips(p2Name, p2Animation);

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
                if (child.name == "JOBJ_2")
                {
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
                if (child.name == "JOBJ_2")
                {
                    p2RotationToReset = child;
                }
            };


            world.localScale = new Vector3(1 * worldScale, 1 * worldScale, 1 * worldScale);
            stocks = new Stocks(stage);
            matchStarted = true;

            // Tell Hypersdk players to reload
            hsdkc = GetComponent<HyperSDKController>();
            sceneID++;
            hsdkc.sceneID = sceneID;
        }

        public void EndMatch()
        {
            Debug.Log("END MATCH");
            matchStarted = false;
            game = null;
            counter = 0;
            world.localScale = new Vector3(1, 1, 1);
            // Remove all Smash Objects from the scene
            stocks.Dispose();
            foreach (Transform child in world.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            ShowWaitingForNextMatch();
            
            if (nextGame != null) {
                game = nextGame;
                nextGame = null;
                //matchStarted = true;
            }

        }

        public void Tick()
        {
  
            //Debug.Log(game.frames.Count);

            if (!matchStarted)
            {
                if (game != null && game.frames.Count != 0)
                {
                    StartMatch();
                }
                return;
            }

            if (ManuallyAdvanceFrames)
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
            var outOfFrames = game.frames.Count <= counter;
            if (outOfFrames)
            {
                if (game.gameFinished)
                {
                    Debug.Log("Ending match");
                    EndMatch();
                    return;
                }
                Debug.Log("Out of frames. Game Finished = " + game.gameFinished);
                return;
            }


            if (game.frames[counter].players.Count != 2)
            {
                counter++;
                return;
            }
            SlippiPre pre1 = game.frames[counter].players[player1Index].pre;
            SlippiPre pre2 = game.frames[counter].players[player2Index].pre;
            SlippiPost post1 = game.frames[counter].players[player1Index].post;
            SlippiPost post2 = game.frames[counter].players[player2Index].post;


            player1.localPosition = new Vector3((float)pre1.positionX, (float)pre1.positionY, 0);
            player2.localPosition = new Vector3((float)pre2.positionX, (float)pre2.positionY, 0);

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
                if (player1ASID >= 178 && player1ASID <= 182)
                {
                    player1Shield.SetActive(true);
                }
                else
                {
                    player1Shield.SetActive(false);
                }

                string p1AnimationClip = SlippiLookupTable.GetAnimationName(player1ASID);
                //Debug.Log("P1: " + p1AnimationClip + "|| " + pre1.actionStateId);
                var anim = p1Animation[p1AnimationClip];
                if (anim != null)
                {
                    p1RotationToReset.localRotation = new Quaternion(0, p1RotationToReset.localRotation.y, p1RotationToReset.localRotation.z, p1RotationToReset.localRotation.w);
                    Debug.Log("Is Animation Playing" + p1Animation.isPlaying);
                    p1Animation.Play(p1AnimationClip);
                    if (p1AnimationClip != "") {
                        player1Action.text = "P1(Red) Animation: " + p1AnimationClip;
                        Debug.Log("Animation Clip 1 : " + p1AnimationClip);
                    }
                    
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
                if (player2ASID >= 178 && player2ASID <= 182)
                {
                    player2Shield.SetActive(true);
                }
                else
                {
                    player2Shield.SetActive(false);
                }


                string p2AnimationClip = SlippiLookupTable.GetAnimationName(player2ASID);
                // Debug.Log("P2: " + p2AnimationClip + "|| " + pre2.actionStateId);
                var anim = p2Animation[p2AnimationClip];
                if (anim != null)
                {
                    p2RotationToReset.localRotation = new Quaternion(0, p2RotationToReset.localRotation.y, p2RotationToReset.localRotation.z, p2RotationToReset.localRotation.w);
                    p2Animation.Play(p2AnimationClip);
                    
                    if (p2AnimationClip != "") {
                        player2Action.text = "P2(Blue) Animation: " + p2AnimationClip;
                        Debug.Log("Animation Clip 2 : " + p2AnimationClip);

                    }
                }
                else
                {
                    Debug.LogWarning("Missing Animation: " + p2AnimationClip + " - p2");

                }
            }


            stocks.Update(post1.stocksRemaining, post2.stocksRemaining);
            frameText.text = "Frame: " + pre1.frame;
            counter++;
        }

        private void ShowWaitingForNextMatch()
        {
            GameObject loadingIndicator = GameObject.CreatePrimitive(PrimitiveType.Plane);
            loadingIndicator.transform.parent = world;
            loadingIndicator.transform.localScale = new Vector3(1, 1, .1f);
            loadingIndicator.transform.eulerAngles = new Vector3(90, 180, 0);
            loadingIndicator.transform.localPosition = new Vector3(0,2,0);
            var mr = loadingIndicator.GetComponent<MeshRenderer>();
            Material loadingMaterial = Resources.Load("Materials/LoadingMaterial") as Material;

            mr.sharedMaterial = loadingMaterial;
            sceneID++;
            hsdkc = GetComponent<HyperSDKController>();
            hsdkc.sceneID = sceneID;
        }

        private void LoadAnimationClips(string playerName, Animation playerAnimationComponent) {

            string animationDir = "CharacterPrefabs/" + playerName + "/Animation";
            Object[] clipObjs = Resources.LoadAll(animationDir, typeof(Object));
            foreach (Object clipObj in clipObjs)
            {
                if (clipObj is AnimationClip) {
                    continue;
                }
                AnimationClip clip = Resources.Load<AnimationClip>(animationDir + "/" + clipObj.name) as AnimationClip;
                string animationName = clipObj.name;
                clip.legacy = true;
                playerAnimationComponent.AddClip(clip, animationName);
            }
        }

        private GameObject InstantiateCharacter(string characterName)
        {
            UnityEngine.Object prefab = Resources.Load("CharacterPrefabs/" + characterName + "/" + characterName);
            GameObject character;
            if (prefab == null)
            {
                character = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                character.transform.localScale = new Vector3(10, 10, 10);
            }
            else
            {
                character = Instantiate(prefab) as GameObject;
                if (!SPECIAL_CHARACTER_SCALES.TryGetValue(characterName, out Vector3 scale))
                {
                    scale = DEFAULT_CHARACTER_SCALE;
                }
                character.transform.localScale = scale;
            }

            return character;
        }

        #region Supplemental classes
        private class Stocks : IDisposable
        {
            private enum StockPosition
            {
                LEFT,
                RIGHT
            }
            private static readonly float STOCK_SPACING = 1.2f;
            private static readonly float STOCK_INSET = -5f;

            private readonly Vector3 worldTopLeft = new Vector3();
            private readonly Vector3 worldTopRight = new Vector3();
            private readonly GameObject stockHolder = new GameObject("Stocks");
            private readonly Material player1Material = Resources.Load("Materials/Player1Material") as Material;
            private readonly List<GameObject> player1Stocks = new List<GameObject>();
            private int p1StocksRemaining = -1;
            private readonly Material player2Material = Resources.Load("Materials/Player2Material") as Material;
            private readonly List<GameObject> player2Stocks = new List<GameObject>();
            private int p2StocksRemaining = -1;

            public Stocks(GameObject stage)
            {
                // FIXME: Unsure currently how to compute the actual size of the stage.
                var width = 10;
                var height = 13;
                worldTopLeft.x = stage.transform.position.x - (width / 2);
                worldTopLeft.y = stage.transform.position.y + (height / 2);
                worldTopRight.x = stage.transform.position.x + (width / 2);
                worldTopRight.y = stage.transform.position.y + (height / 2);

                stockHolder.transform.parent = stage.transform;
                stockHolder.transform.position = new Vector3(0, height, stage.transform.position.z + 8);
            }

            public void Dispose()
            {
                Destroy(stockHolder);
            }

            public void Update(int p1StocksRemaining, int p2StocksRemaining)
            {
                var stocksNeedInstantiation = player1Stocks.Count == 0;
                if (stocksNeedInstantiation)
                {
                    InstantiateStocks(p1StocksRemaining, p2StocksRemaining);
                    return;
                }

                AdjustCounts(p1StocksRemaining, p2StocksRemaining);
            }

            private void InstantiateStocks(int p1StocksRemaining, int p2StocksRemaining)
            {
                this.p1StocksRemaining = p1StocksRemaining;
                InstantiateStock(p1StocksRemaining, 1, StockPosition.LEFT, player1Material, player1Stocks);

                this.p2StocksRemaining = p2StocksRemaining;
                InstantiateStock(p2StocksRemaining, 2, StockPosition.RIGHT, player2Material, player2Stocks);
            }

            private void InstantiateStock(int stockCount, int playerNumber, StockPosition position, Material material, List<GameObject> emptyStocks)
            {
                var inset = position == StockPosition.LEFT ? STOCK_INSET : -STOCK_INSET;
                var startX = position == StockPosition.LEFT ? 0 : (worldTopRight.x - worldTopLeft.x) - (stockCount * (1 + STOCK_SPACING) + inset);
                for (var i = 0; i < stockCount; i++)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
                    meshRenderer.material = material;
                    sphere.transform.parent = stockHolder.transform;

                    var stockX = startX + i * STOCK_SPACING + inset;
                    sphere.transform.localPosition = new Vector3(stockX, 0);

                    emptyStocks.Add(sphere);
                }
            }

            private void AdjustCounts(int p1StocksRemaining, int p2StocksRemaining)
            {
                if (this.p1StocksRemaining != p1StocksRemaining)
                {
                    this.p1StocksRemaining = p1StocksRemaining;
                    AdjustCount(p1StocksRemaining, player1Stocks);
                }

                if (this.p2StocksRemaining != p2StocksRemaining)
                {
                    this.p2StocksRemaining = p2StocksRemaining;
                    AdjustCount(p2StocksRemaining, player2Stocks);
                }
            }

            private void AdjustCount(int newStockCount, List<GameObject> stocks)
            {
                for (var i = 0; i < stocks.Count; i++)
                {
                    var showStock = i < newStockCount;
                    stocks[i].SetActive(showStock);
                }
            }
        }

        #endregion
    }
}