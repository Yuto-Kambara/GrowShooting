using UnityEngine;
using System.Collections.Generic;

public class AbsorbSystem : MonoBehaviour
{
    [Header("�z���p�����[�^")]
    public float pullRadius = 1.2f;       // ���q���o���͈�
    public GameObject energyPrefab;       // EnergyOrb �̃v���n�u
    public float energySpeed = 8f;        // ���q�̔�s���x

    [Header("�������[�g")]
    [Tooltip("�͈͓��ɂ���e1��������A���̊Ԋu���Ƃ�1������")]
    public float spawnInterval = 0.5f;

    [Tooltip("�e���͈͂ɓ������u�Ԃ�1�o�����itrue �Ȃ瑦1�{�Ȍ�� interval �Ԋu�j")]
    public bool spawnOnEnter = true;

    [Tooltip("�e�e����o���ő���i0 �Ȃ疳�����j")]
    public int maxPerBullet = 0;

    private GrowthSystem growth;
    private int bulletMask;

    // �e�R���|�[�l���g�iBulletEnergyFlag�j�� InstanceID ���L�[�ɁA���񐶐����������������Ǘ�
    private readonly Dictionary<int, float> _nextSpawnAt = new();
    private readonly Dictionary<int, int> _spawnCount = new();

    void Awake()
    {
        growth = GetComponentInParent<GrowthSystem>();
        bulletMask = 1 << LayerMask.NameToLayer("EnemyBullet");
    }

    void OnDisable()
    {
        _nextSpawnAt.Clear();
        _spawnCount.Clear();
    }

    //========= pullRadius ���̓G�e����A���Ԋu�� EnergyOrb ���o�� =========
    void Update()
    {
        // �߂��̓G�e�����m�i�e�͓������Ȃ��j
        var hits = Physics2D.OverlapCircleAll(transform.position, pullRadius, bulletMask);

        // ���e�W���i�����̑|���p�j
        var visibleIds = new HashSet<int>();

        float now = Time.time;
        foreach (var h in hits)
        {
            // �ȑO�� 1�����p�� BulletEnergyFlag.spawned ���g���Ă������A����́u���݂������o��������v�̂�
            // BulletEnergyFlag �̗L����������Ɏg���i���̃R���|�[�l���g���t���Ă���e�̂ݑΏہj
            var flag = h.GetComponent<BulletEnergyFlag>();
            if (!flag) continue;

            int id = flag.GetInstanceID();
            visibleIds.Add(id);

            // ������ɒB���Ă���Ȃ牽�����Ȃ�
            if (maxPerBullet > 0 && _spawnCount.TryGetValue(id, out int cnt) && cnt >= maxPerBullet)
                continue;

            // �܂��o�^����Ă��Ȃ��e�i�͈͂ɓ������΂���j
            if (!_nextSpawnAt.ContainsKey(id))
            {
                if (spawnOnEnter)
                {
                    SpawnEnergy(h.transform.position);
                    _spawnCount[id] = 1;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
                else
                {
                    _spawnCount[id] = 0;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
                continue;
            }

            // ���񎞍��ɒB���Ă���ΐ���
            if (now >= _nextSpawnAt[id])
            {
                // ����`�F�b�N�i���O�œ��B���Ă��Ȃ����Ƃ͊m�F�ς݂�����d�h��j
                int produced = _spawnCount.TryGetValue(id, out int c) ? c : 0;
                if (maxPerBullet == 0 || produced < maxPerBullet)
                {
                    SpawnEnergy(h.transform.position);
                    _spawnCount[id] = produced + 1;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
            }
        }

        // �����̑|���F�͈͊O�֏o��/�j�����ꂽ�e�̃G���g��������
        if (_nextSpawnAt.Count > 0)
        {
            _tmpKeys.Clear();
            foreach (var key in _nextSpawnAt.Keys)
                if (!visibleIds.Contains(key))
                    _tmpKeys.Add(key);
            foreach (var key in _tmpKeys)
            {
                _nextSpawnAt.Remove(key);
                _spawnCount.Remove(key);
            }
        }
    }

    // �����w���p
    void SpawnEnergy(Vector3 at)
    {
        if (!energyPrefab || !growth) return;
        var orb = Instantiate(energyPrefab, at, Quaternion.identity);
        var e = orb.GetComponent<EnergyOrb>();
        if (e) e.Init(transform.root, energySpeed, growth);
    }

    // �g���̂ăo�b�t�@�iGC�팸�j
    static readonly List<int> _tmpKeys = new();

#if UNITY_EDITOR
    // ���a����
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
#endif
}
