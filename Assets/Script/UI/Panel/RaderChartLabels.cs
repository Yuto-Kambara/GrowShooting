// Assets/Scripts/UI/RadarChartLabels.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RadarChartGraphic �̊e���_�ʒu�Ƀ��x��(Text)��z�u���A
/// GrowthSystem.selected �̍��ڂ������l�p�ň͂��ăn�C���C�g�\������B
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class RadarChartLabels : MonoBehaviour
{
    [Header("Refs")]
    public RadarChartGraphic chart;        // ����GO�ɂ���z��B���ݒ�Ȃ玩���擾
    public GrowthSystem growthSystem;      // �I�𒆔\�͂̃n�C���C�g�p�i�C�Ӂj

    [Header("Labels")]
    [Tooltip("�e���̃��x���iaxes �Ɠ����ɂ���B���ݒ�Ȃ� H/R/S/C/D �������j")]
    public string[] axisLabels;            // ��: H, R, S, C, D
    public Font font;
    [Range(8, 48)] public int fontSize = 16;
    public Color textColor = Color.white;

    [Header("Layout")]
    [Tooltip("���_����O���փI�t�Z�b�g�ipx�j")]
    public float labelOffset = 12f;
    [Tooltip("���x�����i�����ł�OK�j�B0�ȉ��Ȃ� ContentSizeFitter �ɔC����")]
    public Vector2 labelSize = new Vector2(24, 20);

    [Header("Highlight (selected stat)")]
    public Color highlightBorderColor = new Color(1f, 0.96f, 0.4f, 1f);
    [Range(1f, 6f)] public float highlightBorderWidth = 2f;
    public Vector2 highlightPadding = new Vector2(6, 4); // �e�L�X�g�̎��͂ɗ]��

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
        // �� �������C�A�E�g���m�肳����
        Canvas.ForceUpdateCanvases();

        PositionLabels();
        UpdateHighlightImmediate();
    }


    void LateUpdate()
    {
        // ���t���[���y���Ǐ]�iRect�T�C�Y���]�I�t�Z�b�g���ς�����ꍇ�ɑΉ��j
        PositionLabels();
        UpdateHighlightImmediate();
    }

    void BuildLabels()
    {
        int axes = chart ? chart.axes : axisLabels.Length;
        EnsureArrays(axes);

        // �����q�̑|���i�Đ����΍�j
        foreach (Transform c in transform) Destroy(c.gameObject);

        for (int i = 0; i < axes; i++)
        {
            var go = new GameObject($"AxisLabel_{i}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);

            // �w�i�i�g���j��`���q�I�u�W�F�N�g
            var bg = new GameObject("Border", typeof(RectTransform), typeof(RectBorderGraphic));
            bg.transform.SetParent(go.transform, false);
            var br = bg.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);

            var border = bg.GetComponent<RectBorderGraphic>();
            border.color = highlightBorderColor;
            border.borderWidth = highlightBorderWidth;
            border.enabled = false; // �����͔�\��

            // �e�L�X�g
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

            // �� �����T�C�Y���擾�iContentSizeFitter �Ɉˑ����Ȃ��j
            var txtRect = labelTexts[i].rectTransform;
            float pw = Mathf.Max(1f, LayoutUtility.GetPreferredWidth(txtRect));
            float ph = Mathf.Max(1f, LayoutUtility.GetPreferredHeight(txtRect));

            Vector2 content = (labelSize.x > 0f && labelSize.y > 0f)
                ? labelSize
                : new Vector2(pw, ph);

            // ���x�����g�̃T�C�Y�i�C�Ӂj
            r.sizeDelta = content;

            // �g�̋�`�i�e�L�X�g�����{�p�f�B���O�j
            var br = borders[i].GetComponent<RectTransform>();
            br.sizeDelta = content + highlightPadding * 2f;
            borders[i].SetVerticesDirty(); // �� ���b�V���X�V�𖾎�
        }
    }
    void UpdateHighlightImmediate()
    {
        int selectedIndex = -1;
        if (growthSystem)
        {
            // GrowthSystem.StatType �� GrowthRadarBinder.Axis �������Ȃ̂ł��̂܂ܑΉ�
            selectedIndex = (int)growthSystem.selected;
            if (selectedIndex < 0 || selectedIndex >= chart.axes) selectedIndex = -1;
        }

        for (int i = 0; i < borders.Length; i++)
            if (borders[i]) borders[i].enabled = (i == selectedIndex);
    }
}
