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
    private float moveSpeed;
    private Vector3 targetPosition;
    private bool isMoving;
    private Animator animator;
    public float VisualAngle => GetVisualAngle();
    public bool IsMoving => isMoving;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (hullTransform == null) Debug.LogError($"[{name}] Не назначен Hull Transform в TankMovement.", this);
        RecalculateSpeed();
    }
    private void OnValidate() => RecalculateSpeed();
    private void RecalculateSpeed()
    {
        float metersPerSecond = speedKmh / 3.6f;
        moveSpeed = metersPerSecond / Mathf.Max(0.001f, metersPerUnit);
    }
    public bool SetDestination(Vector3 worldPosition)
    {
        worldPosition.z = transform.position.z;
        if (Vector3.Distance(transform.position, worldPosition) < minDestinationDistance) return false;
        targetPosition = worldPosition;
        SetMoving(true);
        return true;
    }
    public void Stop() => SetMoving(false);
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
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        RotateHullTowards(angle, turnSpeed * Time.deltaTime);
    }
    public bool IsFacing(Vector3 worldPos, float toleranceAngle = 5f)
    {
        Vector2 toTarget = (Vector2)(worldPos - transform.position);
        if (toTarget.sqrMagnitude < 0.001f) return true;
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(GetVisualAngle(), angle)) <= toleranceAngle;
    }
    private void Update()
    {
        if (!isMoving || hullTransform == null) return;
        Vector2 toTarget = (Vector2)(targetPosition - transform.position);
        float distance = toTarget.magnitude;
        if (distance <= stopDistance) { Stop(); return; }
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        RotateHullTowards(angle, turnSpeed * Time.deltaTime);
        Vector3 forward = -hullTransform.right; // корпус смотрит в локальный -X
        transform.position += forward * Mathf.Min(moveSpeed * Time.deltaTime, distance);
    }
    private void RotateHullTowards(float forwardAngle, float maxDelta)
    {
        Quaternion target = Quaternion.Euler(0f, 0f, forwardAngle + 180f);
        hullTransform.rotation = Quaternion.RotateTowards(hullTransform.rotation, target, maxDelta);
    }
    private float GetVisualAngle()
    {
        if (hullTransform == null) return transform.eulerAngles.z;
        Vector2 forward = -hullTransform.right;
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