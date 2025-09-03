using UnityEngine;

[RequireComponent(typeof(Health), typeof(PlayerStats))]
public class AutoRegen : MonoBehaviour
{
    Health hp;
    PlayerStats stats;

    void Awake()
    {
        hp = GetComponent<Health>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (hp.hp >= hp.maxHP) return;

        float delta = stats.regenRate * Time.deltaTime;
        hp.Heal(delta);
    }
}
