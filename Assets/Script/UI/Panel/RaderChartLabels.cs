// Assets/Scripts/UI/RadarChartLabels.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RadarChartGraphic の各頂点位置にラベル(Text)を配置し、
/// GrowthSystem.selected の項目だけを四角で囲ってハイライト表示する。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RadarChartLabels : MonoBehaviour
{
    [Header("Refs")]
    public RadarChartGraphic chart;        // 同じGOにある想定。未設定なら自動取得
    public GrowthSystem growthSystem;      // 選択中能力のハイライト用（任意）

    [Header("Labels")]
    [Tooltip("各軸のラベル（axes と同数にする。未設定なら H/R/S/C/D を自動）")]
    public string[] axisLabels;            // 例: H, R, S, C, D
    public Font font;
    [Range(8, 48)] public int fontSize = 16;
    public Color textColor = Color.white;

    [Header("Layout")]
    [Tooltip("頂点から外側へオフセット（px）")]
    public float labelOffset = 12f;
    [Tooltip("ラベル幅（自動でもOK）。0以下なら ContentSizeFitter に任せる")]
    public Vector2 labelSize = new Vector2(24, 20);

    [Header("Highlight (selected stat)")]
    public Color highlightBorderColor = new Color(1f, 0.96f, 0.4f, 1f);
    [Range(1f, 6f)] public float highlightBorderWidth = 2f;
    public Vector2 highlightPadding = new Vector2(6, 4); // テキストの周囲に余白

    RectTransform rt;
    RectTransform[] labelRects;
    Text[] labelTexts;
    RectBorderGraphic[] borders;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (!chart) chart = GetComponent<RadarChartGraphic>();
        if (!growthSystem) growthSystem = FindAnyObjectByType<GrowthSystem>();

        if (axisLabels == null || axisLabels.Length == 0)
            axisLabels = new[] { "H", "R", "S", "C", "D" };

        BuildLabels();
        // ★ 初期レイアウトを確定させる
        Canvas.ForceUpdateCanvases();

        PositionLabels();
        UpdateHighlightImmediate();
    }


    void LateUpdate()
    {
        // 毎フレーム軽く追従（Rectサイズや回転オフセットが変わった場合に対応）
        PositionLabels();
        UpdateHighlightImmediate();
    }

    void BuildLabels()
    {
        int axes = chart ? chart.axes : axisLabels.Length;
        EnsureArrays(axes);

        // 既存子の掃除（再生成対策）
        foreach (Transform c in transform) Destroy(c.gameObject);

        for (int i = 0; i < axes; i++)
        {
            var go = new GameObject($"AxisLabel_{i}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);

            // 背景（枠線）を描く子オブジェクト
            var bg = new GameObject("Border", typeof(RectTransform), typeof(RectBorderGraphic));
            bg.transform.SetParent(go.transform, false);
            var br = bg.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);

            var border = bg.GetComponent<RectBorderGraphic>();
            border.color = highlightBorderColor;
            border.borderWidth = highlightBorderWidth;
            border.enabled = false; // 初期は非表示

            // テキスト
            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            txtGO.transform.SetParent(go.transform, false);
            var tr = txtGO.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);

            var fitter = txtGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var t = txtGO.GetComponent<Text>();
            t.text = (i < axisLabels.Length) ? axisLabels[i] : $"A{i + 1}";
            t.font = font ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = fontSize;
            t.color = textColor;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;

            labelRects[i] = r;
            labelTexts[i] = t;
            borders[i] = border;
        }
    }

    void EnsureArrays(int n)
    {
        if (labelRects == null || labelRects.Length != n)
        {
            labelRects = new RectTransform[n];
            labelTexts = new Text[n];
            borders = new RectBorderGraphic[n];
        }
    }

    void PositionLabels()
    {
        if (!chart || labelRects == null) return;

        var rect = rt.rect;
        float radius = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - chart.padding);
        float baseAng = Mathf.PI / 2f + chart.rotationOffsetDeg * Mathf.Deg2Rad;
        float dirSign = chart.clockwise ? -1f : 1f;

        for (int i = 0; i < chart.axes && i < labelRects.Length; i++)
        {
            float ang = baseAng + dirSign * (Mathf.PI * 2f) * (i / (float)chart.axes);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 vertex = dir * radius;
            Vector2 place = vertex + dir * labelOffset;

            var r = labelRects[i];
            r.anchoredPosition = place;

            // ★ 推奨サイズを取得（ContentSizeFitter に依存しない）
            var txtRect = labelTexts[i].rectTransform;
            float pw = Mathf.Max(1f, LayoutUtility.GetPreferredWidth(txtRect));
            float ph = Mathf.Max(1f, LayoutUtility.GetPreferredHeight(txtRect));

            Vector2 content = (labelSize.x > 0f && labelSize.y > 0f)
                ? labelSize
                : new Vector2(pw, ph);

            // ラベル自身のサイズ（任意）
            r.sizeDelta = content;

            // 枠の矩形（テキスト相当＋パディング）
            var br = borders[i].GetComponent<RectTransform>();
            br.sizeDelta = content + highlightPadding * 2f;
            borders[i].SetVerticesDirty(); // ★ メッシュ更新を明示
        }
    }
    void UpdateHighlightImmediate()
    {
        int selectedIndex = -1;
        if (growthSystem)
        {
            // GrowthSystem.StatType と GrowthRadarBinder.Axis が同順なのでそのまま対応
            selectedIndex = (int)growthSystem.selected;
            if (selectedIndex < 0 || selectedIndex >= chart.axes) selectedIndex = -1;
        }

        for (int i = 0; i < borders.Length; i++)
            if (borders[i]) borders[i].enabled = (i == selectedIndex);
    }
}
