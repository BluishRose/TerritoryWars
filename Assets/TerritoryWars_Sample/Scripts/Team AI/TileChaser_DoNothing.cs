using UnityEngine;
using System.Collections.Generic;

public class TileChaser_DoNothing : TileChaser
{
    public override int DetermineIdealNextTile(List<TerritoryTile> allTiles, int startingIndex)
    {
        return -1;
    }
}
