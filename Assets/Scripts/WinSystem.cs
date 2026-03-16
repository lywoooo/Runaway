using UnityEngine;

public class DestinationWinSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CityBuilder city;
    [SerializeField] private Transform player;  

    [Header("Win Settings")]
    [SerializeField] private float winRadius = 10f; 

    private bool won;
    private GameOverManager endManager;

    private void Awake()
    {
        if (!city) city = FindFirstObjectByType<CityBuilder>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        endManager = FindFirstObjectByType<GameOverManager>();
    }

    private void Update()
    {
        if (won) return;
        if (!city || !city.ready) return;
        if (!player) return;

        Vector3 dest = city.DestWorld;
        dest.y = player.position.y;

        float dist = Vector3.Distance(player.position, dest);
        if (dist <= winRadius)
        {
            won = true;
            if (endManager) endManager.Win();
            else Debug.Log("WIN (no GameOverManager found)!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!city) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(city.DestWorld, winRadius);
    }
}
