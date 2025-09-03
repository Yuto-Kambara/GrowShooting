using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    public float maxHP = 5f;

    public UnityEvent onHit;
    public UnityEvent onDeath;
    public UnityEvent<float, float> onHpChanged = new(); // current, max

    [HideInInspector] public float hp;

    void Awake()
    {
        hp = maxHP;
        onHpChanged.Invoke(hp, maxHP);
    }

    public void Take(float dmg)
    {
        hp -= dmg;
        onHit?.Invoke();
        onHpChanged.Invoke(hp, maxHP);

        if (hp <= 0f)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(float v)
    {
        hp = Mathf.Min(maxHP, hp + v);
        onHpChanged.Invoke(hp, maxHP);
    }

    public void SetMax(float newMax, bool fill = false)
    {
        maxHP = newMax;
        if (fill) hp = maxHP;
        onHpChanged.Invoke(hp, maxHP);
    }
}
