// Assets/Scripts/Stage/StageFlow.cs
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

    [Header("Boss Spawn Defaults")]
    [Tooltip("CSVにpathが無いボス行のスポーン既定座標")]
    public Vector2 defaultBossSpawn = new Vector2(12f, 0f);

    enum SpawnKind { Normal, MidBoss, FinalBoss }

    class SpawnEvent
    {
        public float time;
        public SpawnKind kind;

        public GameObject prefab;

        // --- 通常敵のみ使用 ---
        public EnemyMover.MotionPattern pattern;
        public float speed;
        public List<EnemyMover.Waypoint> path;

        // --- 共通（射撃）---
        public FireDirSpec fire;
        public FireTimingSpec timing;
        public EnemyBulletSpec bulletSpec;

        // --- ボス用（移動はプレハブの Mover に委譲）---
        public Vector2? bossSpawnPos; // null の場合は defaultBossSpawn
    }

    readonly List<SpawnEvent> events = new();
    int nextIdx = 0;

    // ★ 変更: ステージ実時間と、スポーン用スケジュール時間を分離
    float levelTime = 0f;     // 常に進む
    float scheduleTime = 0f;  // ボス中は停止（= 凍結）

    bool pausedByBoss; // BossManager から制御
    public void SetPausedByBoss(bool v) => pausedByBoss = v;

    void Awake()
    {
        if (!csvFile) { Debug.LogError("[StageFlow] CSV が未設定"); return; }
        ParseCsv(csvFile.text);
        events.Sort((a, b) => a.time.CompareTo(b.time));

        // 念のため初期化
        nextIdx = 0;
        levelTime = 0f;
        scheduleTime = 0f;
        pausedByBoss = false;
    }

    void Update()
    {
        // ★ 実時間は常に進める
        levelTime += Time.deltaTime;

        // ★ ボス中はスポーンの時間を止める（= 凍結）
        if (!pausedByBoss)
            scheduleTime += Time.deltaTime;

        // ★ スポーン判定は scheduleTime を使う
        while (nextIdx < events.Count && scheduleTime >= events[nextIdx].time)
        {
            var e = events[nextIdx];

            // ボス中は通常敵の出現を停止（ボス自身の行は通す）
            if (pausedByBoss && e.kind == SpawnKind.Normal) break;

            nextIdx++;

            // --- 生成位置 ---
            Vector3 spawnPos;
            if (e.kind == SpawnKind.Normal)
            {
                // 通常敵：path の先頭
                spawnPos = (e.path != null && e.path.Count > 0) ? (Vector3)e.path[0].pos : Vector3.zero;
            }
            else
            {
                // ボス：pathは使わず、bossSpawnPos -> default
                Vector2 p = e.bossSpawnPos ?? defaultBossSpawn;
                spawnPos = new Vector3(p.x, p.y, 0f);
            }

            var go = Instantiate(e.prefab, spawnPos, Quaternion.identity);

            // --- 移動の割当 ---
            if (e.kind == SpawnKind.Normal)
            {
                var mover = go.GetComponent<EnemyMover>();
                if (!mover) mover = go.AddComponent<EnemyMover>();
                mover.InitPath(e.path, e.pattern, e.speed);
            }
            else
            {
                // ボス：Moverはプレハブに付いている MidBossMover/FinalBossMover に任せる
                // ここでは何もしない
            }

            // --- 射撃設定は共通 ---
            var shooters = go.GetComponentsInChildren<EnemyShooter>(true);
            if (e.kind == SpawnKind.Normal)
            {
                foreach (var s in shooters)
                {
                    s.ApplyFireDirection(e.fire);
                    s.ApplyFireTiming(e.timing);
                    s.ApplyBulletSpec(e.bulletSpec);
                }
            }

            // --- ボス行なら、以降の通常敵スポーンを一時停止（タイムライン凍結） ---
            if (e.kind == SpawnKind.MidBoss || e.kind == SpawnKind.FinalBoss)
                SetPausedByBoss(true);
        }
    }

    // ----------------- CSV 解析 -----------------
    void ParseCsv(string text)
    {
        var lines = text.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                        .ToArray();

        // 既定の列並び: time,id,pattern,speed,path,shoot,timing,bulletSpec,boss
        int start = lines[0].StartsWith("time") ? 1 : 0;
        var inv = CultureInfo.InvariantCulture;

        for (int i = start; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 2)
            {
                Debug.LogWarning($"CSV 行{i + 1}: 最低限の列不足 : {lines[i]}");
                continue;
            }

            // time / id
            if (!float.TryParse(cols[0], NumberStyles.Float, inv, out float time))
            {
                Debug.LogWarning($"CSV 行{i + 1}: time が不正 : {cols[0]}");
                continue;
            }
            string id = cols[1].Trim();

            // boss 列（最後の列を想定。ただし柔軟に見に行く）
            SpawnKind kind = SpawnKind.Normal;
            if (cols.Length >= 9)
            {
                string bossRaw = SafeTrimQuotes(cols[8]).ToLower();
                kind = ParseBossKind(bossRaw);
            }

            // 共通：射撃
            FireDirSpec fire = FireDirSpec.Fixed(Vector2.left);
            if (cols.Length >= 6) fire = ParseShoot(SafeTrimQuotes(cols[5]));

            FireTimingSpec timing = FireTimingSpec.Default();
            if (cols.Length >= 7) timing = ParseFireTiming(SafeTrimQuotes(cols[6]));

            EnemyBulletSpec bulletSpec = new EnemyBulletSpec();
            if (cols.Length >= 8) bulletSpec = EnemyBulletSpec.FromCsv(SafeTrimQuotes(cols[7]));

            // 通常敵用：動き（pattern/speed/path）
            EnemyMover.MotionPattern pattern = EnemyMover.MotionPattern.Linear;
            float speed = 2f;
            List<EnemyMover.Waypoint> path = null;

            // ボス用：スポーン座標（pathの先頭を位置取りにだけ使う。無ければ既定値）
            Vector2? bossSpawn = null;

            if (kind == SpawnKind.Normal)
            {
                if (cols.Length >= 3) pattern = ParsePattern(cols[2].Trim());
                if (cols.Length >= 4 && float.TryParse(cols[3], NumberStyles.Float, inv, out float sp)) speed = sp;
                if (cols.Length >= 5) path = ParsePath(SafeTrimQuotes(cols[4]));
                if (path == null || path.Count < 2)
                {
                    Debug.LogWarning($"CSV 行{i + 1}: path が不正（最低2点必要）: {(cols.Length >= 5 ? cols[4] : "")}");
                    continue;
                }
            }
            else
            {
                // ボス：動きはCSVを撤廃。pathがあればスポーン位置だけ拾う
                if (cols.Length >= 5)
                {
                    var tmp = ParsePath(SafeTrimQuotes(cols[4]));
                    if (tmp != null && tmp.Count > 0) bossSpawn = tmp[0].pos;
                }
            }

            events.Add(new SpawnEvent
            {
                time = time,
                kind = kind,
                prefab = FindPrefab(id),
                pattern = pattern,
                speed = speed,
                path = path,
                fire = fire,
                timing = timing,
                bulletSpec = bulletSpec,
                bossSpawnPos = bossSpawn
            });
        }
    }

    string SafeTrimQuotes(string s) => s.Trim().Trim('"').Trim('\'');

    SpawnKind ParseBossKind(string s)
    {
        s = s?.Trim().ToLower();
        if (string.IsNullOrEmpty(s)) return SpawnKind.Normal;
        if (s == "mid" || s == "midboss" || s == "mid-boss") return SpawnKind.MidBoss;
        if (s == "final" || s == "boss" || s == "last" || s == "finalboss") return SpawnKind.FinalBoss;
        return SpawnKind.Normal;
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

    FireDirSpec ParseShoot(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return FireDirSpec.Fixed(Vector2.left);

        var s = raw.Trim().ToLower();

        if (s == "player" || s == "atplayer") return FireDirSpec.AtPlayer();

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

        if (s.StartsWith("vec=")) s = s.Substring(4);
        var xy = s.Split('~');
        if (xy.Length != 2) xy = s.Split(':');
        if (xy.Length == 2 &&
            float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
        {
            return FireDirSpec.Fixed(new Vector2(x, y));
        }

        Debug.LogWarning($"[StageFlow] shoot='{raw}' を解釈できません。leftにフォールバック");
        return FireDirSpec.Fixed(Vector2.left);
    }

    FireTimingSpec ParseFireTiming(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return FireTimingSpec.Default();

        string s = raw.Trim().ToLower();

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

        Debug.LogWarning($"[StageFlow] fireTiming '{raw}' を解釈できません（既定にフォールバック）");
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
        if (!p) { Debug.LogWarning($"[StageFlow] id '{id}' 未登録 → default 使用"); p = defaultEnemyPrefab; }
        return p;
    }
}
