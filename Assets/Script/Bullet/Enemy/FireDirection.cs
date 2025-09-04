using UnityEngine;

public enum FireDirMode { Fixed, AtPlayer }

public struct FireDirSpec
{
    public FireDirMode mode;
    public Vector2 fixedDir;     // mode==Fixed のときだけ使用（要 normalize 済み）

    public static FireDirSpec Fixed(Vector2 d)
    {
        if (d.sqrMagnitude < 1e-6f) d = Vector2.left; // 保険
        return new FireDirSpec { mode = FireDirMode.Fixed, fixedDir = d.normalized };
    }

    public static FireDirSpec AtPlayer()
    {
        return new FireDirSpec { mode = FireDirMode.AtPlayer, fixedDir = Vector2.zero };
    }
}
