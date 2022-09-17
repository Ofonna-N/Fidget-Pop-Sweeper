using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;

namespace FidgetSweeper
{
    public class FidgetBoard : SerializedMonoBehaviour
    {
        //========== SCENE COMPONENTS

        [SerializeField, BoxGroup("Time Data", showLabel: false)]
        private MText.Modular3DText timeText;
        public MText.Modular3DText TimeText => timeText;

        [SerializeField, BoxGroup("Bomb Data", showLabel:false)]
        private float gameOverBlowDelay = .5f;
        public float GameOverBlowDelay => gameOverBlowDelay;

        [SerializeField, BoxGroup("Node Data", showLabel: false)]
        private float neighborNodeRevealDelay = .1f;
        public float NeighboreNodeRevealDelay => neighborNodeRevealDelay;

        [SerializeField, BoxGroup("Flag Data", showLabel: false)]
        private GameObject mainFlag;
        public GameObject MainFlag => mainFlag;


        [SerializeField, BoxGroup("Feedbacks Data", showLabel: false)]
        private MMFeedbacks gameOverFeedback;
        public MMFeedbacks GameOverFeedback => gameOverFeedback;

        [SerializeField, BoxGroup("Board", showLabel:false)]
        private int mineCount = 2;
        public int MineCount => mineCount;

        [SerializeField, BoxGroup("Board"), TableMatrix(HorizontalTitle = "column", VerticalTitle = "row", SquareCells = true)]
        private Node[,] nodes;
        public Node[,] Nodes => nodes;

        private Node initNode;

        /// <summary>
        /// initialize the fidget board
        /// </summary>
        /// <param name="node"></param>
        public void Init(Node node)
        {
            initNode = node;

            InitClickedNode(); // initialize the fist clicked node

            InitializeMinesNodes(); //now instert mines
            InitializeNumbersNodes(); //now instert numbers
            GameManager.instance.UpdateSettings();
        }


        /// <summary>
        /// the fist node clicked by the player must be initialized after
        /// to avoid the player losing on first click
        /// </summary>
        private void InitClickedNode()
        {
            for (int row = 0; row < nodes.GetLength(0); row++)
            {
                for (int col = 0; col < nodes.GetLength(1); col++)
                {
                    if (nodes[row, col] == initNode)
                    {
                        var neighbors = GetNeighbors(row, col);

                        nodes[row, col].SetEmpty(neighbors);
                    }
                }
            }
        }

        /// <summary>
        /// class to store positions on a 2 dimensional array
        /// </summary>
        public class NodeDimension
        {
            Node node;
            public Node Node => node;
            int row;
            public int Row => row;
            int col;
            public int Col => col;

            public NodeDimension(Node n, int r, int c)
            {
                node = n;
                row = r;
                col = c;
            }
        }

        /// <summary>
        /// initialize mindes by placing them in random spots in the board
        /// </summary>
        public void InitializeMinesNodes()
        {
            List<NodeDimension> possibleMinePos = new List<NodeDimension>();

            for (int r = 0; r < nodes.GetLength(0); r++)
            {
                for (int c = 0; c < nodes.GetLength(1); c++)
                {
                    if (nodes[r, c].Initialized || initNode.Neighbors.Contains(nodes[r, c])) continue;
                    possibleMinePos.Add(new NodeDimension(nodes[r, c], r, c));
                }
            }

            for (int i = 0; i < mineCount; i++)
            {

                int randNode = Random.Range(0, possibleMinePos.Count);      //get random row
                
                var neighbors = GetNeighbors(possibleMinePos[randNode].Row, possibleMinePos[randNode].Col);

                var node = possibleMinePos[randNode].Node;

                node.SetMine();

                possibleMinePos.Remove(possibleMinePos[randNode]);
                /*int rowRand = Random.Range(0, nodes.GetLength(0) - 1);      //get random row
                int colRand = Random.Range(0, nodes.GetLength(1) - 1);      //get random column
                var neighbors = GetNeighbors(rowRand, colRand);
                

                if (nodes[rowRand, colRand].Initialized || initNode.Neighbors.Contains(nodes[rowRand, colRand]))
                {
                    i -= 1;
                    
                    continue;
                }
                var node = nodes[rowRand, colRand];
                
                node.SetMine();*/

            }
        }

        /// <summary>
        /// initialize nodes with numbers
        /// </summary>
        public void InitializeNumbersNodes()
        {
            for (int row = 0; row < nodes.GetLength(0); row++)
            {
                for (int col = 0; col < nodes.GetLength(1); col++)
                {
                    if (nodes[row, col].Type == NodeType.Empty && !nodes[row, col].Initialized)
                    {
                        var neighbors = GetNeighbors(row, col);

                        int mineCount = 0;

                        for (int i = 0; i < neighbors.Count; i++)
                        {
                            if (neighbors[i].Type == NodeType.Mine)
                            {
                                mineCount += 1;
                            }
                        }

                        if (mineCount > 0)
                        {
                            var node = nodes[row, col];
                            node.SetNumber(mineCount, neighbors);
                        }
                        else
                        {
                            nodes[row, col].SetEmpty(neighbors);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// initialize the empty nodes
        /// if nodes is empty, initialize node and save neighbors
        /// make sure to call this last
        /// </summary>
        /*public void InitializeEmptyNodes()
        {
            for (int row = 0; row < nodes.GetLength(0); row++)
            {
                for (int col = 0; col < nodes.GetLength(1); col++)
                {
                    if (nodes[row, col].Type == NodeType.Empty)
                    {
                        var neighbors = GetNeighbors(row, col);

                        nodes[row, col].SetEmpty(neighbors);
                    }
                }
            }
        }*/

        /// <summary>
        /// this function is used to get neighbors of a specific node
        /// in the board
        /// </summary>
        /// <param name="row"> the row of the node passed</param>
        /// <param name="col"> the column of the row passed</param>
        /// <returns></returns>
        private List<Node> GetNeighbors(int row, int col)
        {
            var retVal = new List<Node>();

            // All Lefts
            if (col > 0) //can go left
            {
                //Left
                retVal.Add(nodes[row, col - 1]);

                //top Left
                if(row > 0)
                {
                    retVal.Add(nodes[row - 1, col - 1 ]);
                }

                //Bottom Left
                if (row < nodes.GetLength(0) - 1)
                {
                    retVal.Add(nodes[row + 1, col - 1]);
                }
            }

            //All Rights
            if (col < nodes.GetLength(1) - 1)
            {
                //Right
                retVal.Add(nodes[row, col + 1]);

                //Top Right
                if (row > 0)
                {
                    retVal.Add(nodes[row - 1, col + 1]);
                }

                //Bottom Right
                if (row < nodes.GetLength(0) - 1)
                {
                    retVal.Add(nodes[row + 1, col + 1]);
                }
            }

            //top
            if (row > 0)
            {
                retVal.Add(nodes[row - 1, col]);
            }

            //down
            if (row < nodes.GetLength(0) - 1)
            {
                retVal.Add(nodes[row + 1, col]);
            }

            return retVal;
        }
    }
}
