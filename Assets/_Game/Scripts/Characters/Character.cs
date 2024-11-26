using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class Character : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] protected float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] protected GameObject visualComponent;
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] public GameSingleton gameSingleton;

    [Header("Character Sprites")]
    [SerializeField] protected List<SpriteRenderer> characterSprites = new List<SpriteRenderer>();
    [SerializeField] protected bool useVerticalFlip = false;

    [Header("Combat Settings")]
    [SerializeField] protected int defaultSwordCount = 2;
    [SerializeField] protected int hp = 1;
    [SerializeField] protected int maxHp = 3;
    [SerializeField] protected float invulnerabilityTime = 0.25f;
    [SerializeField] public SwordRotationSystem swordSystem;

    [Header("UI Settings")]
    [SerializeField] protected SpriteRenderer hpFillRenderer;
    [SerializeField] protected float hpBarAnimationSpeed = 2f;
    [SerializeField] protected Color hpFullColor = Color.green;
    [SerializeField] protected Color hpEmptyColor = Color.red;

    [Header("SFX Settings")]
    [SerializeField] protected AudioClip clashSound;
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected AudioClip pickupSound;

    [Header("Death Animation Settings")]
    [SerializeField] protected float deathAnimationDuration = 1f;
    [SerializeField] protected float deathShrinkSpeed = 5f;
    [SerializeField] protected float deathFlySpeed = 10f;
    [SerializeField] protected AnimationCurve deathScaleCurve;

    protected int swordCount = 0;
    protected readonly int IsMovingHash = Animator.StringToHash("isWalking");
    protected readonly int damagedHash = Animator.StringToHash("damaged");
    protected Vector2 movementInput;
    protected Vector2 lastNonZeroDirection;
    protected bool isDead;
    protected bool isFacingRight = true;
    protected bool isInvulnerable = false;
    protected float invulnerabilityTimer = 0f;
    protected float currentHpBarFill = 1f;
    protected Vector3 originalHpBarScale;
    protected Vector3 originalHpBarPosition;
    protected Coroutine hpBarUpdateCoroutine;
    protected AudioSource sfxPlayer;
    private bool _isMoving;
    protected bool isMoving
    {
        get => _isMoving;
        set
        {
            _isMoving = value;
            if (animator != null)
            {
                animator.SetBool(IsMovingHash, value);
                Vector2 animationDirection = value ? movementInput : lastNonZeroDirection;
            }
        }
    }

    protected virtual void Awake()
    {
        lastNonZeroDirection = Vector2.right;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
        hp = maxHp;
        sfxPlayer = gameObject.AddComponent<AudioSource>();
        if (hpFillRenderer != null)
        {
            originalHpBarScale = hpFillRenderer.transform.localScale;
            UpdateHPBarInstant();
        }
    }

    protected virtual void Start()
    {
        AddSwords(defaultSwordCount);
    }

    protected virtual void Update()
    {
        UpdateInvulnerability();
        if (isMoving) { UpdateSpriteDirection(); }
    }

    protected virtual void FixedUpdate()
    {
        Move();
    }
    protected virtual void Move()
    {
        if (movementInput.sqrMagnitude > 0)
        {
            lastNonZeroDirection = movementInput.normalized;
            Vector2 movement = movementInput.normalized * moveSpeed;
            rb.velocity = movement;
            isMoving = true;
        }
        else
        {
            rb.velocity = Vector2.zero;
            isMoving = false;
        }
    }

    protected virtual void TakeDamage()
    {
        if (isInvulnerable || isDead) return;

        int previousHp = hp;
        hp--;
        StartInvulnerability();
        animator.SetTrigger(damagedHash);
        if (hpFillRenderer != null)
        {
            UpdateHPBarAnimated();
        }

        if (hp <= 0)
        {
            Die();
        }
    }

    protected virtual void UpdateHPBarInstant()
    {
        if (hpFillRenderer != null)
        {
            float fillAmount = (float)hp / maxHp;
            UpdateHPBarScale(fillAmount);
            UpdateHPBarColor(fillAmount);
            currentHpBarFill = fillAmount;
        }
    }

    protected virtual void UpdateHPBarScale(float fillAmount)
    {
        if (hpFillRenderer != null)
        {
            Vector3 newScale = originalHpBarScale;
            newScale.x = originalHpBarScale.x * fillAmount;
            hpFillRenderer.transform.localScale = newScale;
        }
    }

    protected virtual void UpdateHPBarColor(float fillAmount)
    {
        if (hpFillRenderer != null)
        {
            Color lerpedColor = Color.Lerp(hpEmptyColor, hpFullColor, fillAmount);
            hpFillRenderer.color = lerpedColor;
        }
    }

    protected virtual IEnumerator AnimateHPBar()
    {
        float targetFill = (float)hp / maxHp;
        float startFill = currentHpBarFill;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * hpBarAnimationSpeed;
            currentHpBarFill = Mathf.Lerp(startFill, targetFill, elapsed);

            UpdateHPBarScale(currentHpBarFill);
            UpdateHPBarColor(currentHpBarFill);

            yield return null;
        }

        currentHpBarFill = targetFill;
        UpdateHPBarScale(targetFill);
        UpdateHPBarColor(targetFill);
    }

    protected virtual void UpdateHPBarAnimated()
    {
        if (hpBarUpdateCoroutine != null)
        {
            StopCoroutine(hpBarUpdateCoroutine);
        }
        hpBarUpdateCoroutine = StartCoroutine(AnimateHPBar());
    }


    protected virtual void UpdateInvulnerability()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }
    }

    protected virtual void StartInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityTime;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        swordSystem.UpdateSwordCount(0);
        swordCount = 0;
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.None;
        }
        StartCoroutine(DeathAnimation());
    }

    protected virtual IEnumerator DeathAnimation()
    {
        float elapsed = 0f;
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 originalScale = visualComponent.transform.localScale;
        if (rb != null)
        {
            rb.velocity = randomDirection * deathFlySpeed;
        }
        while (elapsed < deathAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / deathAnimationDuration;
            float scaleMultiplier = deathScaleCurve != null
                ? deathScaleCurve.Evaluate(progress)
                : 1f - progress;

            visualComponent.transform.localScale = originalScale * scaleMultiplier;

            yield return null;
        }
        gameObject.SetActive(false);
    }

    protected virtual void UpdateSpriteDirection()
    {
        Vector2 currentVelocity = rb.velocity;
        if (currentVelocity.sqrMagnitude < 0.01f) return;

        if (useVerticalFlip)
        {
            if (Mathf.Abs(currentVelocity.y) > Mathf.Abs(currentVelocity.x))
            {
                bool shouldFaceUp = currentVelocity.y > 0;
                if (shouldFaceUp != isFacingRight)
                {
                    FlipSprites();
                }
            }
        }
        else
        {
            bool shouldFaceRight = currentVelocity.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprites();
            }
        }
    }

    protected virtual void FlipSprites()
    {
        isFacingRight = !isFacingRight;
        foreach (var spriteRenderer in characterSprites)
        {
            if (spriteRenderer != null)
            {
                if (useVerticalFlip)
                {
                    float rotationY = isFacingRight ? 0 : 180;
                    spriteRenderer.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
                else
                {
                    float rotationY = isFacingRight ? 0 : 180;
                    spriteRenderer.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
                }
            }
        }
    }

    protected virtual void OnDisable()
    {
        if (hpBarUpdateCoroutine != null)
        {
            StopCoroutine(hpBarUpdateCoroutine);
            hpBarUpdateCoroutine = null;
        }
    }

    public virtual void AddSwords(int amount)
    {
        int newCount = swordCount + amount;
        SetSwordCount(newCount);
    }

    public virtual void RemoveSwords(int amount)
    {
        int newCount = Mathf.Max(0, swordCount - amount);
        SetSwordCount(newCount);
    }

    protected virtual void SetSwordCount(int newCount)
    {
        swordCount = Mathf.Max(0, newCount);
        if (swordSystem != null)
        {
            swordSystem.UpdateSwordCount(swordCount);
        }
    }

    public virtual bool HandleHit()
    {
        if (isInvulnerable || isDead) return false;

        if (swordCount > 0)
        {
            sfxPlayer.PlayOneShot(clashSound, 0.8f);
            RemoveSwords(1);
        }
        else
        {
            sfxPlayer.PlayOneShot(hitSound);
            TakeDamage();
        }
        return true;
    }

    public virtual int GetSwordCount()
    {
        return swordCount;
    }

    public virtual int GetHealth()
    {
        return hp;
    }

    public virtual bool IsInvulnerable()
    {
        return isInvulnerable;
    }
}