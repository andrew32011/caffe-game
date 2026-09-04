/// <summary>
/// Батч 15: постоянный виджет цели обустройства на HUD («Копим на: X — A/B») + панель проекта.
/// Это ГЛАВНЫЙ «грид денег»: игрок всегда видит, на что копит монеты, и одним тапом
/// обустраивает кофейню. Строит свой Canvas в КОДЕ. Виджет — в левой колонке под кассой/
/// кристаллами (не пересекает правый док). По завершении проекта — RewardPopup + видимая
/// мебель (RenovationVisualizer) + сюжетный бит.
/// Сцена: MainScene (рантайм). Зависимости: RenovationManager, GameManager, RenovationVisualizer,
/// RewardPopupUI, Loc, TMPro. SDK: нет.
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RenovationGoalUI : MonoBehaviour
{
    public static RenovationGoalUI Instance { get; private set; }

    private TMP_FontAsset _font;
    private bool _built;

    // Виджет
    private GameObject _widget;
    private TextMeshProUGUI _widgetLabel;
    private Image _widgetFill;
    private int _shownCoins = -1, _shownStage = -1;

    // Панель
    private GameObject _panel, _dim;
    private TextMeshProUGUI _projTitle, _projStory, _projBenefit, _buyLabel, _gemLabel;
    private Button _buyBtn, _gemBtn;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        Build();
    }

    public static RenovationGoalUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("RenovationGoal");
            Instance = go.AddComponent<RenovationGoalUI>();
        }
        Instance.EnsureBuilt();
        return Instance;
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || _widget == null) return;
        // Перф: обновляем виджет только при изменении монет/стадии (как CoinsUI).
        int coins = gm.TotalCoins, stage = RenovationManager.Stage;
        if (coins == _shownCoins && stage == _shownStage) return;
        _shownCoins = coins; _shownStage = stage;
        RefreshWidget();
    }

    private void RefreshWidget()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (RenovationManager.AllDone)
        {
            if (_widgetLabel != null) _widgetLabel.text = Loc.T("Кофейня обустроена!", "Café fully done!");
            if (_widgetFill != null) _widgetFill.fillAmount = 1f;
            return;
        }
        var p = RenovationManager.Current;
        int cost = RenovationManager.CurrentCost;
        if (_widgetFill != null) _widgetFill.fillAmount = Mathf.Clamp01(gm.TotalCoins / (float)Mathf.Max(1, cost));
        bool ready = gm.TotalCoins >= cost;
        string name = Loc.IsRu ? p.Ru : p.En;
        if (_widgetLabel != null)
            _widgetLabel.text = (ready ? Loc.T("Готово! ", "Ready! ") : Loc.T("Копим: ", "Saving: "))
                              + name + $"  {Mathf.Min(gm.TotalCoins, cost)}/{cost}";
    }

    // ─── Панель проекта ─────────────────────────────────────────────────────────
    public void OpenPanel()
    {
        if (_panel == null) return;
        RefreshPanel();
        if (_dim != null) { _dim.SetActive(true); _dim.transform.SetAsLastSibling(); }
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void ClosePanel()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
        if (_dim != null) _dim.SetActive(false);
    }

    private void RefreshPanel()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        bool done = RenovationManager.AllDone;
        var p = RenovationManager.Current;
        int cost = RenovationManager.CurrentCost;

        if (_projTitle != null) _projTitle.text = done
            ? Loc.T("Кофейня обустроена", "Café fully renovated")
            : (Loc.IsRu ? p.Ru : p.En);
        if (_projStory != null) _projStory.text = done
            ? Loc.T("Ты вернула «Междумирью» тепло. Дом ждёт.", "You brought warmth back to the Inbetween. Home awaits.")
            : (Loc.IsRu ? p.StoryRu : p.StoryEn);
        if (_projBenefit != null) _projBenefit.text = done ? "" : BenefitLine(p);

        if (_buyBtn != null) _buyBtn.gameObject.SetActive(!done);
        if (_buyBtn != null) _buyBtn.interactable = RenovationManager.CanAfford();
        if (_buyLabel != null) _buyLabel.text = Loc.T($"Обустроить — {cost}", $"Renovate — {cost}");

        bool canGem = !done && gm.Gems >= RenovationManager.GemInstantCost;
        if (_gemBtn != null) _gemBtn.gameObject.SetActive(!done);
        if (_gemBtn != null) _gemBtn.interactable = canGem;
        if (_gemLabel != null) _gemLabel.text = Loc.T($"Ускорить — {RenovationManager.GemInstantCost} крист.",
                                                      $"Speed up — {RenovationManager.GemInstantCost} gems");
    }

    private static string BenefitLine(RenovationManager.Project p)
    {
        int pct = Mathf.RoundToInt(p.Value * 100f);
        switch (p.Benefit)
        {
            case RenovationManager.Benefit.Price:     return Loc.T($"Выгода: +{pct}% к оплате", $"Perk: +{pct}% payment");
            case RenovationManager.Benefit.Tip:       return Loc.T($"Выгода: +{pct}% к чаевым", $"Perk: +{pct}% tips");
            default:                                  return Loc.T($"Выгода: точнее кофемашина", $"Perk: steadier machine");
        }
    }

    private void OnBuy()
    {
        if (RenovationManager.Complete(out var done)) OnCompleted(done);
        else AudioController.Instance?.PlayWrongOrder();
        RefreshPanel();
    }

    private void OnGem()
    {
        if (RenovationManager.CompleteWithGems(out var done)) OnCompleted(done);
        else AudioController.Instance?.PlayWrongOrder();
        RefreshPanel();
    }

    private void OnCompleted(RenovationManager.Project done)
    {
        AudioController.Instance?.PlayBonus();
        int newStage = RenovationManager.Stage;         // уже инкрементнут
        RenovationVisualizer.Instance?.ShowStage(newStage - 1); // индекс завершённой стадии
        RewardPopupUI.Ensure().Show(
            Loc.T("Кофейня преобразилась!", "The café transformed!"),
            (Loc.IsRu ? done.Ru : done.En) + "\n" + (Loc.IsRu ? done.StoryRu : done.StoryEn),
            new Color(0.95f, 0.8f, 0.35f), 4f);
        _shownCoins = -1; // форс-обновление виджета
    }

    // ─── Построение ─────────────────────────────────────────────────────────────
    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("RenovationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 215;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuildWidget(canvasGo.transform);
        BuildPanel(canvasGo.transform);
        RefreshWidget();
    }

    private void BuildWidget(Transform parent)
    {
        // Левая колонка, под кассой (0.9) и кристаллами (0.835) → цель на 0.745–0.815.
        _widget = new GameObject("GoalWidget", typeof(RectTransform), typeof(Image), typeof(Button));
        _widget.transform.SetParent(parent, false);
        var rt = (RectTransform)_widget.transform;
        rt.anchorMin = new Vector2(0.02f, 0.745f);
        rt.anchorMax = new Vector2(0.36f, 0.815f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _widget.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.16f, 0.9f);
        _widget.GetComponent<Button>().onClick.AddListener(OpenPanel);

        _widgetLabel = MakeText("Label", _widget.transform, new Vector2(0.04f, 0.42f), new Vector2(0.97f, 0.98f), 18,
            "", new Color(1f, 0.92f, 0.7f), FontStyles.Normal, TextAlignmentOptions.Left);

        var barBg = new GameObject("BarBG", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(_widget.transform, false);
        var brt = (RectTransform)barBg.transform;
        brt.anchorMin = new Vector2(0.04f, 0.10f); brt.anchorMax = new Vector2(0.97f, 0.36f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        barBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        barBg.GetComponent<Image>().raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barBg.transform, false);
        var frt = (RectTransform)fillGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        _widgetFill = fillGo.GetComponent<Image>();
        _widgetFill.color = new Color(0.95f, 0.8f, 0.35f);
        _widgetFill.sprite = WhiteSprite();
        _widgetFill.type = Image.Type.Filled;
        _widgetFill.fillMethod = Image.FillMethod.Horizontal;
        _widgetFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _widgetFill.fillAmount = 0f;
        _widgetFill.raycastTarget = false;
    }

    private void BuildPanel(Transform parent)
    {
        _dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        _dim.transform.SetParent(parent, false);
        var drt = (RectTransform)_dim.transform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
        _dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        _dim.GetComponent<Button>().onClick.AddListener(ClosePanel);
        _dim.SetActive(false);

        _panel = new GameObject("RenoPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(parent, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.28f, 0.24f); prt.anchorMax = new Vector2(0.72f, 0.80f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.13f, 0.98f);

        MakeText("Hdr", _panel.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), 26,
            Loc.T("Обустройство кофейни", "Café renovation"), new Color(0.95f, 0.8f, 0.4f), FontStyles.Bold, TextAlignmentOptions.Center);
        _projTitle   = MakeText("Title", _panel.transform, new Vector2(0.06f, 0.74f), new Vector2(0.94f, 0.86f), 28, "", Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        _projStory   = MakeText("Story", _panel.transform, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.72f), 20, "", new Color(0.88f, 0.9f, 1f), FontStyles.Italic, TextAlignmentOptions.Center);
        _projBenefit = MakeText("Benefit", _panel.transform, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.47f), 20, "", new Color(0.6f, 0.95f, 0.65f), FontStyles.Normal, TextAlignmentOptions.Center);

        _buyLabel = MakeButton("BuyBtn", _panel.transform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.34f), new Color(0.22f, 0.45f, 0.28f, 1f), OnBuy);
        _buyBtn = _buyLabel.transform.parent.GetComponent<Button>();
        _gemLabel = MakeButton("GemBtn", _panel.transform, new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.21f), new Color(0.22f, 0.38f, 0.5f, 1f), OnGem);
        _gemBtn = _gemLabel.transform.parent.GetComponent<Button>();

        var closeLabel = MakeButton("CloseBtn", _panel.transform, new Vector2(0.36f, 0.005f), new Vector2(0.64f, 0.08f), new Color(0.28f, 0.24f, 0.30f, 1f), ClosePanel);
        closeLabel.text = Loc.T("Закрыть", "Close");

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size,
        string content, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false; t.enableWordWrapping = true; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 22, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    private static Sprite _white;
    private static Sprite WhiteSprite()
    {
        if (_white != null) return _white;
        var tex = new Texture2D(4, 4);
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }
}
