// Assets/Scripts/UI/RectBorderGraphic.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 自分の RectTransform の矩形に「枠線のみ」を描く簡易 Graphic。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]   // ★ 追加：CanvasRendererを必須化（重要）
public class RectBorderGraphic : Graphic
{
    [Range(1f, 10f)]
    public float borderWidth = 2f;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false; // ★ 枠はヒット不要
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = rectTransform.rect;

        float w = borderWidth;
        // 外側
        Vector2 oBL = new(rect.xMin, rect.yMin);
        Vector2 oTL = new(rect.xMin, rect.yMax);
        Vector2 oTR = new(rect.xMax, rect.yMax);
        Vector2 oBR = new(rect.xMax, rect.yMin);
        // 内側
        Vector2 iBL = oBL + new Vector2(w, w);
        Vector2 iTL = oTL + new Vector2(w, -w);
        Vector2 iTR = oTR + new Vector2(-w, -w);
        Vector2 iBR = oBR + new Vector2(-w, w);

        // 4辺（外枠-内枠のリング）
        AddQuad(vh, oBL, oTL, iTL, iBL, color); // 左
        AddQuad(vh, oTL, oTR, iTR, iTL, color); // 上
        AddQuad(vh, oTR, oBR, iBR, iTR, color); // 右
        AddQuad(vh, oBR, oBL, iBL, iBR, color); // 下
    }

    private void AddQuad(VertexHelper vh, Vector2 bl, Vector2 tl, Vector2 tr, Vector2 br, Color c)
    {
        int idx = vh.currentVertCount;
        vh.AddVert(new UIVertex { position = bl, color = c });
        vh.AddVert(new UIVertex { position = tl, color = c });
        vh.AddVert(new UIVertex { position = tr, color = c });
        vh.AddVert(new UIVertex { position = br, color = c });
        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 0, idx + 2, idx + 3);
    }
}
