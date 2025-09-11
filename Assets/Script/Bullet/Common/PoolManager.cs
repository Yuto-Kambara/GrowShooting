using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    public ObjectPool NormalBulletPool;
    public ObjectPool ChargeBulletPool;
    public ObjectPool enemyBulletPool;
    public ObjectPool BeamBulletPool;
    public ObjectPool HomingBulletPool;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}