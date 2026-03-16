using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class CityBuilder : MonoBehaviour
{
    [SerializeField] private CitySettings settings;
    [SerializeField] private NavMeshSurface nm;
    public bool ready {  get; private set; }


    public CityGridData Grid { get; private set; }
    public Vector2Int SpawnCell { get; private set; }
    public Vector2Int DestCell { get; private set; }
    public int LastSeed { get; private set; }

    public Vector3 SpawnWorld { get; private set; }
    public Vector3 DestWorld { get; private set; }

    private GameObject _floorInstance;

    private void Awake()
    {
        if (!settings) settings = GetComponent<CitySettings>();
        if (settings && settings.generateOnStart) GenerateAndBuild();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public void GenerateAndBuild()
    {
        ready = false; 

        if (!settings) return;

        CityGen core = new CityGen();
        CityGen.Result res = core.Generate(settings);

        Grid = res.grid;
        SpawnCell = res.spawn;
        DestCell = res.dest;
        LastSeed = res.seed;

        BuildRoads();
        BuildFloor();
        BuildBuildings();

        SpawnWorld = CellToWorld(SpawnCell, 0.5f);
        DestWorld = CellToWorld(DestCell, 0.5f);

        if (settings.spawnMarker) settings.spawnMarker.position = SpawnWorld;
        if (settings.destinationMarker) settings.destinationMarker.position = DestWorld;


        if (!nm) nm = FindFirstObjectByType<NavMeshSurface>();
        if (nm) nm.BuildNavMesh();

        ready = true;
    }

    private Vector3 CellToWorld(Vector2Int c, float y)
    {
        float cellSize = Mathf.Max(0.01f, settings.cellSize);
        return settings.origin + new Vector3(c.x * cellSize, y, c.y * cellSize);
    }

    private void BuildRoads()
    {
        Transform parent = settings.roadsParent ? settings.roadsParent : transform;
        ClearChildren(parent);

        for (int y = 0; y < Grid.height; y++)
            for (int x = 0; x < Grid.width; x++)
            {
                if (!Grid.IsRoad(x, y)) continue;

                var c = Grid.GetConn(x, y);
                int deg = Grid.Degree(x, y);

                var (prefab, rotY) = PickRoadPrefab(c, deg);
                if (!prefab) continue;

                Vector3 pos = settings.origin + new Vector3(x * settings.cellSize, 0f, y * settings.cellSize);
                var go = Instantiate(prefab, pos, Quaternion.Euler(0f, rotY, 0f), parent);
                go.name = $"Road_{x}_{y}";
            }
    }

    private (GameObject prefab, float rotY) PickRoadPrefab(CityGridData.Conn c, int deg)
    {
        bool n = (c & CityGridData.Conn.N) != 0;
        bool e = (c & CityGridData.Conn.E) != 0;
        bool s = (c & CityGridData.Conn.S) != 0;
        bool w = (c & CityGridData.Conn.W) != 0;

        if (deg == 4 && settings.roadCross)
            return (settings.roadCross, 0f + settings.crossOffset);

        if (deg == 3 && settings.roadTJunction)
        {
            if (!n) return (settings.roadTJunction, 180f + settings.tOffset);
            if (!e) return (settings.roadTJunction, 270f + settings.tOffset);
            if (!s) return (settings.roadTJunction, 0f + settings.tOffset);
            return (settings.roadTJunction, 90f + settings.tOffset);
        }

        if (deg == 2)
        {
            if (n && s && settings.roadStraight) return (settings.roadStraight, 0f + settings.straightOffset);
            if (e && w && settings.roadStraight) return (settings.roadStraight, 90f + settings.straightOffset);

            if (settings.roadCorner)
            {
                if (n && e) return (settings.roadCorner, 0f + settings.cornerOffset);
                if (e && s) return (settings.roadCorner, 90f + settings.cornerOffset);
                if (s && w) return (settings.roadCorner, 180f + settings.cornerOffset);
                if (w && n) return (settings.roadCorner, 270f + settings.cornerOffset);
            }
        }

        if (deg == 1 && settings.roadDeadEnd)
        {
            if (n) return (settings.roadDeadEnd, 0f + settings.deadEndOffset);
            if (e) return (settings.roadDeadEnd, 90f + settings.deadEndOffset);
            if (s) return (settings.roadDeadEnd, 180f + settings.deadEndOffset);
            return (settings.roadDeadEnd, 270f + settings.deadEndOffset);
        }

        return (null, 0f);
    }

   
    private void BuildFloor()
    {
        if (!settings.buildFloor) return;

        if (_floorInstance)
        {
#if UNITY_EDITOR
            DestroyImmediate(_floorInstance);
#else
            Destroy(_floorInstance);
#endif
        }

        _floorInstance = GameObject.CreatePrimitive(PrimitiveType.Plane);
        _floorInstance.name = "CityFloor";

        Transform parent = settings.floorParent ? settings.floorParent : transform;
        _floorInstance.transform.SetParent(parent, false);

        _floorInstance.layer = settings.floorLayer;

        int padCells = Mathf.Max(0, settings.floorPaddingCells);
        float cell = settings.cellSize;

        float sizeX = (Grid.width + padCells * 2) * cell;
        float sizeZ = (Grid.height + padCells * 2) * cell;

        _floorInstance.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);

        float minX = settings.origin.x - padCells * cell;
        float minZ = settings.origin.z - padCells * cell;

        float centerX = minX + sizeX * 0.5f - cell * 0.5f;
        float centerZ = minZ + sizeZ * 0.5f - cell * 0.5f;

        _floorInstance.transform.position = new Vector3(centerX, settings.floorY, centerZ);

        if (settings.floorMaterial)
            _floorInstance.GetComponent<Renderer>().sharedMaterial = settings.floorMaterial;

        if (!settings.floorHasCollider)
        {
            var col = _floorInstance.GetComponent<Collider>();
            if (col)
            {
#if UNITY_EDITOR
                DestroyImmediate(col);
#else
                Destroy(col);
#endif
            }
        }
    }

    private void BuildBuildings()
    {
        Transform parent = settings.buildingsParent ? settings.buildingsParent : transform;
        ClearChildren(parent);

        bool hasSmall = settings.buildingPrefabs != null && settings.buildingPrefabs.Length > 0;
        bool hasBig = settings.bigBuildingPrefabs != null && settings.bigBuildingPrefabs.Length > 0;
        if (!hasSmall && !hasBig) return;

        var rng = new System.Random(LastSeed ^ 0x5bd1e995);

        bool[] occupied = new bool[Grid.width * Grid.height];

        for (int y = 0; y < Grid.height; y++)
            for (int x = 0; x < Grid.width; x++)
                if (Grid.IsRoad(x, y))
                    occupied[Grid.Idx(x, y)] = true;

        if (hasSmall)
            PlaceSmallRoadWall(rng, occupied, parent);

        if (hasBig && settings.bigBehindRoadWall)
            PlaceBigBehindRoadWall(rng, occupied, parent);

        if (hasBig && settings.bigPocketFillExtra)
            PlaceBigPocketFillExtra(rng, occupied, parent);
    }

    private void PlaceSmallRoadWall(System.Random rng, bool[] occupied, Transform parent)
    {
        float fill = Mathf.Clamp01(settings.buildingFill);

        for (int y = 0; y < Grid.height; y++)
            for (int x = 0; x < Grid.width; x++)
            {
                int id = Grid.Idx(x, y);
                if (occupied[id]) continue;

                if (!IsAdjacentToRoad4(x, y)) continue;

                if (!settings.roadsideWall)
                {
                    if ((float)rng.NextDouble() > fill) continue;
                }

                GameObject prefab = settings.buildingPrefabs[rng.Next(0, settings.buildingPrefabs.Length)];
                if (!prefab) continue;

                float rotY = RotationFacingNearestRoad(x, y);
                Vector3 pos = settings.origin + new Vector3(x * settings.cellSize, 0f, y * settings.cellSize);
                Instantiate(prefab, pos, Quaternion.Euler(0f, rotY, 0f), parent);

                occupied[id] = true;
            }
    }

    private void PlaceBigBehindRoadWall(System.Random rng, bool[] occupied, Transform parent)
    {
        int layers = Mathf.Max(1, settings.bigBehindLayers);
        float chance = Mathf.Clamp01(settings.bigBehindChance);

        int minD = 2;
        int maxD = 1 + layers;

        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < Grid.height; y++)
                for (int x = 0; x < Grid.width; x++)
                {
                    int id = Grid.Idx(x, y);
                    if (occupied[id]) continue;

                    if (IsAdjacentToRoad4(x, y)) continue;

                    int d = RoadDistanceManhattan(x, y, maxD);
                    if (d < minD || d > maxD) continue;

                    if ((float)rng.NextDouble() > chance) continue;

                    TryPlaceBigAt(x, y, rng, occupied, parent, preferLargest: true);
                }
        }
    }

    private void PlaceBigPocketFillExtra(System.Random rng, bool[] occupied, Transform parent)
    {
        float chance = Mathf.Clamp01(settings.bigPocketFillChance);

        int minD = 3;
        int maxD = 6;

        for (int y = 0; y < Grid.height; y++)
            for (int x = 0; x < Grid.width; x++)
            {
                int id = Grid.Idx(x, y);
                if (occupied[id]) continue;

                if (IsAdjacentToRoad4(x, y)) continue;

                int d = RoadDistanceManhattan(x, y, maxD);
                if (d < minD || d > maxD) continue;

                if (!IsNearOccupied(x, y, occupied, radius: 2)) continue;

                if ((float)rng.NextDouble() > chance) continue;

                TryPlaceBigAt(x, y, rng, occupied, parent, preferLargest: true);
            }
    }

    private bool TryPlaceBigAt(int x, int y, System.Random rng, bool[] occupied, Transform parent, bool preferLargest)
    {
        if (settings.bigBuildingPrefabs == null || settings.bigBuildingPrefabs.Length == 0) return false;

        var sizes = (settings.bigBuildingSizes != null && settings.bigBuildingSizes.Length > 0)
            ? settings.bigBuildingSizes
            : new Vector2Int[] { new Vector2Int(2, 2) };

        var sorted = new List<Vector2Int>(sizes);
        sorted.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y));

        int start = 0, end = sorted.Count, step = 1;
        if (!preferLargest)
        {
            start = sorted.Count - 1;
            end = -1;
            step = -1;
        }

        for (int i = start; i != end; i += step)
        {
            int w = Mathf.Max(1, sorted[i].x);
            int h = Mathf.Max(1, sorted[i].y);

            if (rng.NextDouble() < 0.5)
            {
                int tmp = w; w = h; h = tmp;
            }

            if (!CanPlaceRect(x, y, w, h, occupied)) continue;

            GameObject prefab = settings.bigBuildingPrefabs[rng.Next(0, settings.bigBuildingPrefabs.Length)];
            if (!prefab) continue;

            Vector3 basePos = settings.origin + new Vector3(x * settings.cellSize, 0f, y * settings.cellSize);
            Vector3 offset = new Vector3((w - 1) * settings.cellSize * 0.5f, 0f, (h - 1) * settings.cellSize * 0.5f);
            Vector3 pos = basePos + offset;

            float rotY = 90f * rng.Next(0, 4);
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, rotY, 0f), parent);

            if (settings.fitBigBuildingsToGrid)
            {
                FitBigInstanceToFootprint(go, w, h);
                go.transform.position = pos;
            }

            MarkRectOccupied(x, y, w, h, occupied);
            return true;
        }

        return false;
    }

    private void FitBigInstanceToFootprint(GameObject instance, int wCells, int hCells)
    {
        if (!instance) return;

        var rends = instance.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float desiredX = wCells * settings.cellSize;
        float desiredZ = hCells * settings.cellSize;

        float curX = Mathf.Max(0.0001f, b.size.x);
        float curZ = Mathf.Max(0.0001f, b.size.z);

        float sx = desiredX / curX;
        float sz = desiredZ / curZ;

        var t = instance.transform;
        Vector3 ls = t.localScale;
        t.localScale = new Vector3(ls.x * sx, ls.y, ls.z * sz);
    }

    private bool CanPlaceRect(int x, int y, int w, int h, bool[] occupied)
    {
        if (x < 0 || y < 0) return false;
        if (x + w > Grid.width) return false;
        if (y + h > Grid.height) return false;

        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                if (occupied[Grid.Idx(xx, yy)]) return false;
                if (Grid.IsRoad(xx, yy)) return false; 
            }
        return true;
    }

    private void MarkRectOccupied(int x, int y, int w, int h, bool[] occupied)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                occupied[Grid.Idx(xx, yy)] = true;
    }

    private bool IsAdjacentToRoad4(int x, int y)
    {
        if (Grid.InBounds(x, y + 1) && Grid.IsRoad(x, y + 1)) return true;
        if (Grid.InBounds(x + 1, y) && Grid.IsRoad(x + 1, y)) return true;
        if (Grid.InBounds(x, y - 1) && Grid.IsRoad(x, y - 1)) return true;
        if (Grid.InBounds(x - 1, y) && Grid.IsRoad(x - 1, y)) return true;
        return false;
    }

    private float RotationFacingNearestRoad(int x, int y)
    {
        if (Grid.InBounds(x, y - 1) && Grid.IsRoad(x, y - 1)) return 0f;     // north
        if (Grid.InBounds(x - 1, y) && Grid.IsRoad(x - 1, y)) return 90f;    // east
        if (Grid.InBounds(x, y + 1) && Grid.IsRoad(x, y + 1)) return 180f;   // south
        if (Grid.InBounds(x + 1, y) && Grid.IsRoad(x + 1, y)) return 270f;   // west

        return 90f * UnityEngine.Random.Range(0, 4);
    }

    private int RoadDistanceManhattan(int x, int y, int maxSearch)
    {
        if (Grid.IsRoad(x, y)) return 0;

        for (int d = 1; d <= maxSearch; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                int dy = d - Mathf.Abs(dx);

                int x1 = x + dx, y1 = y + dy;
                if (Grid.InBounds(x1, y1) && Grid.IsRoad(x1, y1)) return d;

                if (dy != 0)
                {
                    int x2 = x + dx, y2 = y - dy;
                    if (Grid.InBounds(x2, y2) && Grid.IsRoad(x2, y2)) return d;
                }
            }
        }
        return int.MaxValue;
    }

    private bool IsNearOccupied(int x, int y, bool[] occupied, int radius)
    {
        int r = Mathf.Max(1, radius);
        for (int oy = -r; oy <= r; oy++)
            for (int ox = -r; ox <= r; ox++)
            {
                int nx = x + ox;
                int ny = y + oy;
                if (!Grid.InBounds(nx, ny)) continue;
                if (occupied[Grid.Idx(nx, ny)]) return true;
            }
        return false;
    }

    public bool TryGetRandomRoadWorld(System.Random rng, out Vector3 world, float y = 0.5f, int maxTries = 200)
    {
        world = Vector3.zero;
        if (Grid == null) return false;

        for (int i = 0; i < maxTries; i++)
        {
            int x = rng.Next(0, Grid.width);
            int yCell = rng.Next(0, Grid.height);

            if (!Grid.IsRoad(x, yCell)) continue;

            world = CellToWorld(new Vector2Int(x, yCell), y);
            return true;
        }
        return false;
    }

    public bool TryGetRoadNearWorld(Vector3 nearWorld, out Vector3 roadWorld, float y = 0.5f, int searchRadiusCells = 12)
    {
        roadWorld = Vector3.zero;
        if (Grid == null) return false;

        int cx = Mathf.RoundToInt((nearWorld.x - settings.origin.x) / settings.cellSize);
        int cy = Mathf.RoundToInt((nearWorld.z - settings.origin.z) / settings.cellSize);

        for (int r = 0; r <= searchRadiusCells; r++)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx;
                    int yCell = cy + dy;
                    if (!Grid.InBounds(x, yCell)) continue;
                    if (!Grid.IsRoad(x, yCell)) continue;

                    roadWorld = CellToWorld(new Vector2Int(x, yCell), y);
                    return true;
                }
        }
        return false;
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        float cs = Mathf.Max(0.01f, settings.cellSize);
        Vector3 o = settings.origin;

        // Convert world x/z to cell coords
        int x = Mathf.RoundToInt((world.x - o.x) / cs);
        int y = Mathf.RoundToInt((world.z - o.z) / cs);

        return new Vector2Int(x, y);
    }

    public bool IsInDestinationCell(Vector3 world)
    {
        return WorldToCell(world) == DestCell;
    }

    private void ClearChildren(Transform t)
    {
        if (!t) return;

        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var child = t.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
