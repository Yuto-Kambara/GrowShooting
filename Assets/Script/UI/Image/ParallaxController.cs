using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [Tooltip("�E�֐i��ł���̂ŁA�w�i�͍��֗����i�P�ʁF���[���h�P��/�b�j")]
    public Vector2 baseScroll = new Vector2(-4f, 0f);

    [Tooltip("���i�قǏ������A�ߌi�قǑ傫���i��F0.1, 0.3, 0.6, 1.0�j")]
    public List<ParallaxLayer> layers = new();

    void Update()
    {
        var delta = (Vector3)(baseScroll * Time.deltaTime);
        for (int i = 0; i < layers.Count; i++)
            if (layers[i]) layers[i].Scroll(delta);
    }
}
