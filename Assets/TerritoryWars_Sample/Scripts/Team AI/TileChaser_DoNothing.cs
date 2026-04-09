using UnityEngine;
using System.Collections.Generic;

public class TileChaser_DoNothing : TileChaser
{
    public override TerritoryTile DetermineIdealNextTile(List<TerritoryTile> allTiles, int startingIndex)
    {
        return null;
    }
}
