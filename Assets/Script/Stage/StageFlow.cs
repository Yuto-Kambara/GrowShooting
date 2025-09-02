using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;        // ← 小数点パース用

public class StageFlow : MonoBehaviour
{
    [Header("▼ CSV (TextAsset)")]
    public TextAsset csvFile;

    [Header("▼ 敵プレハブ")]
    public GameObject defaultEnemyPrefab;          // 未登録 ID 用フォールバック
    public GameObject[] enemyPrefabs;              // name ↔ prefab 対応表

    //--------------------------------------------------
    class SpawnEvent
    {
        public float time;
        public GameObject prefab;
        public Vector3 pos;
    }
    readonly List<SpawnEvent> events = new();
    int nextIdx = 0;
    float t;

    void Awake()
    {
        if (!csvFile) { Debug.LogError("[StageFlowCsv] CSV が未設定"); return; }
        ParseCsv(csvFile.text);
        events.Sort((a, b) => a.time.CompareTo(b.time));   // 念のため昇順
    }

    void Update()
    {
        t += Time.deltaTime;

        while (nextIdx < events.Count && t >= events[nextIdx].time)
        {
            var e = events[nextIdx++];
            Instantiate(e.prefab, e.pos, Quaternion.identity);
        }
    }

    //--------------------------------------------------
    void ParseCsv(string text)
    {
        var lines = text.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToArray();

        // 1 行目がヘッダならスキップ
        int start = lines[0].StartsWith("time") ? 1 : 0;
        var inv = CultureInfo.InvariantCulture;   // 小数ピリオド固定

        for (int i = start; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 4)
            {
                Debug.LogWarning($"CSV 行 {i + 1} 列数不足（取得={cols.Length}）");
                continue;
            }

            float time = float.Parse(cols[0], inv);
            string id = cols[1].Trim();
            float x = float.Parse(cols[2], inv);
            float y = float.Parse(cols[3], inv);

            events.Add(new SpawnEvent
            {
                time = time,
                prefab = FindPrefab(id),
                pos = new Vector3(x, y, 0f)
            });
        }
    }

    GameObject FindPrefab(string id)
    {
        var p = enemyPrefabs.FirstOrDefault(e => e && e.name == id);
        if (!p)
        {
            Debug.LogWarning($"[StageFlowCsv] id '{id}' 未登録 → default 使用");
            p = defaultEnemyPrefab;
        }
        return p;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 生成前でも CSV を読んで Gizmo 表示したい場合は、
        // Editor 拡張で再読込するか、手動で呼び出してください。
    }
#endif
}
