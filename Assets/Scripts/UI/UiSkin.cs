/// <summary>
/// Батч 16: единый «скин» интерфейса на ассетах Mini UI. Runtime-код не может грузить ассеты
/// через AssetDatabase, поэтому билдер (CoffeGameSceneSetup) находит-или-создаёт этот объект в
/// сцене и заполняет ссылки на спрайты/префабы Mini UI. Все код-построенные окна берут отсюда
/// рамки, кнопки, иконки и шрифт через UiKit, чтобы совпадать по стилю с остальной игрой.
///
/// Если объекта нет (например, сцена не пересобрана) — UiKit мягко откатывается на плоский вид.
/// Сцена: MainScene (singleton-компонент). Зависимости: TMPro. SDK: нет.
/// </summary>
using UnityEngine;
using TMPro;

public class UiSkin : MonoBehaviour
{
    public static UiSkin Instance { get; private set; }

    [Header("Панели / кнопки (9-slice Mini UI)")]
    public Sprite panelSprite;        // фон окна
    public Sprite panelAccentSprite;  // акцентная панель (карточки/строки)
    public Sprite buttonSprite;        // обычная кнопка
    public Sprite buttonAccentSprite;  // акцентная кнопка (главное действие)

    [Header("Иконки")]
    public Sprite coinIcon;
    public Sprite gemIcon;
    public Sprite heartIcon;
    public Sprite chestSprite;
    public Sprite wheelSprite;   // 5052447 — колесо на 6 секторов
    public Sprite badgeSprite;   // «!» — индикатор доступной покупки
    public Sprite arrowLeft;
    public Sprite arrowRight;
    public Sprite whiteSprite;   // плоский белый для шкал/заливок

    [Header("Готовые виджеты валют (Mini UI)")]
    public GameObject coinCountPrefab; // Mini UI/Prefabs/Coin Count.prefab
    public GameObject gemCountPrefab;  // Mini UI/Prefabs/Gem Count.prefab

    [Header("Шрифт")]
    public TMP_FontAsset font;

    private void Awake()
    {
        // Держим единственный экземпляр; если билдер положил заполненный — он выигрывает.
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Возвращает скин (с поиском по сцене, если синглтон ещё не проснулся).</summary>
    public static UiSkin Get()
    {
        if (Instance == null) Instance = FindObjectOfType<UiSkin>();
        return Instance;
    }
}
