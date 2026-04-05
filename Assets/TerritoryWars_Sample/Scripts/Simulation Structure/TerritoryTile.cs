using UnityEngine;

public class TerritoryTile : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Colors")]
    [SerializeField] private Color startingBorderColor;
    [SerializeField] private Color startingFillColor;

    [Header("Territory Info")]
    [SerializeField] private TileChaser occupyingChaser = null;
    public TileChaser OccupyingChaser => occupyingChaser;

    private GridMaker grid;

    private void Awake()
    {
        SetFillColor(startingFillColor);
        SetBorderColor(startingBorderColor);
    }


    public void Init(GridMaker grid)
    {
        this.grid = grid;
    }

    public bool IsOccupied()
    {
        return occupyingChaser != null;
    }

    public bool IsValidTileForChaser(TileChaser chaser)
    {
        //A tile is valid for a chaser to move to if it is not occupied by another chaser
        return !IsOccupied() || occupyingChaser == chaser;
    }

    public void SetOccupyingChaser(TileChaser chaser)
    {
        //Make sure the border color is updated to help keep track of where chasers are on the grid.

        occupyingChaser = chaser;
        SetFillColor(chaser.TeamColor);
        SetBorderColor(chaser.OverlapColor);
    }

    public void ResetBorderColor()
    {
        SetBorderColor(startingBorderColor);
    }

    void SetFillColor(Color color)
    {
        fillRenderer.color = color;
    }

    void SetBorderColor(Color color)
    {
        borderRenderer.color = color;
    }

}
