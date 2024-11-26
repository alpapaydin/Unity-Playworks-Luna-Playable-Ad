using UnityEngine;
using System.Linq;

public class NPC : Character
{
    private enum NPCState
    {
        SeekingSword,
        ChasingPlayer,
        Fleeing
    }

    [Header("NPC Settings")]
    [SerializeField] private float criticalHP = 1f;
    [SerializeField] private float fleeSpeed = 7f;
    [SerializeField] private float detectionRange = 10f;

    [Header("Drop Settings")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private int minDrops = 1;
    [SerializeField] private int maxDrops = 3;
    [SerializeField] private float dropSpreadRadius = 1f;
    [SerializeField] private float dropForce = 5f;


    private NPCState currentState;
    private Transform targetTransform;

    protected override void Awake()
    {
        base.Awake();
        if (gameSingleton == null)
        {
            gameSingleton = FindObjectOfType<GameSingleton>();
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateState();
        UpdateMovementInput();
    }

    private void UpdateState()
    {
        if (gameSingleton == null || gameSingleton.player == null) return;
        int playerSwordCount = gameSingleton.player.GetSwordCount();
        if (hp <= criticalHP && playerSwordCount > swordCount)
        {
            var closestSaferSword = FindClosestSaferSword();
            if (closestSaferSword != null)
            {
                currentState = NPCState.SeekingSword;
                targetTransform = closestSaferSword.transform;
            }
            else
            {
                currentState = NPCState.Fleeing;
                targetTransform = gameSingleton.player.transform;
            }
            return;
        }
        if (playerSwordCount > swordCount || swordCount == 0)
        {
            var closestSword = FindClosestSword();
            if (closestSword != null)
            {
                currentState = NPCState.SeekingSword;
                targetTransform = closestSword.transform;
            }
            else
            {
                currentState = NPCState.Fleeing;
                targetTransform = gameSingleton.player.transform;
            }
        }
        else if (swordCount >= playerSwordCount)
        {
            currentState = NPCState.ChasingPlayer;
            targetTransform = gameSingleton.player.transform;
        }
    }

    private void UpdateMovementInput()
    {
        if (targetTransform == null) return;
        Vector2 directionToTarget = (targetTransform.position - transform.position).normalized;
        switch (currentState)
        {
            case NPCState.Fleeing:
                movementInput = -directionToTarget;
                moveSpeed = fleeSpeed;
                break;
            case NPCState.SeekingSword:
            case NPCState.ChasingPlayer:
                movementInput = directionToTarget;
                moveSpeed = base.moveSpeed;
                break;
        }
    }

    private SwordPickup FindClosestSword()
    {
        var pickups = gameSingleton.GetPickups();
        if (pickups == null || pickups.Count == 0) return null;
        return pickups
            .Where(p => p != null && p.gameObject.activeSelf)
            .OrderBy(p => Vector2.Distance(transform.position, p.transform.position))
            .FirstOrDefault();
    }

    private SwordPickup FindClosestSaferSword()
    {
        if (gameSingleton.player == null) return null;

        var pickups = gameSingleton.GetPickups();
        if (pickups == null || pickups.Count == 0) return null;

        return pickups
            .Where(p => p != null && p.gameObject.activeSelf)
            .Where(p => Vector2.Distance(transform.position, p.transform.position) <
                       Vector2.Distance(gameSingleton.player.transform.position, p.transform.position))
            .OrderBy(p => Vector2.Distance(transform.position, p.transform.position))
            .FirstOrDefault();
    }

    protected override void Die()
    {
        if (!isDead)
        {
            SpawnDrops();
        }
        base.Die();
        gameSingleton.NpcKilled++;
    }
    private void SpawnDrops()
    {
        if (dropPrefab == null) return;
        int dropCount = Random.Range(minDrops, maxDrops + 1);
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * dropSpreadRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            GameObject drop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);
            Rigidbody2D dropRb = drop.GetComponent<Rigidbody2D>();
            if (dropRb != null)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                dropRb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
            }
        }
    }
}