/// <summary>
/// Батч 16: магазин-обустройство камерой. Заменяет плашку «Копим». На HUD справа — кнопка
/// «Магазин» с восклицательным значком, когда хватает на следующую покупку. По нажатию камера
/// облетает точки кофейни (позиции RenoStages): стрелки листают точки, на каждой мигает будущий
/// предмет и показана его цена; покупка ставит предмет (RenovationManager.Complete) + бонус/сюжет.
/// Стиль Mini UI (UiKit). Сцена: MainScene (рантайм).
/// Зависимости: RenovationManager, RenovationVisualizer, GameManager, UiKit/UiSkin, RewardPopupUI, Loc.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RenovationShopUI : MonoBehaviour
{
    public static RenovationShopUI Instance { get; private set; }

    private bool _built;
    private GameObject _hudBtn, _badge, _shopRoot;
    private TextMeshProUGUI _topLabel, _name, _benefit, _buyLbl, _gemLbl, _hint;
    private Button _buyBtn, _gemBtn, _prevBtn, _nextBtn;
    private int _view;
    private bool _open;
    private float _badgeTimer;
    private bool _shownBadge;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureBuilt();
    }

    private void EnsureBuilt() { if (_built) return; _built = true; Build(); }

    public static RenovationShopUI Ensure()
    {
        if (Instance == null) Instance = new GameObject("RenovationShop").AddComponent<RenovationShopUI>();
        Instance.EnsureBuilt();
        return Instance;
    }

    private int Count => RenovationManager.Projects.Length;

    private void Update()
    {
        // Бейдж «!» — когда хватает на следующую покупку (троттлинг).
        _badgeTimer -= Time.unscaledDeltaTime;
        if (_badgeTimer > 0f) return;
        _badgeTimer = 0.4f;
        bool show = !RenovationManager.AllDone && RenovationManager.CanAfford();
        if (show != _shownBadge)
        {
            _shownBadge = show;
            if (_badge != null) _badge.SetActive(show);
        }
    }

    // ─── Открытие/закрытие ──────────────────────────────────────────────────────
    public void Open()
    {
        if (!_built) EnsureBuilt();
        if (_open) return;
        _open = true;
        GameInput.Locked = true;
        RenovationVisualizer.Instance?.EnterShopCamera();
        _view = Mathf.Clamp(RenovationManager.Stage, 0, Mathf.Max(0, Count - 1));
        if (_shopRoot != null) { _shopRoot.SetActive(true); _shopRoot.transform.SetAsLastSibling(); }
        AudioController.Instance?.PlayUiOpen();
        Analytics.Send("shop_open");
        Refresh();
    }

    public void Close()
    {
        if (!_open) return;
        _open = false;
        AudioController.Instance?.PlayUiClose();
        RenovationVisualizer.Instance?.ExitShopCamera();
        if (_shopRoot != null) _shopRoot.SetActive(false);
        GameInput.Locked = false;
    }

    private void Prev() { _view = (_view - 1 + Count) % Count; Refresh(); }
    private void Next() { _view = (_view + 1) % Count; Refresh(); }

    private void Refresh()
    {
        var vis = RenovationVisualizer.Instance;
        vis?.FrameStage(_view);

        int stage = RenovationManager.Stage;
        bool built = _view < stage;
        bool current = _view == stage;              // следующая к постройке
        var p = RenovationManager.Projects[_view];

        // Превью-мигание только для не построенных стадий.
        if (built) vis?.StopPreview(); else vis?.PreviewStage(_view);

        if (_topLabel != null)
            _topLabel.text = Loc.T($"Обустройство · {_view + 1}/{Count}", $"Renovation · {_view + 1}/{Count}");
        if (_name != null) _name.text = Loc.IsRu ? p.Ru : p.En;
        if (_benefit != null)
            _benefit.text = built
                ? Loc.T("Уже обустроено", "Already done")
                : (Loc.IsRu ? p.StoryRu : p.StoryEn) + "\n" + BenefitLine(p);

        var gm = GameManager.Instance;
        bool canBuy = current && gm != null && gm.TotalCoins >= p.Cost;
        bool canGem = current && gm != null && gm.Gems >= RenovationManager.GemInstantCost;

        if (_buyBtn != null) { _buyBtn.gameObject.SetActive(!built); _buyBtn.interactable = canBuy; }
        if (_gemBtn != null) { _gemBtn.gameObject.SetActive(!built); _gemBtn.interactable = canGem; }
        if (_buyLbl != null) _buyLbl.text = Loc.T($"Обустроить — {p.Cost}", $"Renovate — {p.Cost}");
        if (_gemLbl != null) _gemLbl.text = Loc.T($"Ускорить — {RenovationManager.GemInstantCost} крист.",
                                                  $"Speed up — {RenovationManager.GemInstantCost} gems");
        if (_hint != null)
            _hint.text = built ? Loc.T("Готово", "Done")
                : current ? "" : Loc.T("Сначала заверши предыдущие", "Finish the earlier ones first");
    }

    private static string BenefitLine(RenovationManager.Project p)
    {
        int pct = Mathf.RoundToInt(p.Value * 100f);
        switch (p.Benefit)
        {
            case RenovationManager.Benefit.Price: return Loc.T($"Выгода: +{pct}% к оплате", $"Perk: +{pct}% payment");
            case RenovationManager.Benefit.Tip:   return Loc.T($"Выгода: +{pct}% к чаевым", $"Perk: +{pct}% tips");
            default:                              return Loc.T("Выгода: точнее кофемашина", "Perk: steadier machine");
        }
    }

    private void OnBuy()
    {
        if (RenovationManager.Complete(out var done)) OnCompleted(done);
        else AudioController.Instance?.PlayWrongOrder();
    }

    private void OnGem()
    {
        if (RenovationManager.CompleteWithGems(out var done)) OnCompleted(done);
        else AudioController.Instance?.PlayWrongOrder();
    }

    private void OnCompleted(RenovationManager.Project done)
    {
        AudioController.Instance?.PlayBonus();
        int newStage = RenovationManager.Stage;               // уже инкрементнут
        RenovationVisualizer.Instance?.StopPreview();
        RenovationVisualizer.Instance?.ShowStage(newStage - 1); // ставим предмет солидно
        RewardPopupUI.Ensure().Show(
            Loc.T("Кофейня преобразилась!", "The café transformed!"),
            (Loc.IsRu ? done.Ru : done.En) + "\n" + (Loc.IsRu ? done.StoryRu : done.StoryEn),
            new Color(0.95f, 0.8f, 0.35f), 3.5f);
        Analytics.Send("reno_buy_point", "stage", newStage.ToString());
        _view = Mathf.Clamp(newStage, 0, Mathf.Max(0, Count - 1)); // показываем следующую цель
        _badgeTimer = 0f;
        Refresh();
    }

    // ─── Построение ─────────────────────────────────────────────────────────────
    private void Build()
    {
        // HUD-кнопка (правая колонка), низкий порядок — как прочие входы HUD.
        UiKit.Canvas(transform, 218, "RenoHudCanvas");
        var hudT = transform.GetChild(transform.childCount - 1);

        BuildHud(hudT);

        // Оверлей магазина (высокий порядок), НЕ затемняем зал — видно мебель.
        UiKit.Canvas(transform, 321, "RenoShopCanvas");
        var shopT = transform.GetChild(transform.childCount - 1);

        _shopRoot = new GameObject("ShopRoot", typeof(RectTransform));
        _shopRoot.transform.SetParent(shopT, false);
        UiKit.Fill((RectTransform)_shopRoot.transform);

        _topLabel = UiKit.Text(_shopRoot.transform, new Vector2(0.2f, 0.9f), new Vector2(0.8f, 0.98f),
            "", 26, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.6f), FontStyles.Bold, "Top");

        // Стрелки (ASCII — безопасны в шрифте).
        _prevBtn = UiKit.Button(_shopRoot.transform, new Vector2(0.015f, 0.42f), new Vector2(0.085f, 0.58f), "<", Prev, false, 40, "Prev");
        _nextBtn = UiKit.Button(_shopRoot.transform, new Vector2(0.915f, 0.42f), new Vector2(0.985f, 0.58f), ">", Next, false, 40, "Next");

        // Нижняя панель управления.
        var bar = UiKit.Panel(_shopRoot.transform, new Vector2(0.14f, 0.02f), new Vector2(0.86f, 0.26f), false, "Bar");
        _name = UiKit.Text(bar.transform, new Vector2(0.03f, 0.66f), new Vector2(0.97f, 0.96f),
            "", 26, TextAlignmentOptions.Center, Color.white, FontStyles.Bold, "Name");
        _benefit = UiKit.Text(bar.transform, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.64f),
            "", 18, TextAlignmentOptions.Center, new Color(0.85f, 0.92f, 1f), FontStyles.Italic, "Benefit");
        _hint = UiKit.Text(bar.transform, new Vector2(0.03f, 0.30f), new Vector2(0.97f, 0.36f),
            "", 16, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.6f), FontStyles.Normal, "Hint");

        _buyBtn = UiKit.Button(bar.transform, new Vector2(0.05f, 0.05f), new Vector2(0.49f, 0.29f), "", OnBuy, true, 22, "Buy");
        _buyLbl = UiKit.Label(_buyBtn);
        _gemBtn = UiKit.Button(bar.transform, new Vector2(0.51f, 0.05f), new Vector2(0.95f, 0.29f), "", OnGem, false, 22, "Gem");
        _gemLbl = UiKit.Label(_gemBtn);

        UiKit.Button(_shopRoot.transform, new Vector2(0.90f, 0.90f), new Vector2(0.99f, 0.985f),
            Loc.T("Закрыть", "Close"), Close, false, 18, "Close");

        _shopRoot.SetActive(false);
    }

    private void BuildHud(Transform hudT)
    {
        var btn = UiKit.Button(hudT, new Vector2(0.855f, 0.70f), new Vector2(0.995f, 0.79f),
            Loc.T("Магазин", "Shop"), Open, true, 20, "ShopBtn");
        _hudBtn = btn.gameObject;

        _badge = UiKit.Badge(_hudBtn.transform, new Vector2(0.82f, 0.6f), new Vector2(1.05f, 1.15f)).gameObject;
        UiKit.Text(_badge.transform, Vector2.zero, Vector2.one, "!", 22, TextAlignmentOptions.Center, Color.white, FontStyles.Bold, "N");
        _badge.SetActive(false);
    }
}
