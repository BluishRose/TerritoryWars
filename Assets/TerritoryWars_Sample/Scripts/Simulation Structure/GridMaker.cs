using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

[Serializable]
public class TerritoryWarGridSettings
{
    [Header("Prefabs")]
    public GameObject tilePrefab;

    [Header("Grid Size")]
    [Tooltip("Number of columns in the grid. (Horizontal Length")]
    public int gridWidth = 160;
    [Tooltip("Number of columns in the grid. (Vertical Length)")]
    public int gridHeight = 90;
    [Tooltip("Scale of the prefabs used to create the grid.")]
    public float scaleFactor = 1f;

    [Header("DEBUG")]
    public bool RebuildGridOnValidate = false;
    public bool BeginBattleOnSceneStart = false;
}

[Serializable]
public class TerritoryWarChaserSettings
{
    public List<TileChaserInfo> Chasers = new();
}


[Serializable]
public class TileChaserInfo
{
    public TileChaser Chaser;

    [Header("Status")]
    public int TileIndex;

    [Header("Start Settings")]
    public Vector2Int StartingPosition;
}

[Serializable]
public class TerritoryWarTimescaleSettings
{
    [Tooltip("Number of times to update chasers per second.")]
    [Range(0f, 0.5f)] public float tickSpeed = 0.2f;
    [Tooltip("If tickSpeed is set to 0, this determines how many turns to simulate before updating the graphic.")]
    [Range(1, 10)] public int frameSkip = 1;
}

public class GridMaker : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private TerritoryWarGridSettings GridSettings;
    [Header("Chaser Settings")]
    [SerializeField] private TerritoryWarChaserSettings ChaserSettings;
    [Header("Timescale Settings")]
    [SerializeField] private TerritoryWarTimescaleSettings TimeSettings;

    //Runtime Objects
    private List<TerritoryTile> tileInstances = new();

    private bool isBattleActive = false;
    private bool battleIsOver = false;

    #region Grid Building

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

    [ContextMenu("Rebuild Grid")]
    void RebuildGrid()
    {
        foreach (TerritoryTile tile in tileInstances)
        {
            Destroy(tile.gameObject);
        }

        tileInstances.Clear();

        for (int row = 0; row < GridSettings.gridHeight; row++)
        {
            for (int col = 0; col < GridSettings.gridWidth; col++)
            {
                //Position new tile gameobject based on row and column, and scale it by the scale factor
                Vector3 position = new Vector3(col * GridSettings.scaleFactor, row * GridSettings.scaleFactor, 0);
                GameObject tile = Instantiate(GridSettings.tilePrefab, position, Quaternion.identity);
                tile.transform.localScale = Vector3.one * GridSettings.scaleFactor;
                tile.transform.SetParent(this.transform);

                //Get the TerritoryTile component from the new tile gameobject and add it to the gridTiles list
                TerritoryTile tileComponent = tile.GetComponent<TerritoryTile>();
                tileInstances.Add(tileComponent);
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
        foreach(TileChaserInfo chaserInfo in ChaserSettings.Chasers)
        {
            int startingIndex = chaserInfo.StartingPosition.y * GridSettings.gridWidth + chaserInfo.StartingPosition.x;
            if (startingIndex < 0 || startingIndex >= tileInstances.Count)
            {
                Debug.LogError("StartBattle: Starting position for " + chaserInfo.Chaser.name + " is out of bounds.");
                return;
            }
            chaserInfo.TileIndex = startingIndex;
            ChaserClaimsTile(chaserInfo.Chaser, tileInstances[startingIndex], false);
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
                {
                    ChaserClaimsTile(move.Key.Chaser, move.Value, true);
                }
                move.Key.TileIndex = tileInstances.IndexOf(move.Value);
            }


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


    #endregion

    #region Turn Logic

    public void ChaserClaimsTile(TileChaser chaser, int tileIndex, bool checkForLoops)
    {
        if (tileIndex < 0 || tileIndex >= tileInstances.Count)
        {
            Debug.LogError("ChaserClaimsTile: Tile index out of bounds.");
            return;
        }
        ChaserClaimsTile(chaser, tileInstances[tileIndex], checkForLoops);
    }

    public void ChaserClaimsTile(TileChaser chaser, TerritoryTile tile, bool checkForLoops)
    {
        tile.SetOccupyingChaser(chaser);

        //Check if battle is over
        CheckWinConditions();

        if (checkForLoops)
        {
            //If battle is not over, check if any tiles adjacent to the one modified are capable of reaching other chasers. If not, that tile is also claimed.
            foreach (int adjacentIndex in GetAdjacentIndicies(tileInstances.IndexOf(tile), GridSettings.gridHeight, GridSettings.gridWidth))
            {
                if (!tileInstances[adjacentIndex].IsOccupied())
                {
                    if (UnclaimedTileHasDominantChaser(adjacentIndex, out TileChaser dominantChaser))
                    {
                        ChaserClaimsTile(dominantChaser, tileInstances[adjacentIndex], checkForLoops);
                    }
                }
            }
        }
    }

    private void CheckWinConditions()
    {
        foreach (TerritoryTile t in tileInstances)
        {
            if (!t.IsOccupied())
            {
                return;
            }
        }

        //battleIsOver = true;
    }


    #endregion

    public int CoordinatesToIndex(int row, int column)
    {
        if (row < 0 || row >= GridSettings.gridHeight || column < 0 || column >= GridSettings.gridWidth)
        {
            Debug.LogError("CoordinatesToIndex: Row or column index out of bounds.");
            return -1;
        }
        return row * GridSettings.gridWidth + column;
    }

    public Vector2Int IndexToCoordinates(int index)
    {
        if (index < 0 || index >= tileInstances.Count)
        {
            Debug.LogError("IndexToCoordinates: Tile index out of bounds.");
            return Vector2Int.zero;
        }
        return new Vector2Int(index % GridSettings.gridWidth, index / GridSettings.gridWidth);
    }

    public TerritoryTile GetTileAt(int row, int column)
    {
        if (row < 0 || row >= GridSettings.gridHeight || column < 0 || column >= GridSettings.gridWidth)
        {
            Debug.LogError("GetTileAt: Row or column index out of bounds.");
            return null;
        }
        int index = row * GridSettings.gridWidth + column;
        return tileInstances[index];
    }

    public bool UnclaimedTileHasDominantChaser(int startIndex, out TileChaser dominantChaser)
    {
        bool[] visited = new bool[tileInstances.Count];
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
            foreach(int nextNeighbor in GetAdjacentIndicies(currentIndex, GridSettings.gridHeight, GridSettings.gridWidth))
            {
                if (visited[nextNeighbor])
                    continue;

                TerritoryTile examinedTile = tileInstances[nextNeighbor];
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