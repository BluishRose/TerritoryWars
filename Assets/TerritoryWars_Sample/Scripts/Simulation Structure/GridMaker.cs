using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEditor.U2D.Aseprite;
using System;

public class GridMaker : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject tilePrefab;
    public float scaleFactor = 1f;

    [Header("Grid Settings")]
    public bool autoRebuild = true;
    public int numRows = 9;
    public int numColumns = 16;

    [Header("Chaser Settings")]
    [SerializeField] private List<TileChaserInfo> chasers = new();
    private List<TileChaser> chaserInstances = new();
    ///Number of times to update chasers per second.
    [SerializeField, Range(0f, 0.5f)] private float tickSpeed = 0.2f;
    //If tickSpeed is set to 0, this determines how many turns to simulate before updating the graphic.
    [SerializeField, Range(1, 10)] private int frameSkip = 1;

    private List<TerritoryTile> gridTiles = new();

    private Dictionary<TileChaser, int> chaserPositions = new();

    private bool isBattleActive = false;
    private bool battleIsOver = false;

    #region Grid Building

    private void Awake()
    {
        RebuildGrid();
    }


    private void OnValidate()
    {
        if (Application.isPlaying && autoRebuild)
        {
            RebuildGrid();
        }
    }

    [ContextMenu("Rebuild Grid")]
    void RebuildGrid()
    {
        foreach (TerritoryTile tile in gridTiles)
        {
            Destroy(tile.gameObject);
        }

        gridTiles.Clear();

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numColumns; col++)
            {
                //Position new tile gameobject based on row and column, and scale it by the scale factor
                Vector3 position = new Vector3(col * scaleFactor, row * scaleFactor, 0);
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
                tile.transform.localScale = Vector3.one * scaleFactor;
                tile.transform.SetParent(this.transform);

                //Get the TerritoryTile component from the new tile gameobject and add it to the gridTiles list
                TerritoryTile tileComponent = tile.GetComponent<TerritoryTile>();
                gridTiles.Add(tileComponent);
            }
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

        //Move chasers to starting positions.
        foreach(TileChaserInfo chaserInfo in chasers)
        {
            int startingIndex = chaserInfo.StartingRow * numColumns + chaserInfo.StartingColumn;
            if (startingIndex < 0 || startingIndex >= gridTiles.Count)
            {
                Debug.LogError("StartBattle: Starting position for " + chaserInfo.Chaser.name + " is out of bounds.");
                return;
            }
            chaserPositions.Add(chaserInfo.Chaser, startingIndex);
            ChaserClaimsTile(chaserInfo.Chaser, gridTiles[startingIndex], false);
            chaserInstances.Add(chaserInfo.Chaser);
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
            foreach (TerritoryTile tile in gridTiles)
            {
                tile.ResetBorderColor();
            }

            //Get a valid tile for each chaser to move to.
            Dictionary<TileChaser, TerritoryTile> chaserMoves = new();

            //Get each tile's ideal moves to check for conflicts.
            foreach (TileChaser chaser in chaserInstances)
            {
                TerritoryTile chaserTile = chaser.DetermineIdealNextTile(gridTiles, numRows, numColumns, chaserPositions[chaser]);

                chaserMoves.Add(chaser, chaserTile);

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
                {
                    ChaserClaimsTile(move.Key, move.Value, true);
                }
                chaserPositions[move.Key] = gridTiles.IndexOf(move.Value);
            }



            //Wait for the next tick before sending chasers to new tiles again
            //if (tickSpeed == 0 && ++turnNumber % frameSkip == 0)
            if (tickSpeed == 0)
            {
                if (frameSkipCounter % frameSkip == 0)
                {
                    frameSkipCounter = 0;
                    yield return null;
                }
                frameSkipCounter++;
            }
            else yield return new WaitForSeconds(tickSpeed);
        }
    }


    #endregion

    #region Turn Logic

    public void ChaserClaimsTile(TileChaser chaser, int tileIndex, bool checkForLoops)
    {
        if (tileIndex < 0 || tileIndex >= gridTiles.Count)
        {
            Debug.LogError("ChaserClaimsTile: Tile index out of bounds.");
            return;
        }
        ChaserClaimsTile(chaser, gridTiles[tileIndex], checkForLoops);
    }

    public void ChaserClaimsTile(TileChaser chaser, TerritoryTile tile, bool checkForLoops)
    {
        tile.SetOccupyingChaser(chaser);

        //Check if battle is over
        CheckWinConditions();

        if (checkForLoops)
        {
            //If battle is not over, check if any tiles adjacent to the one modified are capable of reaching other chasers. If not, that tile is also claimed.
            foreach (int adjacentIndex in GetAdjacentIndicies(gridTiles.IndexOf(tile), numRows, numColumns))
            {
                if (!gridTiles[adjacentIndex].IsOccupied())
                {
                    if (UnclaimedTileHasDominantChaser(adjacentIndex, out TileChaser dominantChaser))
                    {
                        ChaserClaimsTile(dominantChaser, gridTiles[adjacentIndex], checkForLoops);
                    }
                }
            }
        }
    }

    private void CheckWinConditions()
    {
        foreach (TerritoryTile t in gridTiles)
        {
            if (!t.IsOccupied())
            {
                return;
            }
        }

        //battleIsOver = true;
    }


    #endregion

    public TerritoryTile GetTileAt(int row, int column)
    {
        if (row < 0 || row >= numRows || column < 0 || column >= numColumns)
        {
            Debug.LogError("GetTileAt: Row or column index out of bounds.");
            return null;
        }
        int index = row * numColumns + column;
        return gridTiles[index];
    }

    public bool UnclaimedTileHasDominantChaser(int startIndex, out TileChaser dominantChaser)
    {
        bool[] visited = new bool[gridTiles.Count];
        dominantChaser = null;

        Queue<int> queue = new Queue<int>();

        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        //While there are still adjacent tiles to check
        while (queue.Count > 0)
        {
            //Examine next tile
            int currentIndex = queue.Dequeue();

            //Examine adjacent tiles. 
            foreach(int nextNeighbor in GetAdjacentIndicies(currentIndex, numRows, numColumns))
            {
                if (visited[nextNeighbor])
                    continue;

                TerritoryTile examinedTile = gridTiles[nextNeighbor];
                //If any adjacent tile is occupied by a chaser
                if (examinedTile.IsOccupied())
                {
                    //If we haven't found a chaser yet, set foundChaser to the chaser occupying this tile and keep searching.
                    if (dominantChaser == null)
                        dominantChaser = examinedTile.OccupyingChaser;
                    //If we've already found a chaser and this tile is occupied by a different chaser, return true because this tile can reach multiple chasers.
                    else if (examinedTile.OccupyingChaser != dominantChaser)
                         return false;
                }
                //If the tile is not occupied by a chaser, add it to the queue to examine its neighbors later if we haven't already visited it.
                else
                {
                    visited[nextNeighbor] = true;
                    queue.Enqueue(nextNeighbor);
                }
            }
        }
        //If we've gone through all reachable tiles and only found one chaser, return it because this tile cannot reach multiple chasers.
        return true;
    }

    /// <summary>
    /// Returns a list of indicies corresponding to adjacent cells in cardinal directions. If a cell does not exist in a given direction, it is not included in the list.
    /// List includes all existing adjacent cells, even if they are occupied by a chaser or not valid for movement.
    /// <param name="tileIndex"></param>
    /// <param name="numRows"></param>
    /// <param name="numColumns"></param>
    /// <returns></returns>
    public static List<int> GetAdjacentIndicies(int tileIndex, int numRows, int numColumns)
    {
        List<int> adjacentTiles = new();
        int row = tileIndex / numColumns;
        int col = tileIndex % numColumns;
        //Get the tile in each direction if it exists. If it doesn't exist, skip it.
        if (row < numRows - 1)
        {
            adjacentTiles.Add(tileIndex + numColumns);
        }
        if (row > 0)
        {
            adjacentTiles.Add(tileIndex - numColumns);
        }
        if (col < numColumns - 1)
        {
            adjacentTiles.Add(tileIndex+1);
        }
        if (col > 0)
        {
            adjacentTiles.Add(tileIndex - 1);
        }
        return adjacentTiles;
    }

}

[Serializable]
public class TileChaserInfo
{
    public TileChaser Chaser;
    public int StartingRow;
    public int StartingColumn;
}