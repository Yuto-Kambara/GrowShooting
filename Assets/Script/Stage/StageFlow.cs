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
        public List<Vector3> path;
        public FireDirSpec fire;  // ★ 追加
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
            var go = Instantiate(e.prefab, e.path[0], Quaternion.identity);

            // 経路ムーバー
            var mover = go.GetComponent<EnemyMover>();
            if (!mover) mover = go.AddComponent<EnemyMover>();
            mover.InitPath(e.path, e.pattern, e.speed);

            // ★ 射撃方向設定を敵内の全 Shooter に配布
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

            // ★ shoot 列（任意）
            FireDirSpec fire = FireDirSpec.Fixed(Vector2.left); // 既定
            if (cols.Length >= 6)
            {
                fire = ParseShoot(SafeTrimQuotes(cols[5]));
            }

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

    // ★ 追加：shoot のパース
    FireDirSpec ParseShoot(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return FireDirSpec.Fixed(Vector2.left);

        var s = raw.Trim().ToLower();

        // 1) player
        if (s == "player" || s == "atplayer") return FireDirSpec.AtPlayer();

        // 2) deg=xxx / deg:xxx
        if (s.StartsWith("deg"))
        {
            var parts = s.Split('=', ':');
            if (parts.Length == 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float deg))
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector2 d = new(Mathf.Cos(rad), Mathf.Sin(rad));
                return FireDirSpec.Fixed(d);
            }
        }

        // 3) vec=dx~dy / dx~dy
        if (s.StartsWith("vec=")) s = s.Substring(4);
        var xy = s.Split('~');
        if (xy.Length != 2) xy = s.Split(':'); // 旧式も許容
        if (xy.Length == 2 &&
            float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            return FireDirSpec.Fixed(new Vector2(x, y));
        }

        // 不正なら既定
        Debug.LogWarning($"[StageFlowCsv] shoot='{raw}' を解釈できません。leftにフォールバック");
        return FireDirSpec.Fixed(Vector2.left);
    }

    // （前回の ~ 対応・引用符除去・Excel 対策版）
    List<Vector3> ParsePath(string raw)
    {
        var inv = CultureInfo.InvariantCulture;
        var pts = new List<Vector3>();
        if (string.IsNullOrWhiteSpace(raw)) return pts;

        var segs = raw.Split('|');
        foreach (var s in segs)
        {
            var p = s.Trim().Trim('(', ')', '"', '\'');
            if (string.IsNullOrEmpty(p)) continue;

            var xy = p.Split('~');
            if (xy.Length != 2) xy = p.Split(':');   // 旧式も許容
            if (xy.Length != 2) continue;

            if (float.TryParse(xy[0].Trim(), NumberStyles.Float, inv, out float x) &&
                float.TryParse(xy[1].Trim(), NumberStyles.Float, inv, out float y))
            {
                pts.Add(new Vector3(x, y, 0f));
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
