using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class MazeDetectiveMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
    private Rigidbody2D body;
    private PuzzleInputHandler input;
    private Vector2 movement;
    public MazePuzzleController Owner { get; private set; }
    public Rigidbody2D Body => body;

    private void Awake()
    {
        // REQUIRED: runtime-created GameObjects do NOT inherit their parent's layer.
        int layer = LayerMask.NameToLayer(MazePuzzleController.LayerName);
        if (layer < 0) throw new InvalidOperationException("Create the MazePuzzle layer using Tools > Detective Maze > Build Prefab and Test Scene.");
        gameObject.layer = layer;
        ConfigureBody();
    }

    private void ConfigureBody()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
        body.simulated = true;
#if UNITY_6000_0_OR_NEWER
        body.linearDamping = 0f;
#else
        body.drag = 0f;
#endif
    }

    public void Initialize(PuzzleInputHandler handler, MazePuzzleController owner)
    {
        // Also explicit here, for the Editor-built preview before Awake runs.
        gameObject.layer = LayerMask.NameToLayer(MazePuzzleController.LayerName);
        input = handler;
        Owner = owner;
        ConfigureBody();
    }

    private void Update()
    {
        movement = Owner != null && Owner.IsOpen && input != null ? input.ReadMove() : Vector2.zero;
    }

    private void FixedUpdate()
    {
        // Dynamic 2D physics resolves wall contact and sliding. Never move Transform.
        SetVelocity(Owner != null && Owner.IsOpen ? movement * moveSpeed : Vector2.zero);
    }

    public void ResetTo(Vector2 worldPosition)
    {
        ConfigureBody();
        movement = Vector2.zero;
        SetVelocity(Vector2.zero);
        body.angularVelocity = 0f;
        body.position = worldPosition;
        // Teleport only when opening/resetting, never for ordinary movement.
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    public void StopMoving()
    {
        movement = Vector2.zero;
        if (body != null) SetVelocity(Vector2.zero);
    }

    private void SetVelocity(Vector2 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = velocity;
#else
        body.velocity = velocity;
#endif
    }

    private void OnDisable() => StopMoving();
}
