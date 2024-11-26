using ScratchCardAsset;
using System;
using System.Collections.Generic;
using UnityEngine;
public class GameSingleton : MonoBehaviour
{
    [SerializeField] public int totalNpcCount = 3;
    [SerializeField] public Player player;
    [SerializeField] public ScratchCardManager scratchCardManager;
    [SerializeField] public GameObject playerCamera;
    [SerializeField] public UIManager uiManager;
    public MapGenerator mapGenerator;
    public event Action OnGameOver;
    public Animator cameraAnim;
    public float cameraIdleMoveSpeed = 2f;
    public bool isGameOver = false;

    private Vector2 targetPosition;
    private bool isMoving = false;
    private List<SwordPickup> pickups = new List<SwordPickup>();
    private int _npcKilled = 0;
    private int cameraShakeHash = Animator.StringToHash("shakeCamera");
    public int NpcKilled {
        get { return _npcKilled; }
        set {
            _npcKilled = value;
            if (_npcKilled >= totalNpcCount) ShowGameOver();
        }
    }

    public void Update()
    {
        if (!isGameOver) return;
        Vector3 currentPosition = playerCamera.transform.position;
        playerCamera.transform.position = Vector3.MoveTowards(
            currentPosition,
            new Vector3(targetPosition.x, targetPosition.y, currentPosition.z),
            cameraIdleMoveSpeed * Time.deltaTime
        );
        
        if (Vector2.Distance(playerCamera.transform.position, targetPosition) < 0.1f)
        {
            SetNewTargetPosition();
        }
    }
    
    private void SetNewTargetPosition()
    {
        targetPosition = mapGenerator.GetRandomPlayablePoint();
    }
    
    public void ShowGameOver()
    {
        if (isGameOver) return;
        cameraAnim.enabled = false;
        isGameOver = true;
        SetNewTargetPosition();
        uiManager.ShowEndCard();
        OnGameOver?.Invoke();
    }

    public void AddPickup(SwordPickup pickup)
    {
        if (pickup != null && !pickups.Contains(pickup))
        {
            pickups.Add(pickup);
        }
    }

    public void RemovePickup(SwordPickup pickup)
    {
        if (pickup != null && pickups.Contains(pickup))
        {
            pickups.Remove(pickup);
        }
    }

    public void ShakeCamera()
    {
        cameraAnim.SetTrigger(cameraShakeHash);
    }

    public List<SwordPickup> GetPickups()
    { return pickups; }
}