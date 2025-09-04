using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class StageFlow : MonoBehaviour
{
    [Header("▼ CSV (TextAsset)")]
    public TextAsset csvFile;

    [Header("▼ 敵プレハブ")]
    public GameObject defaultEnemyPrefab;
    public GameObject[] enemyPrefabs;

    class SpawnEvent
    {
        public float time;
        public GameObject prefab;
        public EnemyMover.MotionPattern pattern;
        public float speed;
        public List<EnemyMover.Waypoint> path; // ★ 位置 + 待機
        public FireDirSpec fire;
    }

    readonly List<SpawnEvent> events = new();
    int nextIdx = 0;
    float t;

    void Awake()
    {
        if (!csvFile) { Debug.LogError("[StageFlowCsv] CSV が未設定"); return; }
        ParseCsv(csvFile.text);
        events.Sort((a, b) => a.time.CompareTo(b.time));
    }

    void Update()
    {
        t += Time.deltaTime;
        while (nextIdx < events.Count && t >= events[nextIdx].time)
        {
            var e = events[nextIdx++];
            var go = Instantiate(e.prefab, e.path[0].pos, Quaternion.identity);

            // 経路ムーバー
            var mover = go.GetComponent<EnemyMover>();
            if (!mover) mover = go.AddComponent<EnemyMover>();
            mover.InitPath(e.path, e.pattern, e.speed);

            // 射撃方向（前ターンで実装）
            var shooters = go.GetComponentsInChildren<EnemyShooter>(true);
            foreach (var s in shooters) s.ApplyFireDirection(e.fire);
        }
    }

    // ----------------- CSV 解析 -----------------
    void ParseCsv(string text)
    {
        var lines = text.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                        .ToArray();

        int start = lines[0].StartsWith("time") ? 1 : 0;
        var inv = CultureInfo.InvariantCulture;

        for (int i = start; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 5)
            {
                Debug.LogWarning($"CSV 行{i + 1}: 列不足 (need>=5) : {lines[i]}");
                continue;
            }

            float time = float.Parse(cols[0], inv);
            string id = cols[1].Trim();
            var pattern = ParsePattern(cols[2].Trim());
            float speed = float.Parse(cols[3], inv);
            string pathRaw = SafeTrimQuotes(cols[4]);
            var path = ParsePath(pathRaw);

            if (path == null || path.Count < 2)
            {
                Debug.LogWarning($"CSV 行{i + 1}: path が不正（最低2点必要）: {cols[4]}");
                continue;
            }

            FireDirSpec fire = FireDirSpec.Fixed(Vector2.left);
            if (cols.Length >= 6) fire = ParseShoot(SafeTrimQuotes(cols[5]));

            events.Add(new SpawnEvent
            {
                time = time,
                prefab = FindPrefab(id),
                pattern = pattern,
                speed = speed,
                path = path,
                fire = fire
            });
        }
    }

    string SafeTrimQuotes(string s) => s.Trim().Trim('"').Trim('\'');

    EnemyMover.MotionPattern ParsePattern(string s)
    {
        return s.ToLower() switch
        {
            "linear" => EnemyMover.MotionPattern.Linear,
            "smooth" => EnemyMover.MotionPattern.SmoothSpline,
            "smoothspline" => EnemyMover.MotionPattern.SmoothSpline,
            "arrive" => EnemyMover.MotionPattern.Arrive,
            _ => EnemyMover.MotionPattern.Linear,
        };
    }

    // --- shoot は前ターンの実装をそのまま利用 ---
    FireDirSpec ParseShoot(string raw) { /* 省略（前回答のまま） */ return FireDirSpec.Fixed(Vector2.left); }

    /// <summary>
    /// path 例:  "-9~3@0.5 | -3~2 | 3~1@1.2 | 9~1"
    ///            ↑ 到達後0.5秒停止      ↑ 1.2秒停止
    /// 旧式 "x:y" も後方互換で読める
    /// </summary>
    List<EnemyMover.Waypoint> ParsePath(string raw)
    {
        var inv = CultureInfo.InvariantCulture;
        var pts = new List<EnemyMover.Waypoint>();
        if (string.IsNullOrWhiteSpace(raw)) return pts;

        var segs = raw.Split('|');
        foreach (var s in segs)
        {
            // 各点の左右空白と括弧/引用符を除去
            var p = s.Trim().Trim('(', ')', '"', '\'');
            if (string.IsNullOrEmpty(p)) continue;

            // "@wait" を切り出し
            float wait = 0f;
            string xyPart = p;
            int at = p.LastIndexOf('@');
            if (at >= 0)
            {
                xyPart = p.Substring(0, at).Trim();
                var wStr = p.Substring(at + 1).Trim();
                if (!float.TryParse(wStr, NumberStyles.Float, inv, out wait)) wait = 0f;
                wait = Mathf.Max(0f, wait);
            }

            // 座標は "~" 優先、":" 互換
            string[] xy = xyPart.Split('~');
            if (xy.Length != 2) xy = xyPart.Split(':');
            if (xy.Length != 2) continue;

            if (float.TryParse(xy[0].Trim(), NumberStyles.Float, inv, out float x) &&
                float.TryParse(xy[1].Trim(), NumberStyles.Float, inv, out float y))
            {
                pts.Add(new EnemyMover.Waypoint(new Vector3(x, y, 0f), wait));
            }
        }
        return pts;
    }

    GameObject FindPrefab(string id)
    {
        var p = enemyPrefabs.FirstOrDefault(e => e && e.name == id);
        if (!p) { Debug.LogWarning($"[StageFlowCsv] id '{id}' 未登録 → default 使用"); p = defaultEnemyPrefab; }
        return p;
    }
}
