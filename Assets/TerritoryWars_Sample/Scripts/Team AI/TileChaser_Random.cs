using UnityEngine;
using System.Collections.Generic;

public class TileChaser_Random : TileChaser
{
    /// <summary>
    /// Chooses a random tile from the available adjacent tiles to move to. This algorithm can move on its own territory, but will not move onto enemy territory.
    /// </summary>
    /// <param name="allTiles"></param>
    /// <param name="gridRows"></param>
    /// <param name="gridColumns"></param>
    /// <param name="startingIndex"></param>
    /// <returns></returns>
    public override TerritoryTile DetermineIdealNextTile(List<TerritoryTile> allTiles, int startingIndex)
    {
        //From available tiles, pick a random tile to move to.
        Dictionary<int, TerritoryTile> tileOptions = new();

        int gridColumns = GridInstance.GridWidth;

        int currentRow = startingIndex / gridColumns;
        int currentCol = startingIndex % gridColumns;

        //North tile check
        if (currentRow < GridInstance.GridHeight - 1)
        {
            int tileIndex = startingIndex + gridColumns;

            if (tileIndex < allTiles.Count && (!GridInstance.TileIsOccupied(tileIndex, out TileChaser tileOwner) || Equals(tileOwner)))
                tileOptions.Add(tileIndex, allTiles[tileIndex]);
        }

        //South tile
        if (currentRow > 0)
        {
            int tileIndex = startingIndex - gridColumns;

            if (tileIndex >= 0 && (!GridInstance.TileIsOccupied(tileIndex, out TileChaser tileOwner) || Equals(tileOwner)))
                tileOptions.Add(tileIndex, allTiles[tileIndex]);
        }

        //East tile
        if (currentCol < gridColumns - 1)
        {
            int tileIndex = startingIndex + 1;

            if (tileIndex < allTiles.Count && (!GridInstance.TileIsOccupied(tileIndex, out TileChaser tileOwner) || Equals(tileOwner)))
                tileOptions.Add(tileIndex, allTiles[tileIndex]);
        }

        //West tile
        if (currentCol > 0)
        {
            int tileIndex = startingIndex - 1;

            if (tileIndex >= 0 && (!GridInstance.TileIsOccupied(tileIndex, out TileChaser tileOwner) || Equals(tileOwner)))
                tileOptions.Add(tileIndex, allTiles[tileIndex]);
        }

        //Check for validity
        if (tileOptions.Count == 0)
        {
            return null;
        }

        //Get a random element from the dictionary of options and return it
        List<TerritoryTile> tileOptionValues = new(tileOptions.Values);

        return tileOptionValues[Random.Range(0, tileOptionValues.Count)];
    }
}
