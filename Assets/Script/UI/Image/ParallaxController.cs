using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [Tooltip("右へ進んでいる体で、背景は左へ流す（単位：ワールド単位/秒）")]
    public Vector2 baseScroll = new Vector2(-4f, 0f);

    [Tooltip("遠景ほど小さく、近景ほど大きく（例：0.1, 0.3, 0.6, 1.0）")]
    public List<ParallaxLayer> layers = new();

    void Update()
    {
        var delta = (Vector3)(baseScroll * Time.deltaTime);
        for (int i = 0; i < layers.Count; i++)
            if (layers[i]) layers[i].Scroll(delta);
    }
}
