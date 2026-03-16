using UnityEngine;
using System.Collections;

public class PoliceManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CityBuilder cityBuilder;
    [SerializeField] private GameObject policePrefab;

    [Header("Runtime")]
    [SerializeField] private Transform player;
    [SerializeField] private int count = 3;

    [Header("Spawn (on roads)")]
    [Tooltip("How many cells away from the player we search for road spawns.")]
    [SerializeField] private int searchRadiusCells = 25;

    [Tooltip("Minimum distance between player and cop spawn (meters).")]
    [SerializeField] private float minSpawnDist = 25f;

    [Tooltip("Extra lift so the suspension doesn't spawn clipped.")]
    [SerializeField] private float spawnUp = 0.7f;

    [SerializeField] private bool debugLogs = false;

    public void SetPlayer(Transform t) => player = t;

    void Awake()
    {
        if (!cityBuilder) cityBuilder = FindFirstObjectByType<CityBuilder>();
    }

    void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        while (!cityBuilder || !cityBuilder.ready) yield return null;
        while (!player) yield return null;

        if (!policePrefab)
        {
            Debug.LogError("[PoliceManager] policePrefab not assigned.");
            yield break;
        }

        var rng = new System.Random(cityBuilder.LastSeed ^ 0x51a7f00d);

        for (int i = 0; i < count; i++)
        {
            Vector3 roadPos;
            if (!TryPickRoadSpawn(rng, out roadPos))
                roadPos = cityBuilder.SpawnWorld;

            Vector3 spawnPos = roadPos + Vector3.up * spawnUp;

            var cop = Instantiate(policePrefab, spawnPos, Quaternion.identity);

            // Find PoliceAI even if it's on a child
            var ai = cop.GetComponentInChildren<PoliceAI>(true);
            if (!ai)
            {
                Debug.LogError("[PoliceManager] Spawned police prefab missing PoliceAI (in children).");
                continue;
            }

            ai.SetTarget(player);

            // Optional: wake RB so it doesn't spawn "sleeping"
            var copRb = cop.GetComponentInChildren<Rigidbody>();
            if (copRb) copRb.WakeUp();

            if (debugLogs) Debug.Log($"[PoliceManager] Spawned cop {i} at {spawnPos}");
        }
    }

    bool TryPickRoadSpawn(System.Random rng, out Vector3 roadWorld)
    {
        roadWorld = Vector3.zero;

        // 1) Prefer: roads near the player
        if (cityBuilder.TryGetRoadNearWorld(player.position, out var near, y: 0.5f, searchRadiusCells: searchRadiusCells))
        {
            if (FlatDistance(near, player.position) >= minSpawnDist)
            {
                roadWorld = near;
                return true;
            }
        }

        // 2) Otherwise random road attempts until far enough
        for (int k = 0; k < 40; k++)
        {
            if (!cityBuilder.TryGetRandomRoadWorld(rng, out var any, y: 0.5f)) break;
            if (FlatDistance(any, player.position) >= minSpawnDist)
            {
                roadWorld = any;
                return true;
            }
        }

        return false;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

