using UnityEngine;

public class TankMovement : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform hullTransform;

    [Header("Движение")]
    [SerializeField] private float speedKmh = 60f;
    [SerializeField] private float metersPerUnit = 7f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float minDestinationDistance = 1.5f;
    [SerializeField, Range(1f, 90f)] private float movementAlignmentAngle = 65f;

    private float moveSpeed;
    private Vector3 targetPosition;
    private Vector2 finalFacingDirection;
    private bool hasFinalFacingDirection;
    private bool isMoving;
    private bool hasFacingOrder;
    private Vector3 facingWorldPosition;
    private Animator animator;

    public float VisualAngle => GetVisualAngle();
    public bool IsMoving => isMoving;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (hullTransform == null)
            Debug.LogError($"[{name}] Не назначен Hull Transform в TankMovement.", this);
        RecalculateSpeed();
    }

    private void OnValidate() => RecalculateSpeed();

    private void RecalculateSpeed()
    {
        float metersPerSecond = speedKmh / 3.6f;
        moveSpeed = metersPerSecond / Mathf.Max(0.001f, metersPerUnit);
    }

    // Обычная команда движения — корпус после прибытия смотрит по направлению движения.
    public bool SetDestination(Vector3 worldPosition)
    {
        Vector2 direction = worldPosition - transform.position;
        return SetDestination(worldPosition, direction);
    }

    // Команда движения с отдельным направлением корпуса после прибытия.
    public bool SetDestination(Vector3 worldPosition, Vector2 desiredFacingDirection)
    {
        worldPosition.z = transform.position.z;
        if (Vector3.Distance(transform.position, worldPosition) < minDestinationDistance)
        {
            Stop();
            if (desiredFacingDirection.sqrMagnitude > 0.001f)
                SetFacingDirection(desiredFacingDirection);
            return false;
        }

        targetPosition = worldPosition;
        hasFinalFacingDirection = desiredFacingDirection.sqrMagnitude > 0.001f;
        if (hasFinalFacingDirection)
            finalFacingDirection = desiredFacingDirection.normalized;

        hasFacingOrder = false;
        SetMoving(true);
        return true;
    }

    // Повернуть корпус на месте, не перемещая танк.
    public void SetFacingDirection(Vector2 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.001f) return;
        Stop();
        hasFinalFacingDirection = false;
        finalFacingDirection = worldDirection.normalized;
        hasFacingOrder = true;
    }

    // Боевое поведение: корпус старается держать лоб направленным на цель.
    public void FaceTowards(Vector3 worldPosition)
    {
        Stop();
        hasFinalFacingDirection = false;
        hasFacingOrder = true;
        facingWorldPosition = worldPosition;
    }

    public void Stop()
    {
        SetMoving(false);
    }

    private void SetMoving(bool value)
    {
        if (isMoving == value) return;
        isMoving = value;
        if (animator != null) animator.SetBool("isMoving", isMoving);
    }

    public void RotateTowards(Vector3 worldPos)
    {
        if (hullTransform == null) return;
        Vector2 toTarget = (Vector2)(worldPos - transform.position);
        if (toTarget.sqrMagnitude < 0.001f) return;
        RotateHullTowards(toTarget, turnSpeed * Time.deltaTime);
    }

    public bool IsFacing(Vector3 worldPos, float toleranceAngle = 5f)
    {
        Vector2 toTarget = (Vector2)(worldPos - transform.position);
        if (toTarget.sqrMagnitude < 0.001f) return true;
        return Vector2.Angle(GetForward(), toTarget.normalized) <= toleranceAngle;
    }

    private void Update()
    {
        if (hullTransform == null) return;

        if (isMoving)
        {
            Vector2 toDestination = (Vector2)(targetPosition - transform.position);
            float distance = toDestination.magnitude;

            if (distance <= stopDistance)
            {
                SetMoving(false);
                if (hasFinalFacingDirection)
                {
                    hasFacingOrder = true;
                    facingWorldPosition = transform.position + (Vector3)finalFacingDirection;
                }
                else
                {
                    hasFacingOrder = false;
                }
                return;
            }

            Vector2 desiredDirection = toDestination.normalized;
            RotateHullTowards(desiredDirection, turnSpeed * Time.deltaTime);

            // Гусеницы не тянут танк боком: сначала корпус разворачивается, затем едет.
            float alignment = Vector2.Angle(GetForward(), desiredDirection);
            if (alignment <= movementAlignmentAngle)
            {
                float alignmentFactor = Mathf.Lerp(0.25f, 1f, 1f - alignment / movementAlignmentAngle);
                float step = Mathf.Min(moveSpeed * alignmentFactor * Time.deltaTime, distance);
                transform.position += (Vector3)(GetForward() * step);
            }
            return;
        }

        if (hasFacingOrder)
        {
            Vector2 direction = finalFacingDirection;
            if (facingWorldPosition != Vector3.zero)
                direction = (Vector2)(facingWorldPosition - transform.position);

            if (direction.sqrMagnitude > 0.001f)
            {
                RotateHullTowards(direction.normalized, turnSpeed * Time.deltaTime);
                if (Vector2.Angle(GetForward(), direction.normalized) <= 1f)
                    hasFacingOrder = false;
            }
        }
    }

    private void RotateHullTowards(Vector2 forwardDirection, float maxDelta)
    {
        if (forwardDirection.sqrMagnitude < 0.001f) return;
        float forwardAngle = Mathf.Atan2(forwardDirection.y, forwardDirection.x) * Mathf.Rad2Deg;
        // Исходный спрайт корпуса направлен в локальный -X.
        Quaternion target = Quaternion.Euler(0f, 0f, forwardAngle + 180f);
        hullTransform.rotation = Quaternion.RotateTowards(hullTransform.rotation, target, maxDelta);
    }

    private Vector2 GetForward()
    {
        return hullTransform != null ? (Vector2)(-hullTransform.right) : Vector2.left;
    }

    private float GetVisualAngle()
    {
        Vector2 forward = GetForward();
        return Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
    }

    private void OnDrawGizmosSelected()
    {
        if (minDestinationDistance > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, minDestinationDistance);
        }
        if (!Application.isPlaying || !isMoving) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, 0.2f);
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}