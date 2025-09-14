using UnityEngine;

public class BGMStarter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance?.PlayStageBgm();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
