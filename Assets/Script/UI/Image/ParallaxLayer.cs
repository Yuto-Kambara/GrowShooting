using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 2f)]
    public float depthMultiplier = 0.3f; // ������=���i, �傫��=�ߌi
    public bool infiniteX = true;
    public Camera cam;
    [Tooltip("��ʊO�Ŋ����߂��ۂ̗]��")]
    public float wrapBuffer = 0.5f;

    readonly List<Transform> tiles = new();
    float tileWidth;

    void OnEnable() { Prepare(); }
    void OnValidate() { Prepare(); }

    void Prepare()
    {
        if (!cam) cam = Camera.main;
        tiles.Clear();

        // �q����^�C�����W
        foreach (Transform c in transform) tiles.Add(c);

        if (tiles.Count == 0) return;

        // 1���ڂ��畝���v�Z�i�X�v���C�g�̎����j
        var sr = tiles[0].GetComponent<SpriteRenderer>();
        if (sr)
            tileWidth = sr.bounds.size.x;
        else
            tileWidth = tiles[0].lossyScale.x; // �t�H�[���o�b�N

        if (tileWidth <= 0.0001f) tileWidth = 10f;

        // ��ʕ����Œ�2�`3���ŃJ�o�[
        float need = cam ? cam.orthographicSize * cam.aspect * 2f : 20f;
        while (tiles.Count < 3 || tiles.Count * tileWidth < need + tileWidth)
        {
            var clone = Instantiate(tiles[tiles.Count - 1].gameObject, transform).transform;
            tiles.Add(clone);
        }

        // �����ɕ��ג����i�����E�j
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

        // ��ʒ[
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect - wrapBuffer;
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect + wrapBuffer;

        // ���E�[�̃^�C�����擾
        Transform leftMost = tiles[0], rightMost = tiles[0];
        foreach (var t in tiles)
        {
            if (t.position.x < leftMost.position.x) leftMost = t;
            if (t.position.x > rightMost.position.x) rightMost = t;
        }

        // ���֔��������ԉE�̌���
        if (leftMost.position.x + tileWidth * 0.5f < camLeft)
        {
            leftMost.position = new Vector3(rightMost.position.x + tileWidth, leftMost.position.y, leftMost.position.z);
            return; // 1�t���[����1��ŏ\��
        }
        // �E�֔��������ԍ��̑O�ցi�t���Ή��j
        if (rightMost.position.x - tileWidth * 0.5f > camRight)
        {
            rightMost.position = new Vector3(leftMost.position.x - tileWidth, rightMost.position.y, rightMost.position.z);
        }
    }
}
