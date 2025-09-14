using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;

/// <summary>
/// CSVドリブンのステージ進行：
/// - 敵の出現
/// - 経路/速度
/// - 射撃方向/タイミング
/// - 弾種とパラメータ（Normal/Beam/Homing）
/// - ★ Boss対応：boss列（mid/final）で中ボス/大ボス出現 → スクロール停止 & BGM切替（BossMarker経由）
///
/// 期待CSV（後方互換・ヘッダ任意）:
/// time,id,pattern,speed,path,shoot,timing,bulletSpec,boss
/// 例:
/// 45.0,MidBoss01,arrive,0,"(12~0|8~0)",player,,,"mid"
/// 90.0,FinalBoss01,arrive,0,"(12~0|8~0)",player,,,"final"
/// </summary>
public class StageFlow : MonoBehaviour
{
    [Header("▼ CSV (TextAsset)")]
    public TextAsset csvFile;

    [Header("▼ 敵プレハブ")]
    public GameObject defaultEnemyPrefab;
    public GameObject[] enemyPrefabs;

    // ==== 内部型 ====
    enum SpawnKind { Normal, MidBoss, FinalBoss }

    class SpawnEvent
    {
        public float time;
        public GameObject prefab;
        public EnemyMover.MotionPattern pattern;
        public float speed;
        public List<EnemyMover.Waypoint> path;
        public FireDirSpec fire;
        public FireTimingSpec timing;

        // 弾種スペック（Normal/Beam/Homing）
        public EnemyBulletSpec bulletSpec = new EnemyBulletSpec();

        // Boss種別
        public SpawnKind kind = SpawnKind.Normal;
    }

    // ==== ランタイム ====
    readonly List<SpawnEvent> events = new();
    int nextIdx = 0;
    float t;

    // ボス中は通常湧きを止める
    bool pausedByBoss = false;

    // ヘッダ名→列インデックス（ヘッダ無しCSVも後方互換で読める）
    Dictionary<string, int> header = null;

    // ==== ライフサイクル ====
    void Awake()
    {
        if (!csvFile) { Debug.LogError("[StageFlowCsv] CSV が未設定"); return; }
        ParseCsv(csvFile.text);
        events.Sort((a, b) => a.time.CompareTo(b.time));
    }

    void OnEnable()
    {
        // 途中から再開できるように初期化
        t = 0f; nextIdx = 0;
        pausedByBoss = false;
    }

    void Update()
    {
        t += Time.deltaTime;

        // 経過時間に応じてイベントを消化
        while (nextIdx < events.Count && t >= events[nextIdx].time)
        {
            var e = events[nextIdx];

            // ボス中は「通常敵」の出現を止める（ボスイベントは通す）
            if (pausedByBoss && e.kind == SpawnKind.Normal) break;

            nextIdx++;

            // 出現
            var go = Instantiate(e.prefab, e.path[0].pos, Quaternion.identity);

            // 経路ムーバー
            var mover = go.GetComponent<EnemyMover>();
            if (!mover) mover = go.AddComponent<EnemyMover>();
            mover.InitPath(e.path, e.pattern, e.speed);

            // 射撃（子に EnemyShooter があれば適用）
            var shooters = go.GetComponentsInChildren<EnemyShooter>(true);
            foreach (var s in shooters)
            {
                s.ApplyFireDirection(e.fire);
                s.ApplyFireTiming(e.timing);
                s.ApplyBulletSpec(e.bulletSpec);
            }

            // Bossなら印を付与（BossMarker が無ければ付ける）
            if (e.kind == SpawnKind.MidBoss || e.kind == SpawnKind.FinalBoss)
            {
                var marker = go.GetComponent<BossMarker>();
                if (!marker) marker = go.AddComponent<BossMarker>();
                marker.bossType = (e.kind == SpawnKind.MidBoss) ? BossType.Mid : BossType.Final;

                // ステージ側もロック（以後、通常湧きは停止）
                SetPausedByBoss(true);
                // BGM切替/スクロール停止は BossMarker.OnEnable → BossManager.BeginBoss にて実行
            }
        }
    }

    // ==== 外部API（BossManagerからも呼べるように） ====
    public void SetPausedByBoss(bool v) => pausedByBoss = v;

    // ==== CSV 解析 ====
    void ParseCsv(string text)
    {
        var rawLines = text.Split('\n');
        var lines = rawLines
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
            .ToArray();

        if (lines.Length == 0) { Debug.LogWarning("[StageFlowCsv] 空CSV"); return; }

        // ヘッダ判定（1行目に time 等の語が含まれていればヘッダとみなす）
        int startIndex = 0;
        if (lines[0].IndexOf("time", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            header = BuildHeader(lines[0]);
            startIndex = 1;
        }
        var inv = CultureInfo.InvariantCulture;

        for (int i = startIndex; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);

            // time
            string timeStr = GetCol(cols, "time", 0, allowMissing: false);
            if (!float.TryParse(timeStr, NumberStyles.Float, inv, out float time))
            {
                Debug.LogWarning($"CSV 行{i + 1}: time が不正: '{timeStr}'");
                continue;
            }

            // id → prefab
            string id = SafeTrimQuotes(GetCol(cols, "id", 1, allowMissing: false));
            var prefab = FindPrefab(id);

            // pattern
            string patStr = SafeTrimQuotes(GetCol(cols, "pattern", 2, allowMissing: true));
            var pattern = ParsePattern(patStr);

            // speed
            string spdStr = SafeTrimQuotes(GetCol(cols, "speed", 3, allowMissing: true));
            float speed = ParseFloat(spdStr, 0f, inv);

            // path
            string pathRaw = SafeTrimQuotes(GetCol(cols, "path", 4, allowMissing: false));
            var path = ParsePath(pathRaw);
            if (path == null || path.Count < 1)
            {
                Debug.LogWarning($"CSV 行{i + 1}: path 不正: '{pathRaw}'");
                continue;
            }

            // shoot
            FireDirSpec fire = FireDirSpec.Fixed(Vector2.left);
            if (HasCol(cols, "shoot", 5))
                fire = ParseShoot(SafeTrimQuotes(GetCol(cols, "shoot", 5, allowMissing: true)));

            // timing
            FireTimingSpec timing = FireTimingSpec.Default();
            if (HasCol(cols, "timing", 6))
                timing = ParseFireTiming(SafeTrimQuotes(GetCol(cols, "timing", 6, allowMissing: true)));

            // bulletSpec
            EnemyBulletSpec bulletSpec = new EnemyBulletSpec();
            if (HasCol(cols, "bulletspec", 7))
                bulletSpec = EnemyBulletSpec.FromCsv(SafeTrimQuotes(GetCol(cols, "bulletspec", 7, allowMissing: true)));

            // boss
            SpawnKind kind = SpawnKind.Normal;
            if (HasCol(cols, "boss", 8))
            {
                string b = SafeTrimQuotes(GetCol(cols, "boss", 8, allowMissing: true)).ToLowerInvariant();
                if (b == "mid" || b == "midboss" || b == "mid-boss") kind = SpawnKind.MidBoss;
                else if (b == "final" || b == "boss" || b == "last" || b == "finalboss") kind = SpawnKind.FinalBoss;
            }

            events.Add(new SpawnEvent
            {
                time = time,
                prefab = prefab,
                pattern = pattern,
                speed = speed,
                path = path,
                fire = fire,
                timing = timing,
                bulletSpec = bulletSpec,
                kind = kind
            });
        }
    }

    // ==== CSV utils ====
    Dictionary<string, int> BuildHeader(string headerLine)
    {
        var cols = SplitCsvLine(headerLine);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < cols.Length; i++)
        {
            string key = cols[i].Trim().Trim('"', '\'').ToLowerInvariant();
            if (!string.IsNullOrEmpty(key)) map[key] = i;
        }
        return map;
    }

    bool HasCol(string[] cols, string name, int fallbackIndex)
    {
        if (header != null && header.TryGetValue(name, out int idx))
            return idx >= 0 && idx < cols.Length;
        return fallbackIndex >= 0 && fallbackIndex < cols.Length;
    }

    string GetCol(string[] cols, string name, int fallbackIndex, bool allowMissing)
    {
        if (header != null && header.TryGetValue(name, out int idx))
        {
            if (idx >= 0 && idx < cols.Length) return cols[idx];
            return allowMissing ? string.Empty : "";
        }
        if (fallbackIndex >= 0 && fallbackIndex < cols.Length) return cols[fallbackIndex];
        return allowMissing ? string.Empty : "";
    }

    string[] SplitCsvLine(string line)
    {
        // シンプルなCSV分割：ダブルクオート含む複雑ケースが必要なら差し替え
        return line.Split(',');
    }

    string SafeTrimQuotes(string s) => (s ?? string.Empty).Trim().Trim('"').Trim('\'');

    float ParseFloat(string s, float def, IFormatProvider inv)
    {
        if (string.IsNullOrWhiteSpace(s)) return def;
        return float.TryParse(s, NumberStyles.Float, inv, out var v) ? v : def;
    }

    EnemyMover.MotionPattern ParsePattern(string s)
    {
        s = (s ?? "").ToLowerInvariant();
        return s switch
        {
            "linear" => EnemyMover.MotionPattern.Linear,
            "smooth" => EnemyMover.MotionPattern.SmoothSpline,
            "smoothspline" => EnemyMover.MotionPattern.SmoothSpline,
            "arrive" => EnemyMover.MotionPattern.Arrive,
            _ => EnemyMover.MotionPattern.Linear,
        };
    }

    FireDirSpec ParseShoot(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return FireDirSpec.Fixed(Vector2.left);

        var s = raw.Trim().ToLowerInvariant();

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
        if (xy.Length != 2) xy = s.Split(':'); // 互換
        if (xy.Length == 2 &&
            float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            return FireDirSpec.Fixed(new Vector2(x, y));
        }

        Debug.LogWarning($"[StageFlowCsv] shoot='{raw}' を解釈できません。leftにフォールバック");
        return FireDirSpec.Fixed(Vector2.left);
    }

    FireTimingSpec ParseFireTiming(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return FireTimingSpec.Default();

        string s = raw.Trim().ToLowerInvariant();

        // 形式1) every=0.8            …等間隔発射（0.8 秒ごと）
        // 形式1') every=0.8@0.3        …開始遅延 0.3 秒つき
        // 形式1'') every=0.8;delay=0.3 …CSV向け
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
            // 各点の括弧/引用符を除去
            var p = s.Trim().Trim('(', ')', '"', '\'');
            if (string.IsNullOrEmpty(p)) continue;

            // "@wait" を切り出し（省略可）
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

            // "~" 優先、":" 互換
            string[] xy = xyPart.Split('~');
            if (xy.Length != 2) xy = xyPart.Split(':');
            if (xy.Length != 2) continue;

            if (float.TryParse(xy[0].Trim(), NumberStyles.Float, inv, out float x) &&
                float.TryParse(xy[1].Trim(), NumberStyles.Float, inv, out float y))
            {
                pts.Add(new EnemyMover.Waypoint(new Vector3(x, y, 0f), wait));
            }
        }
        // 経路が1点のみでも最低限動作（出現位置=最初の点）
        if (pts.Count == 0) pts.Add(new EnemyMover.Waypoint(Vector3.zero, 0f));
        return pts;
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
}
