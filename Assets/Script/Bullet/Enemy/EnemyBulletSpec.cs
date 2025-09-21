// Assets/Scripts/Enemy/Bullets/EnemyBulletSpec.cs
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum EnemyBulletType { Normal, Beam, Homing }

/// <summary>CSV����G�e�̎�ނƃp�����[�^���󂯎�邽�߂̃X�y�b�N</summary>
public class EnemyBulletSpec
{
    public EnemyBulletType type = EnemyBulletType.Normal;

    // ���ʁi�K�v�ɉ����Ďg���j
    public float damage = 1f;
    public float sizeMul = 1f;

    // �� �ǉ�: Normal ��p
    public float normalSpeed = 0f;        // 0�ȉ��Ȃ�u���w��Ƃ��ăv���n�u������g�p�v

    // Beam ��p
    public float beamWidth = 0.5f;      // ��ʏ�̑����i���[���h�P�ʁj
    public float beamDps = 8f;          // �������Ă���Ԃ̖��b�_���[�W
    public float beamLifetime = 0.75f;  // ���b�o�����ςȂ��ɂ��邩�i1���̎����j

    // Homing ��p
    public float homingSpeed = 6f;           // �ړ����x�i�ȗ��j
    public float homingTurnRate = 540f;      // ���񑬓x(deg/sec)
    public float nearDistance = 2.0f;        // �����܂ŋ߂Â�����u���ł��؂��v��ԂɂȂ�
    public float breakDistance = 5.0f;       // ��x�߂Â�����A������藣�ꂽ��ǔ��I��
    public float maxHomingTime = 2.5f;       // ��莞�ԂŒǔ��I��
    public float life = 6f;

    public static EnemyBulletSpec FromCsv(string raw)
    {
        // ��: "bullet=beam;width=1.2;dps=15;lifetime=0.6"
        //     "bullet=homing;damage=4;size=1.3;speed=7"
        //     "bullet=normal;damage=2;size=1.0;speed=10"
        var spec = new EnemyBulletSpec();
        if (string.IsNullOrWhiteSpace(raw)) return spec;

        var inv = CultureInfo.InvariantCulture;
        string[] tokens = raw.Trim().Trim('"', '\'').Split(';');

        // ��U type �Ƌ���/�ʃL�[��ǂݎ��
        EnemyBulletType parsedType = EnemyBulletType.Normal;

        // �ꎞ�ϐ��ispeed �͒e��Ŋ��蓖�Ă邽�߈�U�ێ��j
        float tmpDamage = spec.damage;
        float tmpSize = spec.sizeMul;
        float tmpSpeed = 0f; // Normal/Homing �o���̌��

        // ��� type ���m��ł���悤 2 �p�X�ł��������A1�p�X�ŏ����㏑���ł� OK
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
                    // Normal/Homing �ǂ���ł��g���\�������邽�߈�U�ێ�
                    float.TryParse(v, NumberStyles.Float, inv, out tmpSpeed);
                    break;
                case "turn": float.TryParse(v, NumberStyles.Float, inv, out spec.homingTurnRate); break;
                case "near": float.TryParse(v, NumberStyles.Float, inv, out spec.nearDistance); break;
                case "break": float.TryParse(v, NumberStyles.Float, inv, out spec.breakDistance); break;
                case "maxtime": float.TryParse(v, NumberStyles.Float, inv, out spec.maxHomingTime); break;
                case "life": float.TryParse(v, NumberStyles.Float, inv, out spec.life); break;
            }
        }

        // type �m���Ɋ����ƃN�����v
        spec.type = parsedType;
        spec.damage = Mathf.Max(0f, tmpDamage);
        spec.sizeMul = Mathf.Max(0.01f, tmpSize);

        // speed �̊����i�e�킲�Ɓj
        if (parsedType == EnemyBulletType.Homing)
        {
            if (tmpSpeed > 0f) spec.homingSpeed = tmpSpeed;
        }
        else if (parsedType == EnemyBulletType.Normal)
        {
            // 0�ȉ��́u���w��i�v���n�u����̂܂܁j�v�Ƃ���
            if (tmpSpeed > 0f) spec.normalSpeed = tmpSpeed;
        }
        // Beam �� speed ���g�p

        // �����̃N�����v
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
