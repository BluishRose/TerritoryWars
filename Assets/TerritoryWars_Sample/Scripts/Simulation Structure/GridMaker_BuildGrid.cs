using UnityEngine;

public partial class GridMaker : MonoBehaviour
{
    #region Grid Building

    [ContextMenu("Rebuild Grid")]
    void RebuildGrid()
    {
        //Destroy old tiles if they exist
        foreach (TerritoryTile tile in tileInstances)
        {
            Destroy(tile.gameObject);
        }
        tileInstances.Clear();
        tileOwnershipMap.Clear();

        //Create new grid of tiles based on settings
        for (int row = 0; row < GridHeight; row++)
        {
            for (int col = 0; col < GridWidth; col++)
            {
                //Position new tile gameobject based on row and column, and scale it by the scale factor
                Vector3 position = new(col * GridSettings.scaleFactor, row * GridSettings.scaleFactor, 0);
                GameObject tile = Instantiate(GridSettings.tilePrefab, position, Quaternion.identity);
                tile.transform.localScale = Vector3.one * GridSettings.scaleFactor;
                tile.transform.SetParent(transform);

                //Get the TerritoryTile component from the new tile gameobject and add it to the gridTiles list
                TerritoryTile tileComponent = tile.GetComponent<TerritoryTile>();
                tileInstances.Add(tileComponent);
                tileOwnershipMap.Add(col + (row * GridWidth), null);
            }
        }
    }


    #endregion

}