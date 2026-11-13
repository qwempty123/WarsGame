using UnityEngine;

public class TankMovement : MonoBehaviour
{
    [Header("Движение (в реальных единицах)")]
    [SerializeField] private float speedKmh = 60f;
    [SerializeField] private float metersPerUnit = 7f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float minDestinationDistance = 1.5f;
    [SerializeField] private bool spriteFacesRight = true;

    private float moveSpeed;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Animator animator;

    // Публичное свойство — куда смотрит нос танка (для расчёта попаданий)
    public float VisualAngle => GetVisualAngle();

    void Awake()
    {
        animator = GetComponent<Animator>();
        RecalculateSpeed();
    }

    void OnValidate() => RecalculateSpeed();

    private void RecalculateSpeed()
    {
        float metersPerSecond = speedKmh / 3.6f;
        moveSpeed = metersPerSecond / Mathf.Max(0.001f, metersPerUnit);
    }

    public bool SetDestination(Vector3 worldPosition)
    {
        worldPosition.z = 0f;
        float dist = Vector3.Distance(transform.position, worldPosition);
        if (dist < minDestinationDistance) return false;

        targetPosition = worldPosition;
        if (!isMoving)
        {
            isMoving = true;
            if (animator != null) animator.SetBool("isMoving", true);
        }
        return true;
    }

    public void Stop()
    {
        if (!isMoving) return;
        isMoving = false;
        if (animator != null) animator.SetBool("isMoving", false);
    }

    public void RotateTowards(Vector3 worldPos)
    {
        Vector3 toTarget = worldPos - transform.position;
        if (toTarget.sqrMagnitude < 0.001f) return;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float visualAngle = GetVisualAngle();
        float newVisualAngle = Mathf.MoveTowardsAngle(visualAngle, targetAngle, turnSpeed * Time.deltaTime);
        ApplyVisualAngle(newVisualAngle);
    }

    public bool IsFacing(Vector3 worldPos, float toleranceAngle = 5f)
    {
        Vector3 toTarget = worldPos - transform.position;
        if (toTarget.sqrMagnitude < 0.001f) return true;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float visualAngle = GetVisualAngle();
        float delta = Mathf.Abs(Mathf.DeltaAngle(visualAngle, targetAngle));
        return delta <= toleranceAngle;
    }

    public bool IsMoving => isMoving;

    void Update()
    {
        if (!isMoving) return;

        Vector3 current = transform.position;
        Vector3 toTarget = targetPosition - current;
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
        {
            Stop();
            return;
        }

        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float visualAngle = GetVisualAngle();
        float newVisualAngle = Mathf.MoveTowardsAngle(visualAngle, targetAngle, turnSpeed * Time.deltaTime);
        ApplyVisualAngle(newVisualAngle);

        float rad = newVisualAngle * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        float step = moveSpeed * Time.deltaTime;
        if (step > distance) step = distance;
        transform.position += forward * step;
    }

    private float SpriteOffset => spriteFacesRight ? 0f : 180f;

    private float GetVisualAngle()
    {
        float rotation = transform.eulerAngles.z;
        bool flipped = transform.localScale.x < 0f;
        float visual = rotation + SpriteOffset + (flipped ? 180f : 0f);
        return Mathf.DeltaAngle(0f, visual);
    }

    private void ApplyVisualAngle(float visualAngle)
    {
        float normalized = Mathf.DeltaAngle(0f, visualAngle);
        float relAngle = Mathf.DeltaAngle(0f, normalized - SpriteOffset);

        bool needFlip = Mathf.Abs(relAngle) > 90f;

        Vector3 scale = transform.localScale;
        float absX = Mathf.Abs(scale.x);
        scale.x = needFlip ? -absX : absX;
        transform.localScale = scale;

        float rotation = needFlip ? relAngle + 180f : relAngle;
        transform.rotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void OnDrawGizmosSelected()
    {
        if (minDestinationDistance > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, minDestinationDistance);
            Gizmos.color = new Color(1f, 0f, 0f, 0.05f);
            Gizmos.DrawSphere(transform.position, minDestinationDistance);
        }
        if (!Application.isPlaying || !isMoving) return;

        Gizmos.color = Color.blue;
        Vector3 simPos = transform.position;
        float simVisualAngle = GetVisualAngle();
        float simStepTime = 0.05f;
        int maxSteps = 500;
        Vector3 prevPos = simPos;

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 toTarget = targetPosition - simPos;
            float dist = toTarget.magnitude;
            if (dist <= stopDistance) break;

            float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float newVisualAngle = Mathf.MoveTowardsAngle(simVisualAngle, targetAngle, turnSpeed * simStepTime);

            float rad = newVisualAngle * Mathf.Deg2Rad;
            Vector3 forward = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

            float step = moveSpeed * simStepTime;
            if (step > dist) step = dist;

            simPos += forward * step;
            simVisualAngle = newVisualAngle;

            Gizmos.DrawLine(prevPos, simPos);
            if (i % 10 == 0) Gizmos.DrawWireSphere(simPos, 0.05f);
            prevPos = simPos;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, 0.2f);
        float crossSize = 0.3f;
        Gizmos.DrawLine(targetPosition + Vector3.up * crossSize, targetPosition + Vector3.down * crossSize);
        Gizmos.DrawLine(targetPosition + Vector3.left * crossSize, targetPosition + Vector3.right * crossSize);
    }
}