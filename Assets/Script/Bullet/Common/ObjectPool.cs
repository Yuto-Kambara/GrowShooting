using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int size = 64;
    GameObject[] pool; int idx;

    void Awake()
    {
        pool = new GameObject[size];
        for (int i = 0; i < size; i++) { pool[i] = Instantiate(prefab, transform); pool[i].SetActive(false); }
    }
    public GameObject Spawn(Vector3 pos, Quaternion rot)
    {
        for (int i = 0; i < size; i++)
        {
            idx = (idx + 1) % size;
            if (!pool[idx].activeSelf) { var go = pool[idx]; go.transform.SetPositionAndRotation(pos, rot); go.SetActive(true); return go; }
        }
        return null; // Žæ‚è“¦‚µ‚ÍŒã‚ÅŠg’£
    }
}
