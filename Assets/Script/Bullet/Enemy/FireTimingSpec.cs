using System.Collections.Generic;
using UnityEngine;

public enum FireTimingMode { Default, Interval, Timeline }

[System.Serializable]
public class FireTimingSpec
{
    public FireTimingMode mode = FireTimingMode.Default;

    // Interval 用
    public float startDelay = 0f;
    public float interval = 1f;

    // Timeline 用（出現からの相対秒）
    public List<float> times = new();

    public static FireTimingSpec Default() => new FireTimingSpec { mode = FireTimingMode.Default };

    public static FireTimingSpec Every(float interval, float delay = 0f)
        => new FireTimingSpec { mode = FireTimingMode.Interval, interval = Mathf.Max(0.01f, interval), startDelay = Mathf.Max(0f, delay) };

    public static FireTimingSpec Timeline(List<float> tlist)
        => new FireTimingSpec { mode = FireTimingMode.Timeline, times = tlist ?? new List<float>() };
}
