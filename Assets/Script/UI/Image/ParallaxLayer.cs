using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 2f)]
    public float depthMultiplier = 0.3f; // 小さい=遠景, 大きい=近景
    public bool infiniteX = true;
    public Camera cam;
    [Tooltip("画面外で巻き戻す際の余白")]
    public float wrapBuffer = 0.5f;

    readonly List<Transform> tiles = new();
    float tileWidth;

    void OnEnable() { Prepare(); }
    void OnValidate() { Prepare(); }

    void Prepare()
    {
        if (!cam) cam = Camera.main;
        tiles.Clear();

        // 子からタイル収集
        foreach (Transform c in transform) tiles.Add(c);

        if (tiles.Count == 0) return;

        // 1枚目から幅を計算（スプライトの実寸）
        var sr = tiles[0].GetComponent<SpriteRenderer>();
        if (sr)
            tileWidth = sr.bounds.size.x;
        else
            tileWidth = tiles[0].lossyScale.x; // フォールバック

        if (tileWidth <= 0.0001f) tileWidth = 10f;

        // 画面幅を最低2～3枚でカバー
        float need = cam ? cam.orthographicSize * cam.aspect * 2f : 20f;
        while (tiles.Count < 3 || tiles.Count * tileWidth < need + tileWidth)
        {
            var clone = Instantiate(tiles[tiles.Count - 1].gameObject, transform).transform;
            tiles.Add(clone);
        }

        // 横一列に並べ直す（左→右）
        for (int i = 0; i < tiles.Count; i++)
        {
            var pos = tiles[0].position;
            tiles[i].position = new Vector3(pos.x + i * tileWidth, pos.y, pos.z);
        }
    }

    public void Scroll(Vector3 worldDelta)
    {
        transform.position += worldDelta * depthMultiplier;
        if (infiniteX) WrapX();
    }

    void WrapX()
    {
        if (!cam || tiles.Count == 0) return;

        // 画面端
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect - wrapBuffer;
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect + wrapBuffer;

        // 左右端のタイルを取得
        Transform leftMost = tiles[0], rightMost = tiles[0];
        foreach (var t in tiles)
        {
            if (t.position.x < leftMost.position.x) leftMost = t;
            if (t.position.x > rightMost.position.x) rightMost = t;
        }

        // 左へ抜けたら一番右の後ろへ
        if (leftMost.position.x + tileWidth * 0.5f < camLeft)
        {
            leftMost.position = new Vector3(rightMost.position.x + tileWidth, leftMost.position.y, leftMost.position.z);
            return; // 1フレームに1回で十分
        }
        // 右へ抜けたら一番左の前へ（逆走対応）
        if (rightMost.position.x - tileWidth * 0.5f > camRight)
        {
            rightMost.position = new Vector3(leftMost.position.x - tileWidth, rightMost.position.y, rightMost.position.z);
        }
    }
}
