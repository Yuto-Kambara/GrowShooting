using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public int maxHP = 5;

    public UnityEvent onHit;
    public UnityEvent onDeath;
    public UnityEvent<int, int> onHpChanged = new(); // ★追加 (current, max)

    [HideInInspector] public int hp;

    void Awake()
    {
        hp = maxHP;
        onHpChanged.Invoke(hp, maxHP);              // 初期値を通知
    }

    public void Take(int dmg)
    {
        hp -= dmg;
        onHit?.Invoke();
        onHpChanged.Invoke(hp, maxHP);              // ★HP 更新通知
        if (hp <= 0)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(int v)
    {
        hp = Mathf.Min(maxHP, hp + v);
        onHpChanged.Invoke(hp, maxHP);              // ★HP 更新通知
    }
}
