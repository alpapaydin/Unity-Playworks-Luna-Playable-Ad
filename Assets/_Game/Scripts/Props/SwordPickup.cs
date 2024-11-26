using UnityEngine;
using System.Collections;

public class SwordPickup : MonoBehaviour
{
    [SerializeField] public GameSingleton gameSingleton;
    [SerializeField] private float initialRepelForce = 3f;
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float minDistanceToCollect = 0.1f;
    [SerializeField] private float repelDuration = 0.2f;
    [SerializeField] private float attractDelay = 0.1f;
    [SerializeField] public Animator animator;

    private bool isAnimating = false;
    private Transform targetCharacter;
    private CircleCollider2D itemCollider;
    private Vector2 repelDirection;

    private void Start()
    {
        if (gameSingleton != null)
        {
            gameSingleton.AddPickup(this);
        }
        itemCollider = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        if (isAnimating && targetCharacter != null)
        {
            float distance = Vector2.Distance(transform.position, targetCharacter.position);
            if (distance <= minDistanceToCollect)
            {
                CollectSword(targetCharacter.GetComponent<Character>());
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform == other.transform.root && other.GetComponent<Character>() != null)
        {
            if (!isAnimating)
            {
                targetCharacter = other.transform;
                repelDirection = (transform.position - targetCharacter.position).normalized;
                StartCoroutine(AnimatePickup());
            }
        }
        else if (other.GetComponent<Character>() != null && other.IsTouching(itemCollider))
        {
            CollectSword(other.GetComponent<Character>());
        }
    }

    private IEnumerator AnimatePickup()
    {
        isAnimating = true;
        float elapsedTime = 0f;
        Vector3 startPos = transform.position;
        while (elapsedTime < repelDuration)
        {
            elapsedTime += Time.deltaTime;
            float repelProgress = elapsedTime / repelDuration;
            float easedProgress = Mathf.Sin(repelProgress * Mathf.PI * 0.5f);
            Vector3 repelOffset = (Vector3)repelDirection * initialRepelForce * (1 - easedProgress);
            transform.position = startPos + repelOffset;
            yield return null;
        }
        yield return new WaitForSeconds(attractDelay);
        while (targetCharacter != null)
        {
            float distance = Vector2.Distance(transform.position, targetCharacter.position);
            float speedMultiplier = 1f + (distance / 2f); // Speed increases with distance
            float currentSpeed = Mathf.Min(baseSpeed * speedMultiplier, maxSpeed);
            Vector2 direction = (targetCharacter.position - transform.position).normalized;
            transform.position += (Vector3)direction * currentSpeed * Time.deltaTime;
            yield return null;
        }
    }

    private void CollectSword(Character character)
    {
        if (character != null)
        {
            character.AddSwords(1);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (gameSingleton != null)
        {
            gameSingleton.RemovePickup(this);
        }
    }
}