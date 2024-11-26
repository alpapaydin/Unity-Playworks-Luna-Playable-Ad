using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private GameSingleton gameSingleton;

    [Header("Control Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float deadZone = 0.1f;
    [Range(0.1f, 3f)]
    [SerializeField] private float sensitivity = 1f;

    private Vector2 touchPosition;
    private Vector2 moveDirection;
    private float joystickRadius;
    private bool isDragging = false;
    private RectTransform deadZoneVisual;

    public Vector2 Direction => moveDirection;
    public bool IsActive { get; private set; }

    private void Start()
    {
        gameSingleton.OnGameOver += DisableCurrentStick;
        background.gameObject.SetActive(false);
        joystickRadius = background.sizeDelta.x / 2;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (gameSingleton.isGameOver) return;
        isDragging = true;
        IsActive = true;
        background.gameObject.SetActive(true);
        if (deadZoneVisual != null) deadZoneVisual.gameObject.SetActive(true);
        background.position = eventData.position;
        joystickHandle.anchoredPosition = Vector2.zero;
        touchPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 direction = eventData.position - touchPosition;
        float distance = direction.magnitude;

        // Apply deadzone
        if (distance < joystickRadius * deadZone)
        {
            joystickHandle.anchoredPosition = direction;
            moveDirection = Vector2.zero;
            return;
        }
        Vector2 normalizedDir = direction.normalized;
        float sensitivityMultiplier = 1f / (1f - deadZone);
        float processedDistance = ((distance / joystickRadius) - deadZone) * sensitivityMultiplier;
        processedDistance = Mathf.Clamp01(processedDistance);
        processedDistance *= sensitivity;
        if (distance > joystickRadius)
        {
            direction = normalizedDir * joystickRadius;
        }

        joystickHandle.anchoredPosition = direction;
        moveDirection = normalizedDir * processedDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DisableCurrentStick();
    }

    private void DisableCurrentStick()
    {
        isDragging = false;
        IsActive = false;
        moveDirection = Vector2.zero;
        background.gameObject.SetActive(false);
        if (deadZoneVisual != null) deadZoneVisual.gameObject.SetActive(false);
    }

    public Vector2 GetProcessedDirection()
    {
        float magnitude = moveDirection.magnitude;
        if (magnitude < deadZone)
            return Vector2.zero;

        Vector2 normalizedDir = moveDirection.normalized;
        float processedMagnitude = ((magnitude - deadZone) / (1 - deadZone)) * sensitivity;
        return normalizedDir * processedMagnitude;
    }
}