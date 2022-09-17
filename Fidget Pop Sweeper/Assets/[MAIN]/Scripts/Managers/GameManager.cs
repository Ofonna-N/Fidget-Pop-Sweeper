using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using MoreMountains.Feedbacks;

namespace FidgetSweeper
{
    public class GameManager : MonoBehaviour
    {
        // =========== GLOBAL VARIABLES
        [ShowInInspector, BoxGroup("Static Variables", showLabel:false), ReadOnly]
        public static GameManager instance;

        [SerializeField, BoxGroup("Static Variables")]
        private ResourcesManager resourcesManager;

        [SerializeField, BoxGroup("Static Variables")]
        private SmallyGames.Menus.RewardCoinsManager reward;

        [SerializeField, BoxGroup("Static Variables")]
        private SmallyGames.Settings.SettingsData settingsData;

        //======== ACTORS
        [SerializeField, BoxGroup("Actors", showLabel: false)]
        private Transform fidgetBoardHolder;

        [SerializeField, BoxGroup("Actors", showLabel: false)]
        private FidgetBoard fidgetBoard;
        public FidgetBoard FidgetBoard => fidgetBoard;

        [SerializeField, BoxGroup("Actors")]
        private LayerMask nodeLayer;

        [SerializeField, BoxGroup("Actors")]
        private LayerMask flagPickupCompLayer;

        [SerializeField, BoxGroup("Actors")]
        private LayerMask backgroundLayer;

        //========== UI DATA
        [BoxGroup("UI Data", showLabel: false)]
        [SerializeField, BoxGroup("UI Data/Mine", showLabel: false)]
        private TextMeshProUGUI mineCount_Text;


        [SerializeField, BoxGroup("UI Data/Slider", showLabel: false)]
        private SmallyGames.Menus.LevelSliderManager levelSlider;


        // == flag comp
        [SerializeField, BoxGroup("Flag Components", showLabel: false)]
        private int flagJumpCount = 1;

        [SerializeField, BoxGroup("Flag Components")]
        private float flagJumpPower = 2f;

        [SerializeField, BoxGroup("Flag Components")]
        private float flagretDur = .15f;

        [SerializeField, BoxGroup("Flag Components")]
        private float flagPlacementyOffset = 2f;

        [SerializeField, BoxGroup("Flag Components")]
        private Transform[] flagPool;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks flagJumpFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks flagLandFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks gameOverFeedback;

        //========= CAMERA DATA
        [SerializeField, BoxGroup("Cameras", showLabel: false)]
        private Camera mainCamera;

        //========= GAME STATE
        [ShowInInspector, BoxGroup("Game State", showLabel: false), ReadOnly]
        public static bool GameStarted = false;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        public static bool GameOver = false;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        public static bool IsFlagMode = false;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        public static bool CanClick = true;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        public bool InMainMenu { get; set; } = true;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        private int NumberNodeCount;

        [ShowInInspector, BoxGroup("Game State"), ReadOnly]
        private Transform curFlag;
        
        private System.Diagnostics.Stopwatch stopwatch;
        private System.TimeSpan timeSpan;

        private int flagCountRuntime;
        public int FlagCountRuntime 
        { 
            get => flagCountRuntime;
            set
            {
                flagCountRuntime = value;
                mineCount_Text.text = flagCountRuntime.ToString();
            }
        }

        [ShowInInspector]
        private List<MMFeedback> hapticFeedbacks;

        /// <summary>
        /// reset all static variables because they persist
        /// acrosss scenes then initializes our stop watch and
        /// game manager singleton and all items that need to be initialized
        /// </summary>
        private void Start()
        {
            GameOver = false;
            GameStarted = false;
            IsFlagMode = false;
            InMainMenu = true;
            CanClick = true;
            //flagClickHoldTime = Time.time;
            stopwatch = new System.Diagnostics.Stopwatch();

            if (instance == null)
            {
                instance = this;
            }

            InitFidgetBoard();
            InitFlag();
            fidgetBoard.TimeText.Text = "0:00";
            reward.CoinText.text = $"{reward.CoinsPrefix}{reward.CoinManager.Coin}";
            AddToHapticPool
                (
                    flagLandFeedback.Feedbacks.Find((x) => x.Label == StaticStrings.HapticLabel_Feedback)
                );
        }
        
        /// <summary>
        /// gets player input and updates our timer
        /// </summary>
        private void Update()
        {
            if (InMainMenu || GameOver || !CanClick) return;
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                ClickNode();
            }


            FlagUpdate();


            timeSpan = stopwatch.Elapsed;
            if (timeSpan.TotalSeconds <= 3600)
            {
                fidgetBoard.TimeText.UpdateText(string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds));
            }


        }

        /// <summary>
        /// what happens when a node is clicked?
        /// we fire a ray from the main camera, if the game has started then we go on as usual
        /// else we initialize our fidget board and start the stop watch
        /// </summary>
        private void ClickNode()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, nodeLayer, QueryTriggerInteraction.UseGlobal))
            {
                var node = hit.transform.GetComponentInParent<Node>();

                if (GameStarted)
                {
                    node.ClickNode(true);
                }
                else
                {
                    GameStarted = true;
                    fidgetBoard.Init(node);
                    stopwatch.Start();
                    //initialize number of number nodes 
                    for (int row = 0; row < fidgetBoard.Nodes.GetLength(0); row++)
                    {
                        for (int col = 0; col < fidgetBoard.Nodes.GetLength(1); col++)
                        {
                            if (fidgetBoard.Nodes[row,col].Type == NodeType.Number)
                            {
                                NumberNodeCount += 1;
                            }
                        }
                    }
                    node.ClickNode(false);
                    //levelSlider.Init
                    levelSlider.Init(NumberNodeCount, resourcesManager.Level.SelectedItem + 1);
                }
            }
        }


        /// <summary>
        /// spawns fidgetboard based on level, do not call init here
        /// </summary>
        private void InitFidgetBoard()
        {
            SmallyGames.Shop.Category curLevel = resourcesManager.Level;

            fidgetBoard = Instantiate(curLevel.InventoryItems[curLevel.SelectedItem].Prefab, fidgetBoardHolder, false).
                GetComponent<FidgetBoard>();

            levelSlider.Init(NumberNodeCount, resourcesManager.Level.SelectedItem + 1);
            //levelSlider.Init()
        }

        /// <summary>
        /// we initialize our flag pool, deactivating all flags and turining off the current 
        /// flag mnode icon
        /// </summary>
        private void InitFlag()
        {
            //mineCount_Text.text = fidgetBoard.MineCount.ToString();
            FlagCountRuntime = fidgetBoard.MineCount;

            for (int i = 0; i < flagPool.Length; i++)
            {
                flagPool[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// handles dragging, placing and dropping for flag
        /// </summary>
        private void FlagUpdate()
        {
            if (Input.GetButton(StaticStrings.MouseButton1_Button))
            {
                //Debug.Log("Flagging Check...");
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;

                if (!IsFlagMode)
                {
                    if (Physics.Raycast(ray, out hit, 100f, flagPickupCompLayer, QueryTriggerInteraction.UseGlobal))
                    {
                        if (hit.transform.gameObject.CompareTag(StaticStrings.Flag_Tag) && flagCountRuntime > 0)
                        {
                            //Debug.Log("Flagging Flag");
                            fidgetBoard.MainFlag.SetActive(false);
                            IsFlagMode = true;

                            if (curFlag == null)
                            {
                                //Debug.Log("Flag gotten");
                                curFlag = GetFlag();
                            }
                        }
                        else if (hit.transform.gameObject.CompareTag(StaticStrings.Node_Tag))
                        {
                            var node = hit.transform.GetComponentInParent<Node>();
                            if (!node.IsFlagged) return;
                            //Debug.Log("Flagging Node: " + node.name);
                            IsFlagMode = true;
                            //Debug.Log(curFlag == null);
                            if (curFlag == null)
                            {
                                curFlag = node.Flag;
                                node.SetFlagged(false, null);
                            }
                        }

                    }
                }

                if (curFlag != null)
                {
                    
                    //Vector3 flagPos = mainCamera.ScreenPointToRay(Input.mousePosition);
                    //Debug.Log(flagPos);
                    //flagPos.y = flagPlacementyOffset;
                    //flagPos.y = 2f;
                    //curFlag.position = Vector3.Lerp(curFlag.position, flagPos, 50f * Time.deltaTime);
                    RaycastHit bgHit;

                    if (Physics.Raycast(ray, out bgHit, 100f, backgroundLayer, QueryTriggerInteraction.UseGlobal))
                    {
                        Vector3 flagPos = bgHit.point;
                        flagPos.y = bgHit.point.y + flagPlacementyOffset;
                        curFlag.position = Vector3.Slerp(curFlag.position, flagPos, 20f * Time.deltaTime);
                    }
                    else
                    {
                        Debug.Log("Not hitting background");
                    }
                }


            }
            else
            {
                if (IsFlagMode)
                {
                    //Debug.Log("unflag");
                    Transform retFlag = curFlag;
                    curFlag = null;
                    IsFlagMode = false;
                    RaycastHit hit;
                    //flagClickHoldTime = Time.time;

                    if (Physics.Raycast(retFlag.transform.position, Vector3.down, out hit, 100f, nodeLayer, QueryTriggerInteraction.UseGlobal))
                    {
                        var node = hit.transform.GetComponentInParent<Node>();
                        //Debug.Log("Place on node");

                        if (node.IsFlagged || node.IsRevealed)
                        {
                            PlaceFlag(false, returnFlag: retFlag);
                        }
                        else
                        {
                            //Debug.Log("Place on node SUCCESS: " + node.name);
                            PlaceFlag(true, retFlag, node);
                            node.SetFlagged(true, retFlag);
                        }
                    }
                    else
                    {
                        PlaceFlag(false, returnFlag: retFlag);
                    }
                }
            }
        }


        /// <summary>
        /// used to place or remove flag 
        /// </summary>
        /// <param name="place">are we placing or removing flag</param>
        /// <param name="node">needed if we're placeing a flag, to place on land pos</param>
        /// <param name="returnFlag">needed if we're removing a flag to deactivate flag for pool</param>
        private void PlaceFlag(bool place, Transform returnFlag, Node node = null)
        {
            if (place)
            {
                //Debug.Log("Place Flag!");
                //Debug.Log("Flag: " + returnFlag.name);
                returnFlag.DOMove(node.FlagLandPos.position, flagretDur).
                            OnStart(() => flagJumpFeedback?.PlayFeedbacks()).
                            OnComplete(() =>
                            {
                                fidgetBoard.MainFlag.SetActive(true);
                                flagLandFeedback?.PlayFeedbacks();
                            });
            }
            else
            {
                FlagCountRuntime += 1;
                returnFlag.DOJump(FidgetBoard.MainFlag.transform.position, flagJumpPower,
                            flagJumpCount, flagretDur).
                            OnStart(() => flagJumpFeedback?.PlayFeedbacks()).
                            OnComplete(() =>
                            {
                                returnFlag.gameObject.SetActive(false);
                                //curFlag = null;
                                flagLandFeedback?.PlayFeedbacks();
                                fidgetBoard.MainFlag.SetActive(true);
                            });
            }
        }


        /// <summary>
        /// returns an active flag for flagging a node
        /// </summary>
        /// <returns></returns>
        public Transform GetFlag()
        {
            Transform retVal = null;
            FlagCountRuntime -= 1;

            for (int i = 0; i < flagPool.Length; i++)
            {
                if (!flagPool[i].gameObject.activeInHierarchy)
                {
                    retVal = flagPool[i];
                    //Debug.Log("Flag Available");
                    break;
                }
            }

            retVal.gameObject.SetActive(true);
            return retVal;
        }

        /// <summary>
        /// what happens when a node is revealed?
        /// we reduce the number node count variable to keep track of how
        /// many nodes have been revealed, to know when we've won the game
        /// </summary>
        public void OnNodeReveal()
        {
            NumberNodeCount -= 1;
            levelSlider.UpdateEnemyCount();
            if (NumberNodeCount <= 0)
            {
                fidgetBoard.GameOverFeedback.PlayFeedbacks();
                OnGameOver(true);
            }
        }

        /// <summary>
        /// what happens when game is over,
        /// send events to our doozy ui nody graph to
        /// bring up the win or lose view
        /// </summary>
        /// <param name="win"></param>
        public void OnGameOver(bool win)
        {
            GameOver = true;
            stopwatch.Stop();
            if (win)
            {
                Doozy.Engine.GameEventMessage.SendEvent(StaticStrings.PlayerWin_UIEvent);
                reward.CoinManager.Coin += 100;
                reward.CoinText.text = $"{reward.CoinsPrefix}{reward.CoinManager.Coin}";
                gameOverFeedback?.PlayFeedbacks();
                if (resourcesManager.Level.SelectedItem < resourcesManager.Level.InventoryItems.Length - 1)
                {
                    resourcesManager.Level.SelectedItem += 1;
                }
                else
                {
                    resourcesManager.Level.SelectedItem = 0;
                }
            }
            else
            {
                StartCoroutine(nameof(AwaitGameOverBombAction));
                Doozy.Engine.GameEventMessage.SendEvent(StaticStrings.PlayerLose_UIEvent);
            }
        }

        /// <summary>
        /// when game is over we explode and reveal mines in a wave
        /// </summary>
        /// <returns></returns>
        IEnumerator AwaitGameOverBombAction()
        {
            for (int row = 0; row < fidgetBoard.Nodes.GetLength(0); row++)
            {
                for (int col = 0; col < fidgetBoard.Nodes.GetLength(1); col++)
                {
                    if (fidgetBoard.Nodes[row, col].Type == NodeType.Mine)
                    {
                        yield return new WaitForSeconds(fidgetBoard.GameOverBlowDelay);
                        fidgetBoard.Nodes[row, col].ClickNode(false);
                    }
                }
            }
        }


        /// <summary>
        /// set vibrations off or on depending on our settings
        /// </summary>
        public void UpdateSettings()
        {
            if (hapticFeedbacks == null) return;
            for (int i = 0; i < hapticFeedbacks.Count; i++)
            {
                hapticFeedbacks[i].Active = settingsData.ViberateOn;
            }
        }

        /// <summary>
        /// adds a haptic feedback to our list of haptic feedbacks
        /// this is so that we can easily activate/deactivate the 
        /// feedback based on our game settings
        /// </summary>
        public void AddToHapticPool(params MMFeedback[] haptics)
        {
            if (hapticFeedbacks == null) hapticFeedbacks = new List<MMFeedback>();

            for (int i = 0; i < haptics.Length; i++)
            {
                hapticFeedbacks.Add(haptics[i]);
            }
        }

        /// <summary>
        /// used to load and restart scene
        /// </summary>
        /// <param name="scene"></param>
        public void LoadScene(string scene)
        {
            SceneManager.LoadScene(scene);
        }
    }


    /// <summary>
    /// node types
    /// </summary>
    public enum NodeType { Empty, Number, Mine}

    
}
