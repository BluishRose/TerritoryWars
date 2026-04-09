using UnityEngine;

/// <summary>
/// Manages the visuals for a tile in the simulation
/// </summary>
public class TerritoryTile : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer borderRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Colors")]
    [SerializeField] private Color startingBorderColor;
    [SerializeField] private Color startingFillColor;

    private void Awake()
    {
        SetFillColor(startingFillColor);
        SetBorderColor(startingBorderColor);
    }

    public void SetNewColors(Color newBorderColor, Color newFillColor)
    {
        SetBorderColor(newBorderColor);
        SetFillColor(newFillColor);
    }

    public void ResetBorderColor()
    {
        SetBorderColor(startingBorderColor);
    }

    private void SetFillColor(Color color)
    {
        fillRenderer.color = color;
    }

    private void SetBorderColor(Color color)
    {
        borderRenderer.color = color;
    }

}
