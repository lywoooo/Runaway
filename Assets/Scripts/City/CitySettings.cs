using UnityEngine;

public class CitySettings : MonoBehaviour
{
    [Header("References")]
    public int floorLayer = 7; // references floor layer (layer 7)

    [Header("Grid")]
    [Min(1)] public int width = 40;
    [Min(1)] public int height = 40;
    public float cellSize = 10f;

    [Header("Seed")]
    public bool randomSeed = true;
    public int seed = 12345;

    [Header("Road Config")]
    [Min(2)] public int arterialSpacing = 8;
    [Range(0f, 1f)] public float localStreetChance = 0.55f;
    [Range(0f, 1f)] public float streetBreakChance = 0.07f;
    [Min(0)] public int extraLoops = 30;
    [Min(0)] public int roadEdgeMargin = 1;
    [Min(0)] public int edgePadding = 1;
    [Min(2)] public int minManhattan = 40;
    public bool preferSpawnNearEdge = true;

    [Header("Road Prefabs")]
    public GameObject roadStraight;
    public GameObject roadCorner;
    public GameObject roadTJunction;
    public GameObject roadCross;
    public GameObject roadDeadEnd;

    [Header("Prefab Rotation Offsets (degrees)")]
    public float straightOffset = 0f;
    public float cornerOffset = 0f;
    public float tOffset = 0f;
    public float crossOffset = 0f;
    public float deadEndOffset = 0f;

    [Header("Placement")]
    public Transform roadsParent;
    public Transform buildingsParent;
    public Transform floorParent;
    public Vector3 origin = Vector3.zero;

    [Header("Spawn/Destination")]
    public Transform spawnMarker;
    public Transform destinationMarker;

    [Header("Generate")]
    public bool generateOnStart = true;

    [Header("Buildings")]
    public GameObject[] buildingPrefabs;
    public bool roadsideWall = true;
    [Range(0f, 1f)] public float buildingFill = 0.85f;
    public GameObject[] bigBuildingPrefabs;
    public bool bigBehindRoadWall = true;
    [Range(1, 6)] public int bigBehindLayers = 2;
    [Range(0f, 1f)] public float bigBehindChance = 0.75f;
    public bool bigPocketFillExtra = false;
    [Range(0f, 1f)] public float bigPocketFillChance = 0.20f;

    public Vector2Int[] bigBuildingSizes = new Vector2Int[]
    {
        new Vector2Int(3,3),
        new Vector2Int(3,2),
        new Vector2Int(2,2),
        new Vector2Int(4,2),
    };

    public bool fitBigBuildingsToGrid = true;

    [Header("Floor")]
    public bool buildFloor = true;
    [Min(0)] public int floorPaddingCells = 2;
    public float floorY = -0.01f;
    public bool floorHasCollider = true;
    public Material floorMaterial;
}
