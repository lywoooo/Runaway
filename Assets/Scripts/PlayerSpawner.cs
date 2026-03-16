using UnityEngine;
using System.Collections;
using Cinemachine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CityBuilder cityBuilder;
    [SerializeField] private PoliceManager policeManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private CinemachineVirtualCamera playerCam;

    [Header("Spawn")]
    [SerializeField] private float spawnHeightOffset = 0.6f;

    private GameObject playerInstance;

    void Awake()
    {
        if (!cityBuilder) cityBuilder = FindFirstObjectByType<CityBuilder>();
        if (!policeManager) policeManager = FindFirstObjectByType<PoliceManager>();
    }

    void Start()
    {
        StartCoroutine(SpawnWhenCityReady());
    }

    IEnumerator SpawnWhenCityReady()
    {
        if (!cityBuilder)
        {
            yield break;
        }
        if (!playerPrefab)
        {
            yield break;
        }

        while (!cityBuilder.ready)
            yield return null;

        if (playerInstance) Destroy(playerInstance);

        Vector3 pos = cityBuilder.SpawnWorld + Vector3.up * spawnHeightOffset;
        playerInstance = Instantiate(playerPrefab, pos, Quaternion.identity);

        Transform camTarget = playerInstance.transform.Find("CameraTarget");
        if (!camTarget) camTarget = playerInstance.transform;

        if (playerCam)
        {
            playerCam.Follow = camTarget;
            playerCam.LookAt = camTarget;
        }

        if (policeManager)
            policeManager.SetPlayer(playerInstance.transform);
    }
}
