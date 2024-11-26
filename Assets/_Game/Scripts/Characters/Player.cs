using UnityEngine;

public class Player : Character
{
    [Header("Input Settings")]
    [SerializeField] private DynamicJoystick joystick;
    [SerializeField] private bool useAnalogInput = false;
    [SerializeField] private float deadzone = 0.1f;

    new private void Update()
    {
        base.Update();
        if (useAnalogInput)
        {
            movementInput = joystick.GetProcessedDirection();
        }
        else
        {
            HandleKeyboardInput();
        }
    }

    private void GameOver()
    {
        CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
        collider.enabled = false;
    }

    private void HandleKeyboardInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movementInput = new Vector2(horizontal, vertical);
    }

    public override void AddSwords(int amount)
    {
        base.AddSwords(amount);
        sfxPlayer.PlayOneShot(pickupSound);
    }

    protected override void Die() 
    {
        swordSystem.UpdateSwordCount(0);
        swordCount = 0;
        visualComponent.SetActive(false);
        gameSingleton.ShowGameOver();
        GameOver();
    }
}