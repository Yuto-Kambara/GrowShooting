// Assets/Scripts/Background/ParallaxController.cs ‚È‚Ç
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    public Vector2 baseScroll = new(-4f, 0f);
    public List<ParallaxLayer> layers = new();

    bool paused;

    public void SetPaused(bool v) => paused = v;

    void Update()
    {
        if (paused) return;

        var delta = (Vector3)(baseScroll * Time.deltaTime);
        for (int i = 0; i < layers.Count; i++)
            if (layers[i]) layers[i].Scroll(delta);
    }
}
