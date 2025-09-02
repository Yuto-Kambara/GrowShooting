using UnityEngine;

public class InGameEscHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneLoader.LoadTitle();
    }
}
