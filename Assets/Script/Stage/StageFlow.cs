using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;

/// <summary>
/// CSVドリブンのステージ進行：
/// - 敵の出現
/// - 経路/速度
/// - 射撃方向/タイミング（従来）
/// - ★ 弾種とパラメータ（Normal/Beam/Homing）
/// </summary>
public class StageFlow : MonoBehaviour
{
    [Header("▼ CSV (TextAsset)")]
    public TextAsset csvFile;

    [Header("▼ 敵プレハブ")]
    public GameObject defaultEnemyPrefab;
    public GameObject[] enemyPrefabs;

    // === 新規：敵弾種 ===
    public enum EnemyBulletType { Normal = 0, Beam = 1, Homing = 2 }

    class SpawnEvent
    {
        public float time;
        public GameObject prefab;
        public EnemyMover.MotionPattern pattern;
        public float speed;
        public List<EnemyMover.Waypoint> path;
        public FireDirSpec fire;
        public FireTimingSpec timing;

        // ★ 追加：弾スペック
        public EnemyBulletSpec bulletSpec = new EnemyBulletSpec();
    }


    readonly List<SpawnEvent> events = new();
    int nextIdx = 0;
    float t;

    // ヘッダ名→列インデックス
    Dictionary<string, int> header = null;

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

            var mover = go.GetComponent<EnemyMover>();
            if (!mover) mover = go.AddComponent<EnemyMover>();
            mover.InitPath(e.path, e.pattern, e.speed);

            var shooters = go.GetComponentsInChildren<EnemyShooter>(true);
            foreach (var s in shooters)
            {
                s.ApplyFireDirection(e.fire);
                s.ApplyFireTiming(e.timing);
                s.ApplyBulletSpec(e.bulletSpec);   // ★ 追加
            }
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
            if (cols.Length < 5) { Debug.LogWarning($"CSV 行{i + 1}: 列不足"); continue; }

            float time = float.Parse(cols[0], inv);
            string id = cols[1].Trim();
            var pattern = ParsePattern(cols[2].Trim());
            float speed = float.Parse(cols[3], inv);
            string pathRaw = SafeTrimQuotes(cols[4]);
            var path = ParsePath(pathRaw);
            if (path == null || path.Count < 2) { Debug.LogWarning($"CSV 行{i + 1}: path 不正"); continue; }

            FireDirSpec fire = FireDirSpec.Fixed(Vector2.left);
            if (cols.Length >= 6) fire = ParseShoot(SafeTrimQuotes(cols[5]));

            FireTimingSpec timing = FireTimingSpec.Default();
            if (cols.Length >= 7) timing = ParseFireTiming(SafeTrimQuotes(cols[6]));

            // ★ 追加：Bullet Spec（列8）
            EnemyBulletSpec bulletSpec = new EnemyBulletSpec();
            if (cols.Length >= 8) bulletSpec = EnemyBulletSpec.FromCsv(SafeTrimQuotes(cols[7]));

            events.Add(new SpawnEvent
            {
                time = time,
                prefab = FindPrefab(id),
                pattern = pattern,
                speed = speed,
                path = path,
                fire = fire,
                timing = timing,
                bulletSpec = bulletSpec
            });
        }
    }

    // --- CSV utilities ---
    Dictionary<string, int> BuildHeader(string headerLine)
    {
        var cols = SplitCsvLine(headerLine);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < cols.Length; i++)
        {
            string key = cols[i].Trim().Trim('"', '\'').ToLowerInvariant();
            map[key] = i;
        }
        return map;
    }

    string GetCol(string[] cols, string name, int fallbackIndex, bool allowMissing = false)
    {
        // ヘッダがあれば名前で、なければインデックスで取得
        if (header != null && header.TryGetValue(name, out int idx))
        {
            if (idx >= 0 && idx < cols.Length) return cols[idx];
            return string.Empty;
        }
        // 旧CSV互換：固定列位置
        if (fallbackIndex >= 0 && fallbackIndex < cols.Length) return cols[fallbackIndex];
        return allowMissing ? string.Empty : "";
    }

    string[] SplitCsvLine(string line)
    {
        // シンプルな CSV 分割（ダブルクオート内のカンマに弱いですが既存互換）
        return line.Split(',');
    }

    string SafeTrimQuotes(string s) => (s ?? string.Empty).Trim().Trim('"').Trim('\'');

    float ParseFloat(string s, float def, IFormatProvider inv)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;
        return float.TryParse(s, NumberStyles.Float, inv, out var v) ? v : def;
    }

    int ParseInt(string s, int def, IFormatProvider inv)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;
        return int.TryParse(s, NumberStyles.Integer, inv, out var v) ? v : def;
    }

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

    EnemyBulletType ParseBulletType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return EnemyBulletType.Normal;
        string s = raw.Trim().ToLowerInvariant();
        return s switch
        {
            "beam" => EnemyBulletType.Beam,
            "homing" => EnemyBulletType.Homing,
            "normal" => EnemyBulletType.Normal,
            _ => EnemyBulletType.Normal
        };
    }

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

    FireTimingSpec ParseFireTiming(string raw)
    {
        // 空・未指定 → 既定（fireRate で等間隔）
        if (string.IsNullOrWhiteSpace(raw)) return FireTimingSpec.Default();

        string s = raw.Trim().ToLower();

        // 形式1) every=0.8            …等間隔発射（0.8 秒ごと）
        // 形式1') every=0.8@0.3        …開始遅延 0.3 秒つき
        // 形式1'') every=0.8;delay=0.3 …セパレータに ; を使用（CSV 的に扱いやすい）
        if (s.StartsWith("every") || s.StartsWith("interval"))
        {
            int eq = s.IndexOf('=');
            string rest = (eq >= 0) ? s.Substring(eq + 1) : s;

            float interval = 0f, delay = 0f;

            var atParts = rest.Split('@');
            if (atParts.Length >= 1)
            {
                float.TryParse(atParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out interval);
                if (atParts.Length >= 2)
                    float.TryParse(atParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out delay);
            }

            var semi = rest.Split(';');
            foreach (var token in semi)
            {
                var kv = token.Split('=');
                if (kv.Length == 2 && kv[0].Trim() == "delay")
                    float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out delay);
            }

            interval = Mathf.Max(0.01f, interval);
            delay = Mathf.Max(0f, delay);
            return FireTimingSpec.Every(interval, delay);
        }

        // 形式2) times=(0.5|1.2|3.0)  / times=0.5|1.2|3
        // 形式2') t=0.4|0.6|1.0       / (0.4|0.6|1.0)
        if (s.StartsWith("times=") || s.StartsWith("t=") || s.StartsWith("("))
        {
            int eq = s.IndexOf('=');
            string list = (eq >= 0) ? s.Substring(eq + 1) : s;
            list = list.Trim().Trim('(', ')', '"', '\'');

            var parts = list.Split('|');
            var times = new List<float>();
            foreach (var p in parts)
            {
                if (float.TryParse(p.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                    times.Add(Mathf.Max(0f, v));
            }
            times.Sort();
            return FireTimingSpec.Timeline(times);
        }

        Debug.LogWarning($"[StageFlowCsv] fireTiming '{raw}' を解釈できません（既定にフォールバック）");
        return FireTimingSpec.Default();
    }

    List<EnemyMover.Waypoint> ParsePath(string raw)
    {
        var inv = CultureInfo.InvariantCulture;
        var pts = new List<EnemyMover.Waypoint>();
        if (string.IsNullOrWhiteSpace(raw)) return pts;

        var segs = raw.Split('|');
        foreach (var s in segs)
        {
            var p = s.Trim().Trim('(', ')', '"', '\'');
            if (string.IsNullOrEmpty(p)) continue;

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
