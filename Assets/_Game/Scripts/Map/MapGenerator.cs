using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 20;
    [SerializeField] private Sprite basicSprite;

    [Header("Decorative Props")]
    [SerializeField] private Sprite[] propSprites;
    [Range(0f, 1f)]
    [SerializeField] private float propSpawnChance = 0.1f;
    
    [Header("Fence Settings")]
    [SerializeField] private Sprite topLeftFenceSprite;
    [SerializeField] private Sprite bottomLeftFenceSprite;
    [SerializeField] private Sprite verticalFenceConnectorSprite;
    [SerializeField] private Sprite horizontalFenceConnectorSprite;
    [SerializeField] private Sprite verticalFenceSprite;
    [SerializeField] private Sprite horizontalFenceSprite;
    [SerializeField] private int fenceWidth = 16;
    [SerializeField] private int fenceHeight = 12;
    [SerializeField] private int straightFences = 1;
    [SerializeField] private bool centerFence = true;

    [Header("Collision Settings")]
    [SerializeField] private Vector2 horizontalColliderSize = new Vector2(0.8f, 0.8f);
    [SerializeField] private Vector2 verticalColliderSize = new Vector2(0.3f, 0.8f);

    [Header("Sorting Settings")]
    [SerializeField] private string groundSortingLayer = "Ground";
    [SerializeField] private string fenceSortingLayer = "Fence";
    [SerializeField] private int groundOrderInLayer = 0;
    [SerializeField] private int fenceOrderInLayer = 1;

    [Header("References")]
    [SerializeField] private Transform groundParent;
    [SerializeField] private Transform fenceParent;

    [Header("Rendering")]
    [SerializeField] private Material spriteMaterial;
    private Material runtimeMaterial;

    private bool initialized = false;

    private void Awake()
    {
        if (spriteMaterial == null)
        {
            Debug.Log("Creating default sprite material");
            runtimeMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            runtimeMaterial = new Material(spriteMaterial);
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeRoutine());
    }

    private IEnumerator InitializeRoutine()
    {
        if (initialized) yield break;
        GenerateMap();
        initialized = true;
    }

    public void GenerateMap()
    {
        GenerateGround();
        GenerateFence();
    }

    private void GenerateGround()
    {
        if (basicSprite == null) return;
        int startX = -width / 2;
        int startY = -height / 2;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(startX + x, startY + y, 0);
                if (ShouldSpawnProp())
                {
                    PlacePropSprite(position);
                }
                else
                {
                    CreateGroundSprite(position, basicSprite);
                }
            }
        }
    }

    private void GenerateFence()
    {
        int startX = centerFence ? -fenceWidth / 2 : 0;
        int startY = centerFence ? -fenceHeight / 2 : 0;
        for (int x = 0; x <= fenceWidth; x++)
        {
            PlaceHorizontalFence(x, startX, startY);
        }
        for (int y = 1; y < fenceHeight; y++)
        {
            PlaceVerticalFence(y, startX, startY);
        }
    }

    private void PlaceHorizontalFence(int x, int startX, int startY)
    {
        Vector3 bottomPosition = new Vector3(startX + x, startY, 0);
        Vector3 topPosition = new Vector3(startX + x, startY + fenceHeight, 0);
        if (x == 0)
        {
            CreateFenceSprite(bottomPosition, bottomLeftFenceSprite, true, true);
            CreateFenceSprite(topPosition, topLeftFenceSprite, true, true);
        }
        else if (x == fenceWidth)
        {
            GameObject bottomFence = CreateFenceSprite(bottomPosition, bottomLeftFenceSprite, true, true);
            GameObject topFence = CreateFenceSprite(topPosition, topLeftFenceSprite, true, true);
            bottomFence.GetComponent<SpriteRenderer>().flipX = true;
            topFence.GetComponent<SpriteRenderer>().flipX = true;
        }
        else
        {
            int patternLength = straightFences + 1;
            int position = (x - 1) % patternLength;
            Sprite spriteToUse = position < straightFences ? horizontalFenceSprite : horizontalFenceConnectorSprite;
            CreateFenceSprite(bottomPosition, spriteToUse, true, true);
            CreateFenceSprite(topPosition, spriteToUse, true, true);
        }
    }

    private void PlaceVerticalFence(int y, int startX, int startY)
    {
        int patternLength = straightFences + 1;
        int position = (y - 1) % patternLength;
        Sprite spriteToUse = position < straightFences ? verticalFenceSprite : verticalFenceConnectorSprite;
        Vector3 leftPosition = new Vector3(startX, startY + y, 0);
        Vector3 rightPosition = new Vector3(startX + fenceWidth, startY + y, 0);
        CreateFenceSprite(leftPosition, spriteToUse, true, false);
        CreateFenceSprite(rightPosition, spriteToUse, true, false);
    }

    private GameObject CreateGroundSprite(Vector3 position, Sprite sprite)
    {
        if (sprite == null) return null;
        GameObject obj = new GameObject($"Ground_{position.x}_{position.y}");
        position.z = groundParent.transform.position.z;
        obj.transform.position = position;
        obj.transform.SetParent(groundParent);
        SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        return obj;
    }

    private GameObject CreateFenceSprite(Vector3 position, Sprite sprite, bool addCollider, bool isHorizontal)
    {
        if (sprite == null) return null;
        GameObject obj = new GameObject($"Fence_{position.x}_{position.y}");
        position.z = fenceParent.transform.position.z;
        obj.transform.position = position;
        obj.transform.SetParent(fenceParent);
        SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        if (addCollider)
        {
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.size = isHorizontal ? horizontalColliderSize : verticalColliderSize;
        }
        return obj;
    }

    private bool ShouldSpawnProp()
    {
        return propSprites != null && propSprites.Length > 0 && Random.value < propSpawnChance;
    }

    private void PlacePropSprite(Vector3 position)
    {
        if (propSprites == null || propSprites.Length == 0) return;

        int randomPropIndex = Random.Range(0, propSprites.Length);
        if (randomPropIndex < propSprites.Length && propSprites[randomPropIndex] != null)
        {
            CreateGroundSprite(position, propSprites[randomPropIndex]);
        }
    }

    public Vector2Int GetRandomPlayablePoint(int padding = 2)
    {
        int startX = centerFence ? -fenceWidth / 2 : 0;
        int startY = centerFence ? -fenceHeight / 2 : 0;
        return new Vector2Int(
            Random.Range(startX + padding, startX + fenceWidth - padding + 1),
            Random.Range(startY + padding, startY + fenceHeight - padding + 1)
        );
    }

    public bool IsPointInPlayableArea(Vector2Int point, int padding = 1)
    {
        int startX = centerFence ? -fenceWidth / 2 : 0;
        int startY = centerFence ? -fenceHeight / 2 : 0;
        return point.x >= startX + padding &&
               point.x <= startX + fenceWidth - padding &&
               point.y >= startY + padding &&
               point.y <= startY + fenceHeight - padding;
    }
}