// Assets/Scripts/UI/UIListNavigator.cs
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIListNavigator : MonoBehaviour
{
    [Header("対象UI（ボタン & スライダー）")]
    [Tooltip("未設定なら子階層から Button / Slider を自動収集（表示中のみ）")]
    public Selectable[] items;

    [Header("選択カーソル（プレハブ生成）")]
    [Tooltip("選択中アイテムの横に置く矢印プレハブ（Image/TMP/アニメ自由）")]
    public GameObject cursorPrefab;
    [Tooltip("矢印をボタンのどこに置くか（ボタンのRect基準のローカル座標）")]
    public Vector2 cursorOffset = new Vector2(-40f, 0f);
    [Tooltip("プレハブ生成後、常に同じインスタンスを再利用（推奨）")]
    public bool reuseCursorInstance = true;

    [Header("移動挙動")]
    public int startIndex = 0;
    public bool wrap = true;
    public bool onlyInteractable = true;
    public bool autoCollectOnEnable = true;

    [Header("スライダー操作")]
    [Range(0.001f, 1f)] public float stepPerTap = 0.05f;   // 相対（min..max）
    [Range(0f, 5f)] public float holdUnitsPerSec = 0.6f;

    // 内部
    int current = -1;
    Selectable[] liveItems = System.Array.Empty<Selectable>();

    // 生成済みカーソル
    RectTransform cursorRT;   // 実体（生成後）
    GameObject cursorGO;      // インスタンス参照

    void OnEnable()
    {
        RebuildList();
        SelectInitial();

        // EventSystem の選択も合わせる
        if (EventSystem.current && current >= 0 && current < liveItems.Length)
            EventSystem.current.SetSelectedGameObject(liveItems[current].gameObject);

        // 有効化時はカーソルも表示位置を同期
        UpdateCursor();
    }

    void OnDisable()
    {
        // 再利用しない場合は毎回破棄
        if (!reuseCursorInstance) DestroyCursorInstance();
    }

    void OnDestroy()
    {
        DestroyCursorInstance();
    }

    void Update()
    {
        if (liveItems.Length == 0) return;

        // 上下移動
        if (GetDown(KeyCode.W) || GetDown(KeyCode.UpArrow)) Move(-1);
        else if (GetDown(KeyCode.S) || GetDown(KeyCode.DownArrow)) Move(+1);

        // 決定（ボタンのみ）
        if (GetDown(KeyCode.Return) || GetDown(KeyCode.KeypadEnter) || GetDown(KeyCode.Space))
            PressIfButton();

        // スライダー調整（選択中がSliderの時のみ）
        AdjustIfSlider();
    }

    // ========= 操作 =========
    void Move(int delta)
    {
        if (liveItems.Length == 0) return;

        int next = current;
        for (int i = 0; i < liveItems.Length; i++)
        {
            next += delta;

            if (wrap)
            {
                if (next < 0) next = liveItems.Length - 1;
                if (next >= liveItems.Length) next = 0;
            }
            else
            {
                next = Mathf.Clamp(next, 0, liveItems.Length - 1);
            }

            if (!onlyInteractable) break;

            var sel = liveItems[next];
            if (sel && sel.IsActive() && sel.IsInteractable())
                break;
        }
        Select(next);
    }

    void PressIfButton()
    {
        if (!ValidIndex(current)) return;
        var b = liveItems[current] as Button;
        if (b && b.IsActive() && b.interactable) b.onClick?.Invoke();
    }

    void AdjustIfSlider()
    {
        if (!ValidIndex(current)) return;
        var s = liveItems[current] as Slider;
        if (!s || !s.IsActive() || !s.interactable) return;

        float range = s.maxValue - s.minValue;
        if (range <= 0f) return;

        float tapDelta = stepPerTap * range;
        float holdDelta = holdUnitsPerSec * range * Time.unscaledDeltaTime;

        float delta = 0f;

        // 左（減少）
        if (GetDown(KeyCode.A) || GetDown(KeyCode.LeftArrow)) delta -= tapDelta;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) delta -= holdDelta;

        // 右（増加）
        if (GetDown(KeyCode.D) || GetDown(KeyCode.RightArrow)) delta += tapDelta;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) delta += holdDelta;

        if (Mathf.Abs(delta) > 0f)
        {
            if (s.wholeNumbers)
            {
                float v = Mathf.Round(s.value + Mathf.Sign(delta) * 1f);
                s.value = Mathf.Clamp(v, s.minValue, s.maxValue);
            }
            else
            {
                s.value = Mathf.Clamp(s.value + delta, s.minValue, s.maxValue);
            }

            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }

    // ========= 選択 =========
    void SelectInitial()
    {
        if (liveItems.Length == 0) { SetCursorVisible(false); return; }

        int idx = Mathf.Clamp(startIndex, 0, liveItems.Length - 1);

        if (onlyInteractable)
        {
            int safe = idx;
            if (!(liveItems[safe] && liveItems[safe].IsActive() && liveItems[safe].IsInteractable()))
            {
                safe = System.Array.FindIndex(liveItems, x => x && x.IsActive() && x.IsInteractable());
                if (safe < 0) safe = idx;
            }
            idx = safe;
        }
        Select(idx);
    }

    void Select(int idx)
    {
        if (liveItems.Length == 0) { current = -1; SetCursorVisible(false); return; }
        idx = Mathf.Clamp(idx, 0, liveItems.Length - 1);
        current = idx;

        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(liveItems[current].gameObject);

        UpdateCursor();
    }

    // ========= カーソル生成/配置 =========
    void EnsureCursorInstance()
    {
        if (cursorRT && cursorGO) return;
        if (!cursorPrefab)
        {
            Debug.LogWarning("[UIListNavigator] cursorPrefab が未設定です。矢印を表示しません。");
            return;
        }
        cursorGO = Instantiate(cursorPrefab, transform); // 自分配下に一旦生成
        cursorRT = cursorGO.GetComponent<RectTransform>();
        if (!cursorRT)
        {
            Debug.LogWarning("[UIListNavigator] cursorPrefab に RectTransform がありません。");
        }
    }

    void UpdateCursor()
    {
        if (!ValidIndex(current)) { SetCursorVisible(false); return; }

        var target = liveItems[current].transform as RectTransform;
        if (!target) { SetCursorVisible(false); return; }

        EnsureCursorInstance();
        if (!cursorRT) { SetCursorVisible(false); return; }

        // 選択アイテムの子に付け替え（同一Canvas前提）
        cursorRT.SetParent(target, worldPositionStays: false);
        cursorRT.anchorMin = new Vector2(0f, 0.5f);
        cursorRT.anchorMax = new Vector2(0f, 0.5f);
        cursorRT.pivot = new Vector2(1f, 0.5f);
        cursorRT.anchoredPosition = cursorOffset;
        cursorRT.SetAsLastSibling();
        SetCursorVisible(true);
    }

    void SetCursorVisible(bool v)
    {
        if (cursorGO && cursorGO.activeSelf != v) cursorGO.SetActive(v);
    }

    void DestroyCursorInstance()
    {
        if (cursorGO)
        {
            Destroy(cursorGO);
            cursorGO = null;
            cursorRT = null;
        }
    }

    // ========= リスト構築 =========
    public void RebuildList()
    {
        if (autoCollectOnEnable || items == null || items.Length == 0)
        {
            // 子から Button / Slider を拾って並び順維持（Hierarchy順）
            items = GetComponentsInChildren<Selectable>(includeInactive: true)
                    .Where(s => s is Button || s is Slider)
                    .ToArray();
        }

        liveItems = items
            .Where(s => s != null && s.gameObject.activeInHierarchy)
            .ToArray();
    }

    // ========= Util =========
    bool GetDown(KeyCode k) => Input.GetKeyDown(k);
    bool ValidIndex(int i) => i >= 0 && i < liveItems.Length;
}
