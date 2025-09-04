using UnityEngine;

public enum FireDirMode { Fixed, AtPlayer }

public struct FireDirSpec
{
    public FireDirMode mode;
    public Vector2 fixedDir;     // mode==Fixed �̂Ƃ������g�p�i�v normalize �ς݁j

    public static FireDirSpec Fixed(Vector2 d)
    {
        if (d.sqrMagnitude < 1e-6f) d = Vector2.left; // �ی�
        return new FireDirSpec { mode = FireDirMode.Fixed, fixedDir = d.normalized };
    }

    public static FireDirSpec AtPlayer()
    {
        return new FireDirSpec { mode = FireDirMode.AtPlayer, fixedDir = Vector2.zero };
    }
}
