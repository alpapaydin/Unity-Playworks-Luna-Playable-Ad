using UnityEngine;
using System.Collections.Generic;

public class PickupSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSingleton gameSingleton;
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameObject pickupPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnDelay = 4f;
    [SerializeField] private float maxSpawnDelay = 10f;
    [SerializeField] private int minSpawnCount = 2;
    [SerializeField] private int maxSpawnCount = 4;
    [SerializeField] private int maxSpawnableAmount = 10;

    [Header("Cluster Settings")]
    [SerializeField] private float clusterRadius = 2f;
    [SerializeField] private float minDistanceBetweenPickups = 0.5f;

    private List<GameObject> activePickups = new List<GameObject>();
    private float nextSpawnTime;

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            activePickups.RemoveAll(pickup => pickup == null);
            if (activePickups.Count < maxSpawnableAmount)
            {
                SpawnPickupCluster();
            }
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
        nextSpawnTime = Time.time + randomDelay;
    }

    private void SpawnPickupCluster()
    {
        if (mapGenerator == null) return;
        int remainingSpawnable = maxSpawnableAmount - activePickups.Count;
        if (remainingSpawnable <= 0) return;
        int spawnCount = Mathf.Min(Random.Range(minSpawnCount, maxSpawnCount + 1), remainingSpawnable);
        Vector2Int centerPoint = mapGenerator.GetRandomPlayablePoint();
        List<Vector2> usedPositions = new List<Vector2>();
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 spawnPosition = GetValidClusterPosition(centerPoint, clusterRadius, minDistanceBetweenPickups, usedPositions);
            if (spawnPosition != Vector2.positiveInfinity)
            {
                GameObject pickup = SpawnPickup(spawnPosition);
                activePickups.Add(pickup);
                usedPositions.Add(spawnPosition);
            }
        }
    }

    private Vector2 GetValidClusterPosition(Vector2Int centerPoint, float radius, float minDistance, List<Vector2> usedPositions)
    {
        const int MAX_ATTEMPTS = 30;
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomRadius = Random.Range(0f, radius);
            Vector2 offset = new Vector2(Mathf.Cos(randomAngle) * randomRadius, Mathf.Sin(randomAngle) * randomRadius);
            Vector2 candidatePosition = (Vector2)centerPoint + offset;
            Vector2Int gridPosition = Vector2Int.RoundToInt(candidatePosition);
            if (!mapGenerator.IsPointInPlayableArea(gridPosition)) continue;
            bool tooClose = usedPositions.Exists(pos => Vector2.Distance(candidatePosition, pos) < minDistance);
            if (!tooClose)
            {
                return candidatePosition;
            }
        }
        return Vector2.positiveInfinity;
    }

    private GameObject SpawnPickup(Vector2 position)
    {
        GameObject pickup = Instantiate(pickupPrefab, position, Quaternion.identity);
        if (pickup.TryGetComponent<SwordPickup>(out var swordPickup))
        {
            swordPickup.animator.SetTrigger("init");
            swordPickup.gameSingleton = gameSingleton;
        }
        return pickup;
    }
}
