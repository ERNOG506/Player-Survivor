using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private SurvivorGameManager manager;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        manager = SurvivorGameManager.InstanceOrCreate();
        if (manager != null && manager.player == null)
        {
            manager.player = transform;
        }
    }

    private void Update()
    {
        if (manager != null && !manager.CanRunGame)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        if (movement.sqrMagnitude > 0.001f)
        {
            transform.localScale = new Vector3(movement.x < -0.05f ? -1f : 1f, 1f, 1f);
        }
    }

    private void FixedUpdate()
    {
        float currentSpeed = manager == null ? speed : manager.MoveSpeed;
        if (rb != null)
        {
            rb.linearVelocity = movement * currentSpeed;
        }

        if (manager != null)
        {
            transform.position = manager.ClampToArena(transform.position);
        }
    }
}
