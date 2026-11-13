using UnityEngine;

public struct ShellImpactResult
{
    public HitSide side;
    public float armorMM;
    public float effectiveArmorMM;
    public float penetrationMM;
    public float damage;
    public bool penetrated;
    public bool ricocheted;
}

public static class DamageCalculator
{
    public static ShellImpactResult Calculate(ShellData shell, Tank_T90 target, Vector2 incomingDirection, Vector3 hitPoint)
    {
        HitSide side = target.GetHitSide(hitPoint);
        float armor = target.GetArmorMM(side);
        float penetration = shell.penetrationMM;

        if (side != HitSide.Front)
            penetration *= shell.sideRearPenetrationMultiplier;

        Vector2 outward = ((Vector2)(hitPoint - target.transform.position)).normalized;
        float impactAngle = outward.sqrMagnitude < 0.001f
            ? 0f
            : Vector2.Angle(outward, -incomingDirection.normalized);

        bool ricochet = impactAngle >= shell.ricochetAngle;
        float cosine = Mathf.Max(0.15f, Mathf.Cos(impactAngle * Mathf.Deg2Rad));
        float effectiveArmor = armor / cosine;

        // HE is an area-effect shell. Its direct hit is intentionally less effective
        // against heavy armor; splash damage is resolved separately.
        float requiredPenetration = shell.shellType == ShellType.HE
            ? effectiveArmor * 1.5f
            : effectiveArmor;

        bool penetrated = !ricochet && penetration >= requiredPenetration;
        float dealtDamage = penetrated
            ? shell.damage * shell.postPenetrationDamageMultiplier
            : (shell.shellType == ShellType.HE ? shell.damage * 0.15f : 0f);

        return new ShellImpactResult
        {
            side = side,
            armorMM = armor,
            effectiveArmorMM = effectiveArmor,
            penetrationMM = penetration,
            damage = dealtDamage,
            penetrated = penetrated,
            ricocheted = ricochet
        };
    }
}