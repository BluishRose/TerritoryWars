using UnityEngine;
using System.Collections.Generic;

public class TileChaser_Random : TileChaser
{
    public override TerritoryTile DetermineIdealNextTile(List<TerritoryTile> allTiles, int gridRows, int gridColumns, int gridIndex)
    {
        //From available tiles, pick a random tile to move to.

        TerritoryTile northTile, southTile, eastTile, westTile;

        int currentRow = gridIndex / gridColumns;
        int currentCol = gridIndex % gridColumns;

        //Determine valid tiles to move to. Valid tiles are empty or self-owned        
        List<TerritoryTile> validTiles = new();

        northTile = (currentRow < gridRows - 1) ? allTiles[gridIndex + gridColumns] : null;
        southTile = (currentRow > 0) ? allTiles[gridIndex - gridColumns] : null;
        eastTile = (currentCol < gridColumns - 1) ? allTiles[gridIndex + 1] : null;
        westTile = (currentCol > 0) ? allTiles[gridIndex - 1] : null;

        //Performing isValidTile checks because this algorithm is allowed to travel on its own territory.

        if (northTile != null && northTile.IsValidTileForChaser(this))
        {
            validTiles.Add(northTile);
            //Debug.Log("North tile is valid");
        }
        if (southTile != null && southTile.IsValidTileForChaser(this))
        {
            validTiles.Add(southTile);
            //Debug.Log("South tile is valid");
        }
        if (eastTile != null && eastTile.IsValidTileForChaser(this))
        {
            validTiles.Add(eastTile);
            //Debug.Log("East tile is valid");
        }
        if (westTile != null && westTile.IsValidTileForChaser(this))
        {
            validTiles.Add(westTile);
            //Debug.Log("West tile is valid");
        }

        if (validTiles.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validTiles.Count);

        return validTiles[randomIndex];
    }
}
