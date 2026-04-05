using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

public abstract class TileChaser : MonoBehaviour
{
    [Header("Team Information")]
    public Color TeamColor = Color.black;
    public Color OverlapColor = Color.black;

    //Determine the best tile this algorithm should move to next and return that tile. Assume that chaser will start on a valid position, so current position never needs to be checked.
    public abstract TerritoryTile DetermineIdealNextTile(List<TerritoryTile> allTiles, int gridRows, int gridColumns, int gridIndex);

    /// <summary>
    /// Returns the first step of the best path to the desired tile using A* pathfinding.
    /// </summary>
    /// <param name="allTiles"></param>
    /// <param name="gridRows"></param>
    /// <param name="gridColumns"></param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns></returns>
    protected int FindBestStep(List<TerritoryTile> allTiles, int gridRows, int gridColumns, int startIndex, int endIndex)
    {
        //Track the best path so far to the end tile
        Dictionary<int, int> bestPathToTile = new();

        //Using a priority queue (of sorts) to track what tiles to check next
        Dictionary<int, float> tilesToCheckWithPriority = new();
        tilesToCheckWithPriority.Add(startIndex, 0);

        List<int> visitedTiles = new();

        //Tile scores
        //Gscore is the cost to get from start tile to end tile
        Dictionary<int, float> gScores = new();
        gScores.Add(startIndex, 0);

        //Fscore is the cost to get from start to end tile using the best path so far.
        Dictionary<int, float> fScores = new();
        fScores.Add(startIndex, HeuristicCostEstimate(startIndex, endIndex, gridColumns));

        while (tilesToCheckWithPriority.Count > 0)
        {
            //Get tile index with lowest fScore to check next
            int nextTileIndex = Dequeue(tilesToCheckWithPriority);

            //Skip if we already visited this tile
            if (visitedTiles.Contains(nextTileIndex))
                continue;

            //If we reached the end tile, reconstruct the path and return it.
            if (nextTileIndex == endIndex)
            {
                List<int> path = new();
                int currentTileIndex = endIndex;
                while (currentTileIndex != startIndex)
                {
                    path.Add(currentTileIndex);
                    currentTileIndex = bestPathToTile[currentTileIndex];
                }
                path.Reverse();
                return path[0];
            }

            //If not done, check tile neighbors to update scores and checkable tiles
            foreach(int neighborIndex in GridMaker.GetAdjacentIndicies(nextTileIndex, gridRows, gridColumns))
            {
                //Ignore already visited tiles
                if (visitedTiles.Contains(neighborIndex))
                    continue;
                //Ignore invalid tiles
                if (!allTiles[neighborIndex].IsValidTileForChaser(this))
                    continue;
                //Starting score equals current score plus 1 (all moves have equal cost).
                float tentativeGScore = gScores[nextTileIndex] + 1;

                //If this path to the neighbor tile is better than any previously recorded path, update the best path and scores for this tile.
                if (!gScores.ContainsKey(neighborIndex) || tentativeGScore < gScores[neighborIndex])
                {
                    bestPathToTile[neighborIndex] = nextTileIndex;
                    gScores[neighborIndex] = tentativeGScore;
                    fScores[neighborIndex] = tentativeGScore + HeuristicCostEstimate(neighborIndex, endIndex, gridColumns);
                    if (!tilesToCheckWithPriority.ContainsKey(neighborIndex))
                        tilesToCheckWithPriority.Add(neighborIndex, fScores[neighborIndex]);
                }
            }

            //Flag this tile as being visited so we don't check it again
            visitedTiles.Add(nextTileIndex);
        }

        return -1;
    }

    private static float HeuristicCostEstimate(int tileIndex, int endIndex, int gridColumns)
    {
        //Using Manhattan distance as heuristic cost estimate since we can only move in 4 directions.
        return Mathf.Abs((tileIndex / gridColumns) - (endIndex / gridColumns)) + Mathf.Abs((tileIndex % gridColumns) - (endIndex % gridColumns));
    }

    private static int Dequeue(Dictionary<int, float> priorityQueue)
    {
        int bestIndex = -1;
        foreach(KeyValuePair<int, float> kvp in priorityQueue)
        {
            if (bestIndex == -1 || kvp.Value < priorityQueue[bestIndex])
                bestIndex = kvp.Key;
        }
        if (bestIndex != -1)
            priorityQueue.Remove(bestIndex);
        return bestIndex;
    }
}