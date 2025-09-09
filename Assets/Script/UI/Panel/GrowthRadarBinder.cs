using UnityEngine;

[RequireComponent(typeof(RadarChartGraphic))]
public class GrowthRadarBinder : MonoBehaviour
{
    public enum Axis { MaxHP, Regen, Speed, ChargePower, NormalDamage }

    [Header("Refs")]
    public PlayerStats stats;
    public GrowthSystem growthSystem; // îCà”

    [Header("Smoothing")]
    [Range(0f, 20f)] public float smooth = 10f;

    private RadarChartGraphic chart;
    private readonly float[] current = new float[5];
    private readonly float[] target = new float[5];

    void Awake()
    {
        chart = GetComponent<RadarChartGraphic>();
        if (!stats) stats = FindAnyObjectByType<PlayerStats>();

        ForceUpdateValues();
        chart.SetValues(current);

        // ë¶éûîΩâfÇµÇΩÇ¢èÍçáÇÕ GrowthSystem ÇÃÉCÉxÉìÉgçwì«ÅiîCà”Åj
        if (growthSystem) growthSystem.StatsChanged += ForceUpdateValues;
    }

    void Update()
    {
        if (!stats) return;

        // åªç›íl / Cap Ç≈ 0..1
        target[0] = SafeDiv(stats.MaxHP, stats.MaxHP_Cap);
        target[1] = SafeDiv(stats.RegenPerSec, stats.RegenPerSec_Cap);
        target[2] = SafeDiv(stats.SpeedMul, stats.SpeedMul_Cap);
        target[3] = SafeDiv(stats.ChargePowerMul, stats.ChargePowerMul_Cap);
        target[4] = SafeDiv(stats.NormalDamageMul, stats.NormalDamageMul_Cap);

        if (smooth <= 0f)
        {
            System.Array.Copy(target, current, 5);
        }
        else
        {
            float k = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            for (int i = 0; i < 5; i++) current[i] = Mathf.Lerp(current[i], target[i], k);
        }
        chart.SetValues(current);
    }

    public void ForceUpdateValues()
    {
        if (!stats) return;
        current[0] = SafeDiv(stats.MaxHP, stats.MaxHP_Cap);
        current[1] = SafeDiv(stats.RegenPerSec, stats.RegenPerSec_Cap);
        current[2] = SafeDiv(stats.SpeedMul, stats.SpeedMul_Cap);
        current[3] = SafeDiv(stats.ChargePowerMul, stats.ChargePowerMul_Cap);
        current[4] = SafeDiv(stats.NormalDamageMul, stats.NormalDamageMul_Cap);
    }

    private float SafeDiv(float a, float b)
    {
        if (b <= 1e-6f) return 1f; // Capñ¢ê›íËéûÇÕñûÉ^ÉìàµÇ¢
        return Mathf.Clamp01(a / b);
    }
}
