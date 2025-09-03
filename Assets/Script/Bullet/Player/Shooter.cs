using UnityEngine;

public class Shooter : MonoBehaviour
{
    public Transform muzzle;
    public float fireRate = 8f;       // î≠/ïb
    public float chargeTime = 1.2f;   // Ç±ÇÍí¥Ç¶ÇΩÇÁÉ`ÉÉÅ[ÉWíe
    private ObjectPool chargePool;
    private ObjectPool normalPool;

    [HideInInspector] public float normalDamageMul = 1f;
    [HideInInspector] public float chargePowerMul = 1f;

    bool autoFire;
    float cd;
    float hold;

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool Ç™ñ¢ê›íËÇ≈Ç∑");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool Ç™ñ¢ê›íËÇ≈Ç∑");
    }

    void Update()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) { hold = 0f; }
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            if (hold >= chargeTime) FireCharge(); else autoFire = !autoFire;
            hold = 0f;
        }
        if (shift) hold += Time.deltaTime;

        if (autoFire)
        {
            cd -= Time.deltaTime;
            if (cd <= 0f) { FireNormal(); cd = 1f / fireRate; }
        }
    }
    void FireNormal()
    {
        var go = normalPool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul);
    }

    void FireCharge()
    {
        var go = chargePool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul * chargePowerMul);
        go.transform.localScale *= chargePowerMul;   // ÉTÉCÉYÇ‡ägëÂ
    }
}
