/// <summary>
/// Батч 15: хаб прогресса — единая кнопка «Прогресс» (левая колонка) + меню разделов:
/// Рецепты (мастерство), Альбом (коллекция гостей/рецептов), Событие (недельный турнир),
/// Сезон (пасс), Колесо (RewardWheelUI). Строит свой Canvas в КОДЕ; разделы перерисовываются
/// по клику. Бейдж — суммарно доступные награды (событие+сезон+колесо).
/// Сцена: MainScene (рантайм). Зависимости: RecipeBook/EventManager/SeasonPass/RewardWheelUI,
/// GameManager, Loc, TMPro. SDK: нет.
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressHubUI : MonoBehaviour
{
    public static ProgressHubUI Instance { get; private set; }

    private TMP_FontAsset _font;
    private bool _built;
    private GameObject _btn, _badge, _menu, _dim, _content;
    private TextMeshProUGUI _badgeText;
    private int _shownBadge = -1;
    private float _badgeTimer;

    private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; EnsureBuilt(); }
    private void EnsureBuilt() { if (_built) return; _built = true; Build(); }
    public static ProgressHubUI Ensure()
    {
        if (Instance == null) Instance = new GameObject("ProgressHub").AddComponent<ProgressHubUI>();
        Instance.EnsureBuilt();
        return Instance;
    }

    private int TotalClaimable()
    {
        int n = 0;
        if (EventManager.CanClaim()) n++;
        n += SeasonPass.Claimable();
        if (Wheel_Available()) n++;
        return n;
    }
    private static bool Wheel_Available() => YG.YG2.isSDKEnabled && YG.YG2.saves.wheelLastSpin != DateTime.Now.ToString("yyyyMMdd");

    private void Update()
    {
        if (_badge == null) return;
        _badgeTimer -= Time.unscaledDeltaTime;
        if (_badgeTimer > 0f) return;
        _badgeTimer = 0.5f; // перф: проверяем ~2×/сек, без покадровых аллокаций
        int c = TotalClaimable();
        if (c == _shownBadge) return;
        _shownBadge = c;
        _badge.SetActive(c > 0);
        if (_badgeText != null) _badgeText.text = c.ToString();
    }

    public void OpenMenu()
    {
        if (_menu == null) return;
        _dim.SetActive(true); _dim.transform.SetAsLastSibling();
        _menu.SetActive(true); _menu.transform.SetAsLastSibling();
        ShowSection(0);
        AudioController.Instance?.PlayUiOpen();
    }
    public void CloseMenu()
    {
        AudioController.Instance?.PlayUiClose();
        if (_menu != null) _menu.SetActive(false);
        if (_dim != null) _dim.SetActive(false);
        _shownBadge = -1;
    }

    // ─── Разделы ────────────────────────────────────────────────────────────────
    private void ShowSection(int s)
    {
        ClearContent();
        switch (s)
        {
            case 0: BuildRecipes(); break;
            case 1: BuildAlbum();   break;
            case 2: BuildEvent();   break;
            case 3: BuildSeason();  break;
            default: Wheel();       break;
        }
    }

    private void Wheel() { CloseMenu(); RewardWheelUI.Ensure().Open(); }

    private void BuildRecipes()
    {
        Row(Loc.T("Рецепты и мастерство", "Recipes & mastery"), FontStyles.Bold, new Color(0.9f,0.92f,1f));
        foreach (CoffeeType t in Enum.GetValues(typeof(CoffeeType)))
        {
            int lvl = RecipeBook.Level(t); int served = RecipeBook.Served(t);
            if (served == 0 && lvl == 0) continue; // ещё не открыт
            Row($"{RecipeBook.Name(t)} — {Loc.T("ур.", "lv.")}{lvl}  ({served})", FontStyles.Normal, Color.white);
        }
        if (RecipeBook.Discovered() == 0)
            Row(Loc.T("Подавай напитки — открывай рецепты и растит мастерство.", "Serve drinks — unlock recipes and grow mastery."), FontStyles.Italic, new Color(0.8f,0.85f,0.95f));
    }

    private void BuildAlbum()
    {
        int guests = GameManager.Instance != null ? GameManager.Instance.JournalKeys.Count : 0;
        int guestsTotal = Enum.GetValues(typeof(CharacterType)).Length;
        int recipes = RecipeBook.Discovered();
        int recipesTotal = Enum.GetValues(typeof(CoffeeType)).Length;

        Row(Loc.T("Альбом «Междумирья»", "The Inbetween album"), FontStyles.Bold, new Color(0.9f,0.92f,1f));
        Row(Loc.T($"Гости: {guests}/{guestsTotal}", $"Guests: {guests}/{guestsTotal}"), FontStyles.Normal, Color.white);
        Row(Loc.T($"Рецепты: {recipes}/{recipesTotal}", $"Recipes: {recipes}/{recipesTotal}"), FontStyles.Normal, Color.white);

        AlbumSet("guests_all", guests >= guestsTotal, 10, Loc.T("Все гости — забрать +10 крист.", "All guests — claim +10 gems"));
        AlbumSet("recipes_all", recipes >= recipesTotal, 10, Loc.T("Все рецепты — забрать +10 крист.", "All recipes — claim +10 gems"));
    }

    private void AlbumSet(string key, bool complete, int gems, string label)
    {
        bool claimed = YG.YG2.isSDKEnabled && YG.YG2.saves.albumSetsClaimed.Contains(key);
        var b = Button(claimed ? Loc.T("Набор забран", "Set claimed") : label, new Color(0.22f,0.45f,0.28f,1f), () =>
        {
            if (claimed || !complete) { AudioController.Instance?.PlayWrongOrder(); return; }
            YG.YG2.saves.albumSetsClaimed.Add(key);
            GameManager.Instance?.AddGems(gems); GameManager.Instance?.SaveGame();
            AudioController.Instance?.PlayBonus();
            Analytics.Send("album_set", "set", key);
            ShowSection(1);
        });
        b.interactable = complete && !claimed;
    }

    private void BuildEvent()
    {
        Row(Loc.T("Событие недели", "Weekly event"), FontStyles.Bold, new Color(0.9f,0.92f,1f));
        Row(Loc.T($"Очки: {EventManager.Progress} / {EventManager.NextThreshold()}",
                  $"Points: {EventManager.Progress} / {EventManager.NextThreshold()}"), FontStyles.Normal, Color.white);
        Row(Loc.T("Собирай звёзды за подачи — забирай кристаллы на вехах.", "Earn stars by serving — claim gems at milestones."), FontStyles.Italic, new Color(0.8f,0.85f,0.95f));
        var b = Button(EventManager.CanClaim() ? Loc.T("Забрать веху", "Claim milestone") : Loc.T("Веха ещё не достигнута", "Milestone not reached"),
            new Color(0.22f,0.45f,0.28f,1f), () =>
            {
                int g = EventManager.ClaimTier();
                if (g > 0) { AudioController.Instance?.PlayBonus(); UiEffects.Instance?.FloatingText($"+{g}", new Color(0.5f,0.8f,1f)); ShowSection(2); }
                else AudioController.Instance?.PlayWrongOrder();
            });
        b.interactable = EventManager.CanClaim();
    }

    private void BuildSeason()
    {
        Row(Loc.T("Сезонный пасс «Дневник Миры»", "Season pass \"Mira's Journal\""), FontStyles.Bold, new Color(0.9f,0.92f,1f));
        Row(Loc.T($"Уровень {SeasonPass.Level}/{SeasonPass.MaxLevel}  ({SeasonPass.XpInLevel}/{SeasonPass.XpPerLevel})",
                  $"Level {SeasonPass.Level}/{SeasonPass.MaxLevel}  ({SeasonPass.XpInLevel}/{SeasonPass.XpPerLevel})"), FontStyles.Normal, Color.white);
        Row(SeasonPass.Premium ? Loc.T("Премиум-трек активен", "Premium track active") : Loc.T("Трек: бесплатный", "Track: free"), FontStyles.Normal, new Color(0.85f,0.9f,1f));

        int claimable = SeasonPass.Claimable();
        var cb = Button(claimable > 0 ? Loc.T($"Забрать награды ({claimable})", $"Claim rewards ({claimable})") : Loc.T("Наград пока нет", "No rewards yet"),
            new Color(0.22f,0.45f,0.28f,1f), () =>
            {
                int total = 0;
                for (int l = 1; l <= SeasonPass.Level; l++) total += SeasonPass.Claim(l);
                if (total > 0) { AudioController.Instance?.PlayCoin(); UiEffects.Instance?.CoinBurst(total); ShowSection(3); }
                else AudioController.Instance?.PlayWrongOrder();
            });
        cb.interactable = claimable > 0;

        if (!SeasonPass.Premium)
        {
            var pb = Button(Loc.T($"Премиум за {SeasonPass.PremiumGemCost} крист.", $"Premium for {SeasonPass.PremiumGemCost} gems"),
                new Color(0.22f,0.38f,0.5f,1f), () =>
                {
                    if (SeasonPass.BuyPremiumWithGems()) { AudioController.Instance?.PlayBonus(); ShowSection(3); }
                    else AudioController.Instance?.PlayWrongOrder();
                });
            pb.interactable = GameManager.Instance != null && GameManager.Instance.Gems >= SeasonPass.PremiumGemCost;
        }
    }

    // ─── Хелперы контента ───────────────────────────────────────────────────────
    private float _y; // локальный курсор раскладки сверху вниз (в долях панели)
    private void ClearContent()
    {
        if (_content == null) return;
        for (int i = _content.transform.childCount - 1; i >= 0; i--) Destroy(_content.transform.GetChild(i).gameObject);
        _y = 0.98f;
    }
    private void Row(string text, FontStyles style, Color color)
    {
        float h = 0.09f;
        var t = MakeText("Row", _content.transform, new Vector2(0.02f, _y - h), new Vector2(0.98f, _y), 20, text, color, style, TextAlignmentOptions.Left);
        t.enableWordWrapping = true;
        _y -= h + 0.01f;
    }
    private Button Button(string label, Color bg, Action onClick)
    {
        float h = 0.11f;
        var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_content.transform, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = new Vector2(0.05f, _y - h); rt.anchorMax = new Vector2(0.95f, _y); rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        MakeText("L", go.transform, Vector2.zero, Vector2.one, 20, label, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        _y -= h + 0.015f;
        return go.GetComponent<Button>();
    }

    // ─── Построение каркаса ─────────────────────────────────────────────────────
    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("HubCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 217;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;

        // Кнопка «Прогресс» в левой колонке под «Задачами» (0.665..0.735) → 0.585..0.655.
        _btn = new GameObject("HubBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        _btn.transform.SetParent(canvasGo.transform, false);
        var brt = (RectTransform)_btn.transform; brt.anchorMin = new Vector2(0.02f, 0.585f); brt.anchorMax = new Vector2(0.20f, 0.655f); brt.offsetMin = brt.offsetMax = Vector2.zero;
        _btn.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.16f, 0.9f);
        _btn.GetComponent<Button>().onClick.AddListener(OpenMenu);
        MakeText("Cap", _btn.transform, new Vector2(0.06f, 0f), new Vector2(0.96f, 1f), 20, Loc.T("Прогресс", "Progress"), new Color(0.9f,0.92f,1f), FontStyles.Normal, TextAlignmentOptions.Left);

        _badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        _badge.transform.SetParent(_btn.transform, false);
        var bgRt = (RectTransform)_badge.transform; bgRt.anchorMin = bgRt.anchorMax = new Vector2(1f,1f); bgRt.sizeDelta = new Vector2(38,38); bgRt.anchoredPosition = new Vector2(2f,2f);
        _badge.GetComponent<Image>().color = new Color(0.9f,0.2f,0.2f,1f); _badge.GetComponent<Image>().raycastTarget = false;
        _badgeText = MakeText("N", _badge.transform, Vector2.zero, Vector2.one, 22, "", Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        _badge.SetActive(false);

        _dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        _dim.transform.SetParent(canvasGo.transform, false);
        var drt = (RectTransform)_dim.transform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
        _dim.GetComponent<Image>().color = new Color(0f,0f,0f,0.55f);
        _dim.GetComponent<Button>().onClick.AddListener(CloseMenu);
        _dim.SetActive(false);

        _menu = new GameObject("HubMenu", typeof(RectTransform), typeof(Image));
        _menu.transform.SetParent(canvasGo.transform, false);
        var mrt = (RectTransform)_menu.transform; mrt.anchorMin = new Vector2(0.22f, 0.14f); mrt.anchorMax = new Vector2(0.78f, 0.86f); mrt.offsetMin = mrt.offsetMax = Vector2.zero;
        _menu.GetComponent<Image>().color = new Color(0.06f,0.06f,0.13f,0.98f);

        // Вкладки-кнопки сверху.
        string[] tabsRu = { "Рецепты", "Альбом", "Событие", "Сезон", "Колесо" };
        string[] tabsEn = { "Recipes", "Album", "Event", "Season", "Wheel" };
        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            var go = new GameObject($"Tab{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_menu.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = new Vector2(0.02f + i * 0.196f, 0.9f); rt.anchorMax = new Vector2(0.02f + (i + 1) * 0.196f - 0.005f, 0.985f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.16f,0.16f,0.24f,1f);
            go.GetComponent<Button>().onClick.AddListener(() => ShowSection(idx));
            MakeText("L", go.transform, Vector2.zero, Vector2.one, 17, Loc.IsRu ? tabsRu[i] : tabsEn[i], Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        }

        _content = new GameObject("Content", typeof(RectTransform));
        _content.transform.SetParent(_menu.transform, false);
        var crt = (RectTransform)_content.transform; crt.anchorMin = new Vector2(0.04f, 0.1f); crt.anchorMax = new Vector2(0.96f, 0.88f); crt.offsetMin = crt.offsetMax = Vector2.zero;

        var close = MakeButton("Close", _menu.transform, new Vector2(0.38f, 0.01f), new Vector2(0.62f, 0.08f), new Color(0.28f,0.24f,0.30f,1f), CloseMenu);
        close.text = Loc.T("Закрыть", "Close");

        _menu.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size, string content, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }
    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 20, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
    }
}
