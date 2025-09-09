using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 五角形レーダーチャート（uGUI）
/// RectTransform の大きさに合わせて描画。Values は 0..1 を想定
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RadarChartGraphic : Graphic
{
    [Header("Geometry")]
    [Range(3, 12)] public int axes = 5;
    [Range(0f, 40f)] public float padding = 6f;

    [Header("Lines")]
    [Range(0.5f, 6f)] public float gridLineWidth = 1.5f;
    [Range(0.5f, 6f)] public float borderLineWidth = 2f;
    [Range(0.5f, 3f)] public float axisLineWidth = 1.2f;

    [Header("Colors")]
    public Color fillColor = new Color(0.09f, 0.39f, 0.52f, 1f);
    public Color gridColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color borderColor = new Color(0.5f, 0.8f, 0.5f, 1f);
    public Color axisColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("Grid")]
    [Range(0, 5)] public int gridRings = 1;

    [Header("Orientation")]
    [Tooltip("追加回転（度）。0で頂点が真上。")]
    public float rotationOffsetDeg = 0f;      // ここを調整すると全体を回転できます
    [Tooltip("軸の進行方向。false=反時計回り、true=時計回り。")]
    public bool clockwise = false;

    // 0..1 の正規化値。配列長は axes と一致させる
    [SerializeField] private float[] values = new float[5];

    public void SetValues(float[] normalized)
    {
        if (normalized == null || normalized.Length == 0) return;
        if (normalized.Length != axes)
        {
            values = new float[axes];
            for (int i = 0; i < axes; i++)
                values[i] = i < normalized.Length ? normalized[i] : 0f;
        }
        else
        {
            if (values == null || values.Length != axes) values = new float[axes];
            System.Array.Copy(normalized, values, axes);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        var rect = rectTransform.rect;
        var center = Vector2.zero;                           // pivot が中央前提
        float radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - padding);

        // 方向ベクトル（★ 初期角度を「上」＝ +90° から開始に修正）
        float baseAng = Mathf.PI / 2f                         // 上向きスタート
                        + rotationOffsetDeg * Mathf.Deg2Rad;  // 追加回転
        float dirSign = clockwise ? -1f : 1f;                 // 時計回り/反時計回り
        Vector2[] dirs = new Vector2[axes];
        for (int i = 0; i < axes; i++)
        {
            float ang = baseAng + dirSign * (Mathf.PI * 2f) * (i / (float)axes);
            dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }

        // ===== 塗りポリゴン =====
        var fill = new Vector2[axes];
        for (int i = 0; i < axes; i++)
        {
            float t = values != null && i < values.Length ? values[i] : 0f;
            float r = radius * Mathf.Max(0f, t);
            fill[i] = center + dirs[i] * r;
        }
        AddFilledPolygon(vh, center, fill, fillColor);

        // ===== グリッド（同心正多角形） =====
        for (int g = 1; g <= gridRings; g++)
        {
            float rr = radius * (g / (float)gridRings);
            var ring = new Vector2[axes];
            for (int i = 0; i < axes; i++) ring[i] = center + dirs[i] * rr;
            AddPolygonLines(vh, ring, gridLineWidth, gridColor);
        }

        // ===== 外枠 =====
        var outer = new Vector2[axes];
        for (int i = 0; i < axes; i++) outer[i] = center + dirs[i] * radius;
        AddPolygonLines(vh, outer, borderLineWidth, borderColor);

        // ===== 軸線 =====
        for (int i = 0; i < axes; i++)
            AddLine(vh, center, center + dirs[i] * radius, axisLineWidth, axisColor);
    }

    // --- Mesh helpers ------------------------------------------------
    private void AddFilledPolygon(VertexHelper vh, Vector2 center, Vector2[] pts, Color col)
    {
        int startIndex = vh.currentVertCount;
        vh.AddVert(new UIVertex { position = center, color = col });
        for (int i = 0; i < pts.Length; i++)
            vh.AddVert(new UIVertex { position = pts[i], color = col });
        for (int i = 0; i < pts.Length; i++)
        {
            int a = startIndex;
            int b = startIndex + 1 + i;
            int c = startIndex + 1 + ((i + 1) % pts.Length);
            vh.AddTriangle(a, b, c);
        }
    }

    private void AddPolygonLines(VertexHelper vh, Vector2[] pts, float width, Color col)
    {
        for (int i = 0; i < pts.Length; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Length];
            AddLine(vh, a, b, width, col);
        }
    }

    private void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
    {
        Vector2 dir = (b - a);
        float len = dir.magnitude;
        if (len <= 0.001f) return;
        dir /= len;
        Vector2 n = new Vector2(-dir.y, dir.x);
        Vector2 o = n * (width * 0.5f);

        var v0 = a - o;
        var v1 = a + o;
        var v2 = b + o;
        var v3 = b - o;

        int idx = vh.currentVertCount;
        vh.AddVert(new UIVertex { position = v0, color = col });
        vh.AddVert(new UIVertex { position = v1, color = col });
        vh.AddVert(new UIVertex { position = v2, color = col });
        vh.AddVert(new UIVertex { position = v3, color = col });
        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 0, idx + 2, idx + 3);
    }
}
