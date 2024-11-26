using ScratchCardAsset;
using System.Linq;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [System.Serializable]
    public class RotationSettings
    {
        public Vector2 pivotOffset = new Vector2(0f, 0.5f);
        public float rotationOffset = 90f;
        public bool reverseRotation = false;
    }

    [Header("Rotation Settings")]
    [SerializeField] private RotationSettings settings = new RotationSettings();
    [SerializeField] private Vector3 swordScratchOffset = new Vector3(1,1,0);

    [Header("Destroy Animation")]
    [SerializeField] private float destroyAnimationDuration = 0.5f;
    [SerializeField] private float flyAwaySpeed = 10f;
    [SerializeField] private float spinSpeed = 720f; // degrees per second

    [Header("VFX")]
    [SerializeField] private ParticleSystem clashParticle;

    [Header("SFX")]
    [SerializeField] private AudioClip clashSound;

    private bool isInitialized = false;
    private bool isDestroying = false;
    private float destroyTimer = 0f;
    private Vector2 flyAwayDirection;
    private float initialScale;
    private CapsuleCollider2D swordCollider;
    private SpriteRenderer spriteRenderer;
    private AudioSource sfxPlayer;

    public GameSingleton gameSingleton;
    [HideInInspector] public float rotationRadius = 1f;
    [HideInInspector] public float rotationSpeed = 180f;
    [HideInInspector] public float currentAngle = 0f;
    [HideInInspector] public Character owner;

    private void Awake()
    {
        sfxPlayer = gameObject.AddComponent<AudioSource>();
        swordCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale.x;
    }
    private void Update()
    {
        if (isDestroying)
        {
            UpdateDestroyAnimation();
        }
    }

    private void UpdateDestroyAnimation()
    {
        destroyTimer += Time.deltaTime;
        float progress = destroyTimer / destroyAnimationDuration;
        transform.position += (Vector3)flyAwayDirection * flyAwaySpeed * Time.deltaTime;
        transform.Rotate(0, 0, spinSpeed * Time.deltaTime);
        float scaleMultiplier = Mathf.Lerp(1f, 0f, progress);
        transform.localScale = Vector3.one * initialScale * scaleMultiplier;
        Color currentColor = spriteRenderer.color;
        currentColor.a = Mathf.Lerp(1f, 0f, progress);
        spriteRenderer.color = currentColor;
        if (destroyTimer >= destroyAnimationDuration)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null || isDestroying) return;
        Sword otherSword = other.GetComponent<Sword>();
        if (otherSword != null && otherSword.owner != owner && !otherSword.isDestroying)
        {
            sfxPlayer.PlayOneShot(clashSound, 0.8f);
            Vector3 clashPosition = (transform.position + otherSword.transform.position) * 0.5f;
            SpawnClashParticles(clashPosition);
            gameSingleton.ShakeCamera();
            StartDestroyAnimation();
            otherSword.StartDestroyAnimation();
            owner.swordSystem.RemoveSpecificSword(this);
            otherSword.owner.swordSystem.RemoveSpecificSword(otherSword);
            return;
        }
        Character otherCharacter = other.GetComponent<Character>();
        if (otherCharacter != null && otherCharacter != owner)
        {
            if (otherCharacter.HandleHit())
            {
                SpawnClashParticles(transform.position);
                StartDestroyAnimation();
                owner.swordSystem.RemoveSpecificSword(this);
            }
        }
    }

    public void StartDestroyAnimation()
    {
        if (isDestroying) return;
        isDestroying = true;
        destroyTimer = 0f;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        flyAwayDirection = new Vector2(
            Mathf.Cos(randomAngle),
            Mathf.Sin(randomAngle)
        );
        swordCollider.enabled = false;
        transform.SetParent(null);
    }

    public void Initialize(float radius, float speed, float startAngle)
    {
        rotationRadius = radius;
        rotationSpeed = speed;
        currentAngle = startAngle;
        isInitialized = true;
        UpdatePosition(transform.parent.position);
    }

    private void SpawnClashParticles(Vector3 position)
    {
        if (clashParticle != null)
        {
            GameObject particlesObj = Instantiate(clashParticle.gameObject, position, Quaternion.identity);
            ParticleSystem particles = particlesObj.GetComponent<ParticleSystem>();
            particles.Play();
            float totalDuration = particles.main.duration + particles.main.startLifetime.constantMax;
            Destroy(particles.gameObject, totalDuration);
        }
    }

    public void UpdatePosition(Vector3 centerPosition)
    {
        float orbitAngle = settings.reverseRotation ? -currentAngle : currentAngle;
        float x = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * rotationRadius;
        float y = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * rotationRadius;
        float finalRotation = orbitAngle + settings.rotationOffset;
        float pivotX = settings.pivotOffset.x * Mathf.Cos(finalRotation * Mathf.Deg2Rad) -
                      settings.pivotOffset.y * Mathf.Sin(finalRotation * Mathf.Deg2Rad);
        float pivotY = settings.pivotOffset.x * Mathf.Sin(finalRotation * Mathf.Deg2Rad) +
                      settings.pivotOffset.y * Mathf.Cos(finalRotation * Mathf.Deg2Rad);
        transform.position = centerPosition + new Vector3(x - pivotX, y - pivotY, 0);
        transform.rotation = Quaternion.Euler(0, 0, finalRotation);
        ScratchCardAtPos(finalRotation);
    }

    private void ScratchCardAtPos(float finalRotation)
    {
        if (!isInitialized) return;
        ScratchCardManager cardManager = gameSingleton.scratchCardManager;
        if (cardManager == null || cardManager.Card == null) return;
        Vector2 screenPos = cardManager.MainCamera.WorldToScreenPoint(transform.position + swordScratchOffset);
        Vector2 texturePosition = cardManager.Card.ScratchData.GetScratchPosition(screenPos);
        cardManager.Card.ScratchHole(texturePosition);
    }
}