using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Tile Movement")]
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float wallCheckRadius = 0.1f;

    [Header("Death Settings")]
    [SerializeField] private float respawnDelay = 2f;

    [Header("Mobile Input")]
    [SerializeField] private float swipeThreshold = 50f; // Píxeles mínimos para detectar swipe

    private Vector2 rawInput;
    private Vector2 lastRawInput;
    private Vector2 targetPosition;

    private bool isMoving;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 spawnPosition;

    public bool canMove = true;

    // Touch input
    private Vector2 touchStartPosition;
    private bool isTouching;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        targetPosition = rb.position;
    }

    private void Start()
    {
        spawnPosition = transform.position;
    }

    private void Update()
    {
        // Entrada de escritorio
        rawInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // Entrada de móvil (touch)
        Vector2 touchInput = GetTouchInput();
        if (touchInput != Vector2.zero)
        {
            rawInput = touchInput;
        }

        if (!canMove || isMoving)
        {
            lastRawInput = rawInput;
            return;
        }

        bool newPress = rawInput.sqrMagnitude > 0.01f && lastRawInput.sqrMagnitude <= 0.01f;
        if (!newPress)
        {
            lastRawInput = rawInput;
            return;
        }

        Vector2 direction = GetStepDirection(rawInput);
        if (direction == Vector2.zero)
        {
            lastRawInput = rawInput;
            return;
        }

        Vector2 nextPosition = rb.position + direction * tileSize;

        if (IsWallAtPosition(nextPosition))
        {
            lastRawInput = rawInput;
            return;
        }

        // Flip del sprite
        if (direction.x < 0)
            spriteRenderer.flipX = true;
        else if (direction.x > 0)
            spriteRenderer.flipX = false;

        targetPosition = nextPosition;
        isMoving = true;

        lastRawInput = rawInput;
    }

    private Vector2 GetTouchInput()
    {
        if (Input.touchCount <= 0)
        {
            isTouching = false;
            return Vector2.zero;
        }

        Touch touch = Input.GetTouch(0);

        // Detectar inicio del touch
        if (touch.phase == TouchPhase.Began)
        {
            touchStartPosition = touch.position;
            isTouching = true;
            return Vector2.zero;
        }

        // Si no estamos tocando, no hacer nada
        if (!isTouching)
            return Vector2.zero;

        // Detectar fin del touch
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isTouching = false;
            Vector2 swipeDelta = touch.position - touchStartPosition;

            // Solo considerar swipe si la distancia es mayor al threshold
            if (swipeDelta.magnitude < swipeThreshold)
                return Vector2.zero;

            // Determinar la dirección del swipe
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // Movimiento horizontal
                return swipeDelta.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                // Movimiento vertical
                return swipeDelta.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        return Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if ((targetPosition - newPosition).sqrMagnitude <= 0.0001f)
        {
            rb.MovePosition(targetPosition);
            isMoving = false;
        }
    }

    private Vector2 GetStepDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return Vector2.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return new Vector2(Mathf.Sign(input.x), 0f);

        return new Vector2(0f, Mathf.Sign(input.y));
    }

    private bool IsWallAtPosition(Vector2 position)
    {
        Vector2 size = GetColliderWorldSize();
        float angle = transform.eulerAngles.z;
        Vector2 center = position + GetColliderWorldOffset();

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].gameObject != gameObject && hits[i].CompareTag("Wall"))
                return true;
        }

        return false;
    }

    private Vector2 GetColliderWorldSize()
    {
        if (boxCollider == null)
            return new Vector2(wallCheckRadius * 2f, wallCheckRadius * 2f);

        Vector2 s = boxCollider.size;
        Vector3 scale = transform.lossyScale;

        return new Vector2(s.x * Mathf.Abs(scale.x), s.y * Mathf.Abs(scale.y));
    }

    private Vector2 GetColliderWorldOffset()
    {
        if (boxCollider == null)
            return Vector2.zero;

        Vector2 local = boxCollider.offset;
        Vector3 world = transform.TransformVector(new Vector3(local.x, local.y, 0f));

        return new Vector2(world.x, world.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            StartCoroutine(DeathRoutine());
        }
    }

    private IEnumerator DeathRoutine()
    {
        canMove = false;
        boxCollider.enabled = false;

        isMoving = false;

        if (animator != null)
            animator.SetTrigger("Death");

        yield return new WaitForSeconds(respawnDelay);

        rb.position = spawnPosition;
        targetPosition = spawnPosition;

        canMove = true;
        boxCollider.enabled = true;
    }
}