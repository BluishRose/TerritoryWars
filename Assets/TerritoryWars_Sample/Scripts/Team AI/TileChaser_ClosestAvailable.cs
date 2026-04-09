using System.Collections.Generic;

public class TileChaser_ClosestAvailable : TileChaser
{
    /// <summary>
    /// Finds the closest available tile by searching in a "ring" pattern around the current tile. If a tile exists that can be traveled to, return it.
    /// </summary>
    /// <param name="allTiles"></param>
    /// <param name="gridRows"></param>
    /// <param name="gridColumns"></param>
    /// <param name="startingIndex"></param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public override TerritoryTile DetermineIdealNextTile(List<TerritoryTile> allTiles, int startingIndex)
    {
        //Check to see if any adjacent tiles are valid. If so, return one of them.
        List<int> adjacentTiles = GridInstance.GetAdjacentIndicies(startingIndex);
        if (adjacentTiles.Count > 0)
            foreach (int adjacentIndex in adjacentTiles)
                if (!GridInstance.TileIsOccupied(adjacentIndex, out _))
                    return allTiles[adjacentIndex];

        //If no immediate tiles are valid, look along borders of territory for the closest available tile that can be reached by traveling on self-owned tiles.
        Queue<int> tilesToCheck = new();
        HashSet<int> visitedTiles = new();

        //Start BFS from current tile
        tilesToCheck.Enqueue(startingIndex);
        visitedTiles.Add(startingIndex);

        //Store our destination index
        int destinationIndex = -1;

        //Scan tiles
        while (tilesToCheck.Count > 0 && destinationIndex == -1)
        {
            int currentIndex = tilesToCheck.Dequeue();
            List<int> neighbors = GridInstance.GetAdjacentIndicies(currentIndex);
            foreach (int neighborIndex in neighbors)
            {
                //Ignore already visited tiles
                if (visitedTiles.Contains(neighborIndex))
                    continue;
                visitedTiles.Add(neighborIndex);
                //If we can move to this tile, mark it as the closest available tile.

                bool tileOccupied = GridInstance.TileIsOccupied(neighborIndex, out TileChaser owner);

                if (!tileOccupied)
                {
                    destinationIndex = neighborIndex;
                    break;
                }
                //Otherwise, if this tile is owned by us, add it to the queue to continue searching from there.
                if (owner.Equals(this))
                    tilesToCheck.Enqueue(neighborIndex);
            }
        }

        //If we found a destination tile, get the next step towards it. Otherwise, return null to indicate no valid tiles.
        if (destinationIndex != -1)
        {
            int nextStepIndex = FindBestStep(allTiles, startingIndex, destinationIndex);
            //If there is no next step to take, return null.
            if (nextStepIndex != -1)
                return allTiles[nextStepIndex];
        }

        return null;

    }
}
