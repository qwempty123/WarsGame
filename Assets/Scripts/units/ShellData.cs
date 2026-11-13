using UnityEngine;

public enum ShellType
{
    APFSDS,
    HEAT,
    HE
}

[CreateAssetMenu(fileName = "Shell_", menuName = "WarGame/Combat/Shell Data")]
public class ShellData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "APFSDS";
    public ShellType shellType = ShellType.APFSDS;

    [Header("Ballistics")]
    [Min(0f)] public float penetrationMM = 450f;
    [Min(0f)] public float damage = 45f;
    [Min(0.1f)] public float reloadSeconds = 3f;
    [Min(0.1f)] public float projectileSpeed = 18f;
    [Min(0.1f)] public float lifetime = 5f;

    [Header("Impact")]
    [Range(0f, 89f)] public float ricochetAngle = 72f;
    [Min(0f)] public float splashRadius = 0f;
    [Min(0f)] public float splashDamage = 0f;

    [Header("Penetration modifiers")]
    [Tooltip("Multiplier applied to penetration against side and rear armor.")]
    [Min(0f)] public float sideRearPenetrationMultiplier = 1f;
    [Tooltip("Multiplier applied to post-penetration damage.")]
    [Min(0f)] public float postPenetrationDamageMultiplier = 1f;
}