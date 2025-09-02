using UnityEngine;

public class Shooter : MonoBehaviour
{
    public Transform muzzle;
    public float fireRate = 8f;       // ”­/•b
    public float chargeTime = 1.2f;   // ‚±‚ê’´‚¦‚½‚çƒ`ƒƒ[ƒW’e
    private ObjectPool chargePool;
    private ObjectPool normalPool;

    bool autoFire;
    float cd;
    float hold;

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool ‚ª–¢Ý’è‚Å‚·");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool ‚ª–¢Ý’è‚Å‚·");
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
        if (!go) return;
        var b = go.GetComponent<Bullet>();
        go.layer = LayerMask.NameToLayer("PlayerBullet");
        b.dir = Vector2.right;
    }
    void FireCharge()
    {
        var go = chargePool.Spawn(muzzle.position, Quaternion.identity);
        if (!go) return;
        var b = go.GetComponent<Bullet>();
        go.layer = LayerMask.NameToLayer("PlayerBullet");
        b.dir = Vector2.right; b.damage *= 4; b.speed *= 0.8f;
    }
}
