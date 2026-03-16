using UnityEngine;
using System.Collections;

public class DestinationMarker : MonoBehaviour
{
    [SerializeField] private CityBuilder city;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private float height = 6f;     
    [SerializeField] private string markerLayerName = "Minimap"; 

    private GameObject instance;

    private void Awake()
    {
        if (!city) city = FindFirstObjectByType<CityBuilder>();
    }

    private void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        while (!city || !city.ready) yield return null;
        if (!markerPrefab) yield break;

        if (instance) Destroy(instance);

        Vector3 pos = city.DestWorld + Vector3.up * height;
        instance = Instantiate(markerPrefab, pos, Quaternion.identity);
        instance.name = "DestinationMarker";

        int layer = LayerMask.NameToLayer(markerLayerName);
        if (layer != -1)
            SetLayerRecursively(instance, layer);
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
