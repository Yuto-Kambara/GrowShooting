// Assets/Scripts/Enemy/Bullets/EnemyBulletSpec.cs
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum EnemyBulletType { Normal, Beam, Homing }

/// <summary>CSVから敵弾の種類とパラメータを受け取るためのスペック</summary>
public class EnemyBulletSpec
{
    public EnemyBulletType type = EnemyBulletType.Normal;

    // 共通（必要に応じて使う）
    public float damage = 1f;
    public float sizeMul = 1f;

    // ★ 追加: Normal 専用
    public float normalSpeed = 0f;        // 0以下なら「未指定としてプレハブ既定を使用」

    // Beam 専用
    public float beamWidth = 0.5f;      // 画面上の太さ（ワールド単位）
    public float beamDps = 8f;          // 当たっている間の毎秒ダメージ
    public float beamLifetime = 0.75f;  // 何秒出しっぱなしにするか（1発の寿命）

    // Homing 専用
    public float homingSpeed = 6f;           // 移動速度（省略可）
    public float homingTurnRate = 540f;      // 旋回速度(deg/sec)
    public float nearDistance = 2.0f;        // ここまで近づいたら「いつでも切れる」状態になる
    public float breakDistance = 5.0f;       // 一度近づいた後、ここより離れたら追尾終了
    public float maxHomingTime = 2.5f;       // 一定時間で追尾終了
    public float life = 6f;

    public static EnemyBulletSpec FromCsv(string raw)
    {
        // 例: "bullet=beam;width=1.2;dps=15;lifetime=0.6"
        //     "bullet=homing;damage=4;size=1.3;speed=7"
        //     "bullet=normal;damage=2;size=1.0;speed=10"
        var spec = new EnemyBulletSpec();
        if (string.IsNullOrWhiteSpace(raw)) return spec;

        var inv = CultureInfo.InvariantCulture;
        string[] tokens = raw.Trim().Trim('"', '\'').Split(';');

        // 一旦 type と共通/個別キーを読み取る
        EnemyBulletType parsedType = EnemyBulletType.Normal;

        // 一時変数（speed は弾種で割り当てるため一旦保持）
        float tmpDamage = spec.damage;
        float tmpSize = spec.sizeMul;
        float tmpSpeed = 0f; // Normal/Homing 双方の候補

        // 先に type を確定できるよう 2 パスでもいいが、1パスで順次上書きでも OK
        foreach (var tk in tokens)
        {
            var kv = tk.Split('=');
            if (kv.Length != 2) continue;
            string k = kv[0].Trim().ToLower();
            string v = kv[1].Trim().ToLower();

            switch (k)
            {
                case "bullet":
                case "type":
                    if (v == "beam") parsedType = EnemyBulletType.Beam;
                    else if (v == "homing") parsedType = EnemyBulletType.Homing;
                    else parsedType = EnemyBulletType.Normal;
                    break;

                case "damage": float.TryParse(v, NumberStyles.Float, inv, out tmpDamage); break;
                case "size": float.TryParse(v, NumberStyles.Float, inv, out tmpSize); break;

                case "width": float.TryParse(v, NumberStyles.Float, inv, out spec.beamWidth); break;
                case "dps": float.TryParse(v, NumberStyles.Float, inv, out spec.beamDps); break;
                case "lifetime": float.TryParse(v, NumberStyles.Float, inv, out spec.beamLifetime); break;

                case "speed":
                    // Normal/Homing どちらでも使う可能性があるため一旦保持
                    float.TryParse(v, NumberStyles.Float, inv, out tmpSpeed);
                    break;
                case "turn": float.TryParse(v, NumberStyles.Float, inv, out spec.homingTurnRate); break;
                case "near": float.TryParse(v, NumberStyles.Float, inv, out spec.nearDistance); break;
                case "break": float.TryParse(v, NumberStyles.Float, inv, out spec.breakDistance); break;
                case "maxtime": float.TryParse(v, NumberStyles.Float, inv, out spec.maxHomingTime); break;
                case "life": float.TryParse(v, NumberStyles.Float, inv, out spec.life); break;
            }
        }

        // type 確定後に割当とクランプ
        spec.type = parsedType;
        spec.damage = Mathf.Max(0f, tmpDamage);
        spec.sizeMul = Mathf.Max(0.01f, tmpSize);

        // speed の割当（弾種ごと）
        if (parsedType == EnemyBulletType.Homing)
        {
            if (tmpSpeed > 0f) spec.homingSpeed = tmpSpeed;
        }
        else if (parsedType == EnemyBulletType.Normal)
        {
            // 0以下は「未指定（プレハブ既定のまま）」とする
            if (tmpSpeed > 0f) spec.normalSpeed = tmpSpeed;
        }
        // Beam は speed 未使用

        // 既存のクランプ
        spec.beamWidth = Mathf.Max(0.05f, spec.beamWidth);
        spec.beamDps = Mathf.Max(0f, spec.beamDps);
        spec.beamLifetime = Mathf.Max(0.05f, spec.beamLifetime);
        spec.homingSpeed = Mathf.Max(0.1f, spec.homingSpeed);
        spec.homingTurnRate = Mathf.Max(1f, spec.homingTurnRate);
        spec.nearDistance = Mathf.Max(0f, spec.nearDistance);
        spec.breakDistance = Mathf.Max(spec.nearDistance + 0.1f, spec.breakDistance);
        spec.maxHomingTime = Mathf.Max(0.1f, spec.maxHomingTime);

        return spec;
    }
}
