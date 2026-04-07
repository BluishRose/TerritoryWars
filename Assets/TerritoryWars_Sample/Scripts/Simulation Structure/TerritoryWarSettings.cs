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
    public int TerritorySize;

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