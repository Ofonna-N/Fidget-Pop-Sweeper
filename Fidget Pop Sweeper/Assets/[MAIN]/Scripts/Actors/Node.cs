using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;
using MoreMountains.Feedbacks;
//using MText;

namespace FidgetSweeper
{
    [PropertyTooltip("@gameObject.name")]
    public class Node : MonoBehaviour
    {

        [SerializeField, BoxGroup("ref", showLabel: false)]
        private MText.Modular3DText numberText;
        //[SerializeField, BoxGroup("ref", showLabel: false)]
        //private TextMeshPro numberText;

        [SerializeField, BoxGroup("ref")]
        private GameObject cover;

        [SerializeField, BoxGroup("ref")]
        private GameObject mine;

        [SerializeField, BoxGroup("ref")]
        private Transform flagLandPos;
        public Transform FlagLandPos => flagLandPos;

        [SerializeField, BoxGroup("Feedbacks", showLabel: false)]
        private MMFeedbacks popFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks explosionFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks smokeFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks numClickFeedback;

        /*[SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks flagJumpFeedback;
        //public MMFeedbacks FlagJumpFeedback => flagJumpFeedback;

        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks flagLandFeedback;
        //public MMFeedbacks FlagLandFeedback => flagLandFeedback;*/


        [SerializeField, BoxGroup("Feedbacks")]
        private MMFeedbacks neighborCheckFeedback;

        [SerializeField, ReadOnly, BoxGroup("runtime", showLabel: false)]
        private NodeType type;
        public NodeType Type => type;

        [SerializeField, ReadOnly, BoxGroup("runtime")]
        private List<Node> neighbors;
        public List<Node> Neighbors => neighbors;


        [SerializeField, ReadOnly, BoxGroup("runtime")]
        private bool isFlagged;
        public bool IsFlagged => isFlagged;


        [SerializeField, ReadOnly, BoxGroup("runtime")]
        private bool isRevealed;
        public bool IsRevealed => isRevealed;

        [SerializeField, ReadOnly, BoxGroup("runtime")]
        private bool initialized;
        public bool Initialized => initialized;

        [SerializeField, ReadOnly, BoxGroup("runtime")]
        private Transform runtimeFlag;
        public Transform Flag => runtimeFlag;

        private void Awake()
        {
            numberText.gameObject.SetActive(false);
            //cover.SetActive(false);
            mine.SetActive(false);
            
        }

        private void Init()
        {
            GameManager.instance.AddToHapticPool
                (
                    popFeedback.Feedbacks.Find((x) => x.Label == StaticStrings.HapticLabel_Feedback),
                    explosionFeedback.Feedbacks.Find((x) => x.Label == StaticStrings.HapticLabel_Feedback),
                    smokeFeedback.Feedbacks.Find((x) => x.Label == StaticStrings.HapticLabel_Feedback),
                    neighborCheckFeedback.Feedbacks.Find((x) => x.Label == StaticStrings.HapticLabel_Feedback)
                );
        }

        public void SetNumber(int num, List<Node> n)
        {
            initialized = true;
            numberText.gameObject.SetActive(true);
            //numberText.text = num.ToString();
            numberText.UpdateText(num.ToString());
            neighbors = n;
            type = NodeType.Number;
            Init();
            //print(gameObject.name)
        }

        public void SetMine()
        {
            initialized = true;
            mine.SetActive(true);
            type = NodeType.Mine;
            Init();
        }

        public void SetEmpty(List<Node> n)
        {
            initialized = true;
            neighbors = n;
            type = NodeType.Empty;
            Init();
        }

        public void SetFlagged(bool flagged, Transform flag)
        {
            isFlagged = flagged;
            runtimeFlag = flag;
        }

        public void ClickNode(bool playerClicked)
        {
            //if (CheckFlagAction() && !GameManager.GameOver) return; // check if we use flag
            if (isFlagged && !GameManager.GameOver) return;
            if (!isRevealed)
            {
                RevealNode();
                popFeedback?.PlayFeedbacks();

                //Debug.Log("Neighbors: " + neighbors.Count);
                StartCoroutine(nameof(AwaitClickNeighbors)); // reveal neighbors in waves
            }
            else if(isRevealed && playerClicked)
            {
                if (type == NodeType.Number)
                {
                    Debug.Log("Animate Neighbors" + name);
                    numClickFeedback?.PlayFeedbacks();
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        neighbors[i].Wiggle();
                    }
                }
            }
        }

        /// <summary>
        /// there are certain points in the game where a node will be revealed by its neighbors
        /// and instead of revealing all at once we do it in a wave by waiting for a specific amount of time
        /// </summary>
        /// <returns></returns>
        IEnumerator AwaitClickNeighbors()
        {
            GameManager.CanClick = false;
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (type == NodeType.Empty)
                {
                    if (!neighbors[i].IsRevealed)
                    {
                        yield return new WaitForSeconds(GameManager.instance.FidgetBoard.NeighboreNodeRevealDelay);
                        neighbors[i].ClickNode(false);
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }

            GameManager.CanClick = true;
        }

        /// <summary>
        /// this function is used to check if any we make the use of the flag 
        /// in any way, if so return true to avoid any action on the node
        /// </summary>
        /// <returns></returns>
        private bool CheckFlagAction()
        {
            bool retVal = false;

            if ((isFlagged && !GameManager.IsFlagMode) || (!isFlagged && GameManager.IsFlagMode && GameManager.instance.FlagCountRuntime <= 0))
            {
                //do nothing
                retVal = true;
                Debug.Log("Ignore Flag Action");
            }
            else if (isFlagged && GameManager.IsFlagMode)
            {
                //remove flag
                /*GameManager.instance.FlagCountRuntime += 1; // replace flag
                runtimeFlag.DOJump(GameManager.instance.FidgetBoard.MainFlag.transform.position, GameManager.instance.FidgetBoard.JumpPower,
                    GameManager.instance.FidgetBoard.JumpCount, GameManager.instance.FidgetBoard.JumpAnimSpeed).
                    OnStart(()=> flagJumpFeedback?.PlayFeedbacks()).
                    OnComplete(() =>
                    {
                        runtimeFlag.gameObject.SetActive(false);
                        flagLandFeedback?.PlayFeedbacks();
                    });*/
                
                //isFlagged = false;
                retVal = true;
                Debug.Log("Remove Flag");
            }
            else if (!isFlagged && GameManager.IsFlagMode && GameManager.instance.FlagCountRuntime > 0 && !isRevealed)
            {
                //placeflag
                /*runtimeFlag = GameManager.instance.GetFlag();

                runtimeFlag.DOJump(flagLandPos.position, GameManager.instance.FidgetBoard.JumpPower,
                    GameManager.instance.FidgetBoard.JumpCount, GameManager.instance.FidgetBoard.JumpAnimSpeed).
                    OnStart(() => flagJumpFeedback?.PlayFeedbacks()).
                    OnComplete(()=> flagLandFeedback?.PlayFeedbacks());*/

                //isFlagged = true;
                retVal = true;
                Debug.Log("Place Flag");
            }

            return retVal;
        }



        private void RevealNode()
        {
            if (!isFlagged)
            {
                cover.SetActive(false);
                isRevealed = true;
            }

            switch (type)
            {
                case NodeType.Empty:
                    //event on empty node revealed
                    break;
                case NodeType.Number:
                    GameManager.instance.OnNodeReveal();
                    break;
                case NodeType.Mine:
                    if (!GameManager.GameOver)
                    {
                        GameManager.instance.OnGameOver(false);
                    }
                    if (!isFlagged)
                    {
                        explosionFeedback?.PlayFeedbacks();
                    }
                    else
                    {
                        //Debug.Log("Smoke Feedback played");
                        smokeFeedback?.PlayFeedbacks();
                    }
                    break;
                default:
                    break;
            }
        }

        public void Wiggle()
        {
            neighborCheckFeedback.PlayFeedbacks();
        }


#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            //UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(transform.position + (Vector3.up * .5f), name, new GUIStyle() { alignment = TextAnchor.MiddleCenter});
        }
#endif
    }
}
