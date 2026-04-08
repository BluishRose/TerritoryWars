using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GridMaker : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private TerritoryWarGridSettings GridSettings;
    [Header("Chaser Settings")]
    [SerializeField] private TerritoryWarChaserSettings ChaserSettings;
    [Header("Timescale Settings")]
    [SerializeField] private TerritoryWarTimescaleSettings TimeSettings;

    //Runtime Objects
    private List<TerritoryTile> tileInstances = new();
    private Dictionary<int, TileChaserInfo> tileOwnershipMap = new();
    private int totalNumberTiles;

    private bool isBattleActive = false;
    private bool battleIsOver = false;

    #region Object Initialization
    private void Awake()
    {
        RebuildGrid();
    }

    void Start()
    {
        if (GridSettings.BeginBattleOnSceneStart)
        {
            StartBattle();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && GridSettings.RebuildGridOnValidate)
        {
            RebuildGrid();
        }
    }
    #endregion


    #region Battle Loop

    [ContextMenu("Start Battle")]
    void StartBattle()
    {
        if (isBattleActive)
        {
            Debug.LogWarning("Battle is already active.");
            return;
        }

        //Initialize chasers
        foreach (TileChaserInfo chaserInfo in ChaserSettings.Chasers)
        {
            //Calculate the starting index in the 1D array
            int startingIndex = chaserInfo.StartingPosition.y * GridSettings.gridWidth + chaserInfo.StartingPosition.x;
            if (startingIndex < 0 || startingIndex >= tileInstances.Count)
            {
                Debug.LogError("StartBattle: Starting position for " + chaserInfo.Chaser.name + " is out of bounds.");
                return;
            }

            chaserInfo.Chaser.Initialize(this);

            MoveChaserToIndex(chaserInfo, startingIndex);
        }

        isBattleActive = true;
        battleIsOver = false;

        StartCoroutine(TerritoryBattle());
    }

    private IEnumerator TerritoryBattle()
    {
        int turnNumber = 0;
        int frameSkipCounter = 1;

        while (!battleIsOver)
        {
            turnNumber++;
            Debug.Log("Turn " + turnNumber);

            //Before moving chasers, reset the border color of all tiles to make it easier to see where chasers are moving each turn.
            foreach (TerritoryTile tile in tileInstances)
            {
                tile.ResetBorderColor();
            }

            //Get a valid tile for each chaser to move to.
            Dictionary<TileChaserInfo, TerritoryTile> chaserMoves = new();

            //Get each tile's ideal moves to check for conflicts.
            foreach (TileChaserInfo chaserInfo in ChaserSettings.Chasers)
            {
                TileChaser chaser = chaserInfo.Chaser;

                TerritoryTile chaserTile = chaser.DetermineIdealNextTile(tileInstances, GridSettings.gridHeight, GridSettings.gridWidth, chaserInfo.TileIndex);

                chaserMoves.Add(chaserInfo, chaserTile);

                //foreach(var move in chaserMoves)
                //{
                //    if (move.Value == chaserTile)
                //    {
                //        chaserTile = null;
                //        break;
                //    }
                //}
            }

            //Move each chaser to their new tile if possible. If not possible, they will stay on their current tile.
            foreach (var move in chaserMoves)
            {
                if (move.Value != null)
                    MoveChaserToIndex(move.Key, tileInstances.IndexOf(move.Value));
            }

            //Check all unowned tiles to see if they are encapsulated by a single chaser.
            CheckForLoops();

            //Wait for the next tick before sending chasers to new tiles again
            //if (tickSpeed == 0 && ++turnNumber % frameSkip == 0)
            if (TimeSettings.tickSpeed == 0)
            {
                if (frameSkipCounter % TimeSettings.frameSkip == 0)
                {
                    frameSkipCounter = 0;
                    yield return null;
                }
                frameSkipCounter++;
            }
            else yield return new WaitForSeconds(TimeSettings.tickSpeed);
        }
    }

    private void MoveChaserToIndex(TileChaserInfo chaser, int newIndex)
    {
        //Make sure the tile moved to is flagged as claimed by the chaser
        ChaserClaimsTile(chaser, newIndex);

        //Update tile index of the chaser
        chaser.TileIndex = newIndex;
    }

    private void ChaserClaimsTile(TileChaserInfo chaser, int newIndex)
    {
        //If the new tile is already owned by the chaser, don't increment their score
        if (tileOwnershipMap[newIndex] != null && !tileOwnershipMap[newIndex].Equals(chaser.Chaser))
            chaser.TerritorySize++;

        //Update tile at new index visually
        tileInstances[newIndex].SetNewColors(chaser.Chaser.OverlapColor, chaser.Chaser.TeamColor);

        //Update ownership map
        tileOwnershipMap[newIndex] = chaser;
    }

    /// <summary>
    /// Scans the map to see if any chasers have encapsulated an area.
    /// </summary>
    private void CheckForLoops()
    {
        //Track tiles that have been visited so everything is evaluated only once
        bool[] visited = new bool[tileOwnershipMap.Count];

        //Iterate through all tiles
        for (int i = 0; i <  tileOwnershipMap.Count; i++)
        {
            //Ignore tiles we've already seen or are already owned
            if (visited[i] || tileOwnershipMap[i] != null)
                continue;

            //Begin generating a region that originates from this cell
            List<int> region = new();
            HashSet<TileChaserInfo> borderingTeams = new();
            
            //Build a new queue to track tile checking order and mark current cell as examined.
            Queue<int> queue = new();
            queue.Enqueue(i);
            visited[i] = true;

            //For each unexamined tile
            while (queue.Count > 0)
            {
                int currentIndex = queue.Dequeue();
                region.Add(currentIndex);

                //For each neighbor of the currently-examined tile
                foreach (int neighborIndex in GetAdjacentIndicies(currentIndex))
                {
                    //If tile is unowned
                    if (tileOwnershipMap[neighborIndex] == null)
                    {
                        //If the tile hasn't been visited already, add it to the queue to search later.
                        if (!visited[neighborIndex])
                        {
                            visited[neighborIndex] = true;
                            queue.Enqueue(neighborIndex);
                        }
                    }
                    //If tile is owned, add that to the list of bordering teams
                    else
                    {
                        borderingTeams.Add(tileOwnershipMap[neighborIndex]);
                    }
                }

            }

            //Evaluate the results of the region. If there is a single owner, it must be encapsulated.
            if (borderingTeams.Count == 1)
            {
                TileChaserInfo newOwner = borderingTeams.First();
                foreach(int index in region)
                    ChaserClaimsTile(newOwner, index);
            }

        }
    }

    #endregion

    #region Turn Logic

    /// <summary>
    /// Performs a series of checks to determine if a winner can be decided.
    /// </summary>
    private bool WarCanBeDecided()
    {
        //Quick check - if all tiles are claimed, there is nothing else to fight over.
        if (NumberOfClaimedTiles() == tileInstances.Count)
            return true;


        //TO DO: If all chasers have exhausted their options, war can be decided.


        return false;

        int NumberOfClaimedTiles()
        {
            int numClaimedTiles = 0;
            foreach (TileChaserInfo chaser in ChaserSettings.Chasers)
                numClaimedTiles += chaser.TerritorySize;
            return numClaimedTiles;
        }
    }

    public bool TileIsOccupied(int tileIndex, out TileChaser tileOwner)
    {
        tileOwner = tileOwnershipMap[tileIndex]?.Chaser;
        return tileOwner != null;
    }


    #endregion

    #region Utility Functions


    #endregion

    /// <summary>
    /// Returns a list of indicies corresponding to adjacent cells in cardinal directions. If a cell does not exist in a given direction, it is not included in the list.
    /// List includes all existing adjacent cells, even if they are occupied by a chaser or not valid for movement.
    /// <param name="tileIndex"></param>
    /// <param name="numRows"></param>
    /// <param name="numColumns"></param>
    /// <returns></returns>
    public List<int> GetAdjacentIndicies(int tileIndex)
    {
        List<int> adjacentTiles = new();
        int row = tileIndex / GridSettings.gridWidth;
        int col = tileIndex % GridSettings.gridWidth;
        //Get the tile in each direction if it exists. If it doesn't exist, skip it.
        if (row < GridSettings.gridHeight - 1)
            adjacentTiles.Add(tileIndex + GridSettings.gridWidth);
        if (row > 0)
            adjacentTiles.Add(tileIndex - GridSettings.gridWidth);
        if (col < GridSettings.gridWidth - 1)
            adjacentTiles.Add(tileIndex + 1);
        if (col > 0)
            adjacentTiles.Add(tileIndex - 1);
        return adjacentTiles;
    }


    

}