using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// シミュレーション環境内の8つのライト（グループ）をUI上でON/OFF操作するコントローラー。
/// 各スロットには単体 Light でも複数 Light を持つ親オブジェクトでも登録可能。
/// Canvasとトグルパネルを実行時に自動生成するため、UIプレハブは不要。
/// </summary>
public class LightController : MonoBehaviour
{
    [Header("ライト設定")]
    [Tooltip("操作対象のライトまたはライトグループの親 GameObject を8つ登録。\n" +
             "子オブジェクトの Light コンポーネントも含めて自動収集します。")]
    [SerializeField] private GameObject[] lightObjects = new GameObject[8];

    // 実行時に収集した各グループのLightリスト
    private Light[][] _lightGroups;

    [Header("UIパネル設定")]
    [Tooltip("パネルを展開/折りたたみするキー")]
    [SerializeField] private KeyCode togglePanelKey = KeyCode.L;
    [Tooltip("パネルの横幅（ピクセル）")]
    [SerializeField] private float panelWidth = 220f;
    [Tooltip("UIパネルの背景透明度 (0〜1)")]
    [SerializeField] [Range(0f, 1f)] private float panelAlpha = 0.85f;

    // ---- 内部状態 ----
    private Canvas _canvas;
    private GameObject _panel;
    private Toggle[] _toggles;
    private bool _isPanelVisible = true;

    // ---- UIカラー定数 ----
    private static readonly Color ColorBg        = new Color(0.10f, 0.10f, 0.12f, 1f);
    private static readonly Color ColorHeader     = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color ColorRowEven    = new Color(0.14f, 0.14f, 0.17f, 1f);
    private static readonly Color ColorRowOdd     = new Color(0.12f, 0.12f, 0.15f, 1f);
    private static readonly Color ColorToggleOn   = new Color(0.30f, 0.75f, 0.40f, 1f);
    private static readonly Color ColorToggleOff  = new Color(0.55f, 0.55f, 0.58f, 1f);
    private static readonly Color ColorText       = new Color(0.90f, 0.90f, 0.90f, 1f);
    private static readonly Color ColorHeaderText = new Color(1.00f, 0.92f, 0.50f, 1f);

    // ---- 定数 ----
    private const float RowHeight   = 36f;
    private const float HeaderHeight = 44f;
    private const float FooterHeight = 34f;
    private const float Padding      = 8f;

    // =========================================================
    // ライフサイクル
    // =========================================================

    void Start()
    {
        ResolveLightGroups();
        BuildUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(togglePanelKey))
            SetPanelVisible(!_isPanelVisible);
    }

    // =========================================================
    // ライトグループの収集
    // =========================================================

    /// <summary>
    /// Inspector でアサインされた GameObject ごとに、
    /// 自身と子オブジェクト全体から Light コンポーネントを収集してグループ化する。
    /// 非アクティブな Light も対象にするため includeInactive = true を渡す。
    /// </summary>
    private void ResolveLightGroups()
    {
        if (lightObjects == null || lightObjects.Length == 0)
        {
            Debug.LogWarning("[LightController] lightObjectsが未設定です。Inspectorで GameObject をアサインしてください。");
            lightObjects = new GameObject[8];
        }
        else if (lightObjects.Length != 8)
        {
            Debug.LogWarning($"[LightController] lightObjects配列の要素数が{lightObjects.Length}です。8つになるよう設定してください。");
            System.Array.Resize(ref lightObjects, 8);
        }

        _lightGroups = new Light[8][];
        for (int i = 0; i < 8; i++)
        {
            if (lightObjects[i] == null)
            {
                _lightGroups[i] = new Light[0];
                continue;
            }

            // 自身 + 全子孫から Light を収集（非アクティブ含む）
            _lightGroups[i] = lightObjects[i].GetComponentsInChildren<Light>(includeInactive: true);

            if (_lightGroups[i].Length == 0)
                Debug.LogWarning($"[LightController] スロット{i + 1} '{lightObjects[i].name}' に Light コンポーネントが見つかりません。");
            else if (_lightGroups[i].Length > 1)
                Debug.Log($"[LightController] スロット{i + 1} '{lightObjects[i].name}' : {_lightGroups[i].Length} 個の Light を検出しました。");
        }
    }

    /// <summary>グループ内のいずれかの Light が enabled であれば true を返す。</summary>
    private bool IsGroupOn(int index)
    {
        foreach (var light in _lightGroups[index])
            if (light != null && light.enabled) return true;
        return false;
    }

    /// <summary>グループ内の全 Light の enabled 状態を一括設定する。</summary>
    private void SetGroupEnabled(int index, bool isOn)
    {
        foreach (var light in _lightGroups[index])
            if (light != null) light.enabled = isOn;
    }

    // =========================================================
    // UI構築
    // =========================================================

    private void BuildUI()
    {
        // --- Canvas ---
        var canvasGo = new GameObject("LightControllerCanvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // --- パネル本体 ---
        float totalHeight = HeaderHeight + RowHeight * 8 + FooterHeight + Padding * 2;
        _panel = CreateRect("LightPanel", canvasGo.transform,
            new Vector2(panelWidth, totalHeight),
            new Vector2(1f, 1f),
            new Vector2(-10f, -10f));
        AddImage(_panel, ColorBg, panelAlpha);

        // --- ヘッダー ---
        var header = CreateRect("Header", _panel.transform,
            new Vector2(panelWidth, HeaderHeight),
            pivot: new Vector2(0.5f, 1f),
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(0f, -HeaderHeight),
            offsetMax: new Vector2(0f, 0f));
        AddImage(header, ColorHeader, panelAlpha);

        var headerText = AddText(header.transform, "💡 ライト操作パネル", 14, ColorHeaderText, TextAnchor.MiddleCenter);
        FillRect(headerText.gameObject);

        // --- 全ON / 全OFFボタン（フッター） ---
        var footer = CreateRect("Footer", _panel.transform,
            new Vector2(panelWidth, FooterHeight),
            pivot: new Vector2(0.5f, 0f),
            anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(1f, 0f),
            offsetMin: new Vector2(0f, 0f),
            offsetMax: new Vector2(0f, FooterHeight));
        AddImage(footer, ColorHeader, panelAlpha);

        BuildFooterButtons(footer.transform);

        // --- ライト行 ---
        _toggles = new Toggle[8];
        for (int i = 0; i < 8; i++)
            BuildLightRow(i, _lightGroups[i].Length);

        // --- ヒントラベル ---
        var hint = AddText(_panel.transform,
            $"[{togglePanelKey}] でパネルを切替", 9,
            new Color(0.6f, 0.6f, 0.6f, 1f), TextAnchor.UpperRight);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.offsetMin = new Vector2(4f, -(totalHeight));
        hintRt.offsetMax = new Vector2(-4f, -(totalHeight - 14f));
    }

    private void BuildLightRow(int index, int lightCount)
    {
        float yOffset = HeaderHeight + RowHeight * index;
        var row = CreateRect($"Row_{index}", _panel.transform,
            new Vector2(panelWidth, RowHeight),
            pivot: new Vector2(0.5f, 1f),
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(0f, -(yOffset + RowHeight)),
            offsetMax: new Vector2(0f, -yOffset));
        AddImage(row, index % 2 == 0 ? ColorRowEven : ColorRowOdd, panelAlpha);

        // ライト番号ラベル（グループの場合は灯数を表示）
        bool hasObject = lightObjects[index] != null;
        string baseName = hasObject ? lightObjects[index].name : $"ライト {index + 1}（未設定）";
        string countSuffix = lightCount > 1 ? $" ({lightCount}灯)" : (lightCount == 0 && hasObject ? " (Light無)" : "");
        var label = AddText(row.transform, $"  {index + 1}. {baseName}{countSuffix}", 11, ColorText, TextAnchor.MiddleLeft);
        var labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0.72f, 1f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        // トグルスイッチ（グループ内にONのものがあればON状態で初期化）
        bool initialState = IsGroupOn(index);
        var toggleGo = BuildToggleSwitch(row.transform, index, initialState);
        var toggleRt = toggleGo.GetComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(0.72f, 0.15f);
        toggleRt.anchorMax = new Vector2(0.97f, 0.85f);
        toggleRt.offsetMin = Vector2.zero;
        toggleRt.offsetMax = Vector2.zero;
    }

    private GameObject BuildToggleSwitch(Transform parent, int lightIndex, bool isOn)
    {
        var go = new GameObject($"Toggle_{lightIndex}");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        // 背景画像
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = isOn ? ColorToggleOn : ColorToggleOff;

        // チェックマーク（ラベル代用）
        var checkGo = new GameObject("Label");
        checkGo.transform.SetParent(bg.transform, false);
        var checkText = checkGo.AddComponent<Text>();
        checkText.text = isOn ? "ON" : "OFF";
        checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        checkText.fontSize = 11;
        checkText.fontStyle = FontStyle.Bold;
        checkText.color = Color.white;
        checkText.alignment = TextAnchor.MiddleCenter;
        FillRect(checkGo);

        // Toggle コンポーネント
        var toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = null;
        toggle.isOn = isOn;

        int idx = lightIndex;
        toggle.onValueChanged.AddListener((value) => OnToggleChanged(idx, value, bgImg, checkText));

        _toggles[lightIndex] = toggle;
        return go;
    }

    private void BuildFooterButtons(Transform parent)
    {
        BuildSimpleButton(parent, "全ON", 0f, 0.5f, () => SetAllLights(true));
        BuildSimpleButton(parent, "全OFF", 0.5f, 1f, () => SetAllLights(false));
    }

    private void BuildSimpleButton(Transform parent, string label, float xMin, float xMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, 0.1f);
        rt.anchorMax = new Vector2(xMax, 0.9f);
        rt.offsetMin = new Vector2(xMin == 0f ? 6f : 3f, 0f);
        rt.offsetMax = new Vector2(xMax == 1f ? -6f : -3f, 0f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.45f, 0.65f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.60f, 0.85f, 1f);
        colors.pressedColor = new Color(0.20f, 0.35f, 0.50f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var t = textGo.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 12;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        FillRect(textGo);
    }

    // =========================================================
    // ライト操作ロジック
    // =========================================================

    private void OnToggleChanged(int index, bool isOn, Image bgImage, Text label)
    {
        SetGroupEnabled(index, isOn);
        bgImage.color = isOn ? ColorToggleOn : ColorToggleOff;
        label.text = isOn ? "ON" : "OFF";
    }

    private void SetAllLights(bool isOn)
    {
        for (int i = 0; i < 8; i++)
        {
            SetGroupEnabled(i, isOn);
            if (_toggles != null && _toggles[i] != null)
                _toggles[i].isOn = isOn;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        _isPanelVisible = visible;
        if (_panel != null)
            _panel.SetActive(visible);
    }

    // =========================================================
    // UI ユーティリティ
    // =========================================================

    private static GameObject CreateRect(string name, Transform parent, Vector2 size,
        Vector2 anchorAndPivot, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = anchorAndPivot;
        rt.anchorMax = anchorAndPivot;
        rt.pivot = anchorAndPivot;
        rt.anchoredPosition = anchoredPos;
        return go;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 size,
        Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.pivot = pivot;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    private static Image AddImage(GameObject go, Color color, float alpha = 1f)
    {
        var img = go.AddComponent<Image>();
        color.a = alpha;
        img.color = color;
        return img;
    }

    private static Text AddText(Transform parent, string content, int fontSize,
        Color color, TextAnchor anchor)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = anchor;
        return t;
    }

    private static void FillRect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
