/// <summary>
/// ScriptableObject с данными всех 20 дней сюжета.
/// Реплики взяты из документа реплики.docx (Мира и Кай — оригинальные имена).
/// Локализация: ru + en, выбор языка через Loc (плагин YG2, модуль Localization).
/// Создаётся через Assets → Create → CoffeGame → StoryDatabase
/// Зависимости: GameEnums.cs, Loc.cs
/// SDK: Нет
/// </summary>
using System.Collections.Generic;
using UnityEngine;

// ─── Одна строка диалога ─────────────────────────────────────────────────────

[System.Serializable]
public class DialogueLine
{
    [TextArea(1, 4)]
    public string speakerName = "Мира";
    public string speakerNameEn = "";
    [TextArea(2, 5)]
    public string text = "";
    [TextArea(2, 5)]
    public string textEn = "";
    public bool triggerSpeech = true;

    /// <summary>Имя говорящего на текущем языке.</summary>
    public string GetSpeaker() =>
        Loc.IsRu || string.IsNullOrEmpty(speakerNameEn) ? speakerName : speakerNameEn;

    /// <summary>Текст реплики на текущем языке.</summary>
    public string GetText() =>
        Loc.IsRu || string.IsNullOrEmpty(textEn) ? text : textEn;
}

// ─── Данные одного гостя ─────────────────────────────────────────────────────

[System.Serializable]
public class DayCustomerEntry
{
    [Header("Персонаж")]
    public CharacterType characterType = CharacterType.Traveler;
    public int stickmanIndex = 0;

    [Header("Заказ")]
    public CoffeeOrder order = new CoffeeOrder();

    [Header("Диалог — приветствие + сюжет")]
    public List<DialogueLine> greetingLines = new List<DialogueLine>();

    [Header("Диалог — при неправильном заказе")]
    public List<DialogueLine> wrongOrderLines = new List<DialogueLine>();

    [Header("Диалог — раскрытие сюжета (после правильного заказа)")]
    public List<DialogueLine> storyRevealLines = new List<DialogueLine>();
}

// ─── Данные одного дня ────────────────────────────────────────────────────────

[System.Serializable]
public class DayData
{
    public int dayNumber = 1;
    public List<DayCustomerEntry> customers = new List<DayCustomerEntry>();

    [TextArea(2, 4)]
    public string dayEndText = "День завершён.";
    [TextArea(2, 4)]
    public string dayEndTextEn = "";

    public bool hasVignette = false;
    [TextArea(2, 6)]
    public string vignetteText = "";
    [TextArea(2, 6)]
    public string vignetteTextEn = "";
    public VignetteEffectType vignetteEffect = VignetteEffectType.None;

    public int coinsPerCorrectOrder = 15;

    public string GetDayEndText() =>
        Loc.IsRu || string.IsNullOrEmpty(dayEndTextEn) ? dayEndText : dayEndTextEn;

    public string GetVignetteText() =>
        Loc.IsRu || string.IsNullOrEmpty(vignetteTextEn) ? vignetteText : vignetteTextEn;
}

public enum VignetteEffectType
{
    None,
    CameraShake,
    VisionLoss,
    RedPulse,
    DarknessFlash,
    BrightRestore
}

// ─── Главная база данных сюжета ───────────────────────────────────────────────

[CreateAssetMenu(fileName = "StoryDatabase", menuName = "CoffeGame/StoryDatabase", order = 1)]
public class StoryDatabase : ScriptableObject
{
    public List<DayData> days = new List<DayData>();

    public DayData GetDay(int n) { foreach (var d in days) if (d.dayNumber == n) return d; return null; }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    // Английские имена персонажей (для speakerNameEn)
    static readonly Dictionary<string, string> SpkEn = new Dictionary<string, string>
    {
        { "Мира", "Mira" },
        { "Кай", "Kai" },
        { "Странник", "Wanderer" },
        { "Водяной страж", "Water Warden" },
        { "Теневой торговец", "Shadow Merchant" },
        { "Огненный алхимик", "Fire Alchemist" },
        { "Хранитель книг", "Book Keeper" },
        { "Зеркальный вор", "Mirror Thief" },
        { "Временной курьер", "Time Courier" },
        { "Звёздный пастух", "Star Shepherd" },
        { "Незнакомка", "Stranger" },
        { "Туманный охотник", "Fog Hunter" },
        { "Паровой инженер", "Steam Engineer" },
        { "Лунный кузнец", "Moon Smith" },
        { "Кристаллическая певица", "Crystal Singer" },
        { "Лира", "Lira" },
        { "Картограф", "Cartographer" },
        { "Травница", "Herbalist" },
        { "Часовщик", "Clockkeeper" },
        { "Смотритель погоста", "Grave Warden" },
        { "Пасечник", "Beekeeper" },
        { "Эхо", "Echo" },
        { "Бард", "Bard" },
        { "Гадалка", "Fortune Teller" },
        { "Фонарщик", "Lamplighter" },
        { "Контрабандист", "Smuggler" },
        { "Перебежчик", "Defector" },
        { "Вдова", "Widow" },
        { "Стеклодув", "Glassblower" },
        { "Бабушка", "Grandmother" },
        { "Кай (в зеркале)", "Kai (in the mirror)" },
        { "Гость", "Guest" },
        { "Все", "Everyone" },
        { "...", "..." }
    };

    static List<DialogueLine> L(params (string spk, string ru, string en)[] lines)
    {
        var list = new List<DialogueLine>();
        foreach (var (spk, ru, en) in lines)
            list.Add(new DialogueLine
            {
                speakerName   = spk,
                speakerNameEn = SpkEn.TryGetValue(spk, out var e) ? e : spk,
                text          = ru,
                textEn        = en
            });
        return list;
    }

    static CoffeeOrder Order(CoffeeType t, Volume v = Volume.Medium,
        SweetnessLevel s = SweetnessLevel.None, Topping top = Topping.None)
        => new CoffeeOrder { type = t, volume = v, sweet = s, topping = top };

    // ─── ЗАПОЛНИТЬ ВСЕ 20 ДНЕЙ ───────────────────────────────────────────────

    [ContextMenu("Fill Default Story Data")]
    public void FillDefaultData()
    {
        days.Clear();

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 1
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 1, coinsPerCorrectOrder = 10,
            dayEndText   = "Первый день позади. Ты узнала о знаке Ордена — три круга. Кай носил такой медальон...",
            dayEndTextEn = "The first day is over. You learned of the Order's sign — three circles. Kai wore a medallion like that...",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler, stickmanIndex = 0,
                    order = Order(CoffeeType.HerbalTea),
                    greetingLines = L(
                        ("Мира",       "Добро пожаловать в «Междумирье». Что для вас?",
                                       "Welcome to the Inbetween. What can I get you?"),
                        ("Странник",   "Травяной чай. Я из Пыльных Пределов. Здесь пахнет тоской.",
                                       "Herbal tea. I come from the Dusty Reaches. It smells of sorrow here.")
                    ),
                    wrongOrderLines = L(
                        ("Странник",   "Травяной чай, пожалуйста.",
                                       "Herbal tea, please.")
                    ),
                    storyRevealLines = L(
                        ("Мира",       "Мой муж Кай пропал. Вы не видели мужчину с медальоном из трёх кругов?",
                                       "My husband Kai is missing. Have you seen a man with a three-circle medallion?"),
                        ("Странник",   "Три круга... Это знак Ордена. Они ходят у Зеркального Ущелья. Будь осторожна с вопросами.",
                                       "Three circles... That is the sign of the Order. They roam near the Mirror Gorge. Be careful with your questions."),
                        ("Странник",   "Дорога за ним долгая и опасная. Чтобы добраться до Ущелья и снарядиться, нужно не меньше 10 000 монет. Копи — кофейня прокормит мечту. (Оставляет чай недопитым, уходит)",
                                       "The road after him is long and dangerous. To reach the Gorge and outfit yourself you'll need at least 10,000 coins. Save up — the coffee house can fund a dream. (Leaves the tea unfinished and walks out)"),
                        ("Мира",       "Десять тысяч монет. Я накоплю. Я найду тебя, Кай.",
                                       "Ten thousand coins. I'll save them. I will find you, Kai.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 2
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 2, coinsPerCorrectOrder = 10,
            dayEndText   = "Вода помнит всё. Месяц назад Кая увели у Ущелья. Нужна его вещь.",
            dayEndTextEn = "Water remembers everything. A month ago Kai was taken away near the Gorge. You need something of his.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.WaterGuard, stickmanIndex = 1,
                    order = Order(CoffeeType.Water, Volume.Large),
                    greetingLines = L(
                        ("Мира",              "Здравствуйте. Что будете?",
                                              "Hello. What will you have?"),
                        ("Водяной страж",     "Воду. Я страж водных границ.",
                                              "Water. I am a warden of the water borders.")
                    ),
                    wrongOrderLines = L(
                        ("Водяной страж",     "Только воду. Чистую воду.",
                                              "Only water. Pure water.")
                    ),
                    storyRevealLines = L(
                        ("Мира",              "Вы что-нибудь слышали о пропавших у Ущелья?",
                                              "Have you heard anything about people going missing near the Gorge?"),
                        ("Водяной страж",     "Вода помнит всё. Месяц назад там был спор. Человек с тремя кругами на груди спорил с теми, у кого такие же круги, но на ладонях. Его увели. (Пьёт медленно.) Если найдёшь его вещь, вода покажет больше.",
                                              "Water remembers everything. A month ago there was an argument there. A man with three circles on his chest argued with those who bore the same circles on their palms. They took him away. (Drinks slowly.) If you find something of his, the water will show you more.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 3
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 3, coinsPerCorrectOrder = 15,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.CameraShake,
            vignetteText   = "Ночью тебе снилось Зеркальное Ущелье. Три круга, горящие в темноте. Ты просыпаешься с дрожью в руках.",
            vignetteTextEn = "At night you dreamed of the Mirror Gorge. Three circles burning in the dark. You wake up with trembling hands.",
            dayEndText   = "Орден держит своих пленников в «Зеркальной Темнице». Нужен проводник во снах — Сновидица.",
            dayEndTextEn = "The Order keeps its prisoners in the Mirror Dungeon. You need a guide through dreams — the Dreamweaver.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.ShadowMerchant, stickmanIndex = 2,
                    order = Order(CoffeeType.Americano),
                    greetingLines = L(
                        ("Мира",                  "Кофе?",
                                                  "Coffee?"),
                        ("Теневой торговец",       "Кофе. Я вижу, ты ищешь. Это дорого обходится.",
                                                  "Coffee. I can see you are searching. That comes at a price.")
                    ),
                    wrongOrderLines = L(
                        ("Теневой торговец",       "Кофе. Обычный кофе.",
                                                  "Coffee. Just regular coffee.")
                    ),
                    storyRevealLines = L(
                        ("Мира",                  "Что вы знаете об Ордене Трёх Кругов?",
                                                  "What do you know about the Order of Three Circles?"),
                        ("Теневой торговец",       "Они мои лучшие клиенты. Покупают информацию о «тонких местах» — где границы между мирами можно разорвать. Твой муж... он интересовался обратным. Хотел их укрепить. Это сделало его врагом.",
                                                  "They are my best customers. They buy information about the 'thin places' — where the borders between worlds can be torn. Your husband... he was interested in the opposite. He wanted to strengthen them. That made him an enemy."),
                        ("Мира",                  "Где он сейчас?",
                                                  "Where is he now?"),
                        ("Теневой торговец",       "Информация стоит. Принеси завтра самое яркое воспоминание о нём в этой склянке. (Ставит на стойку маленький хрустальный флакон)",
                                                  "Information has a price. Tomorrow, bring me your brightest memory of him in this vial. (Places a small crystal vial on the counter)")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 4
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 4, coinsPerCorrectOrder = 15,
            dayEndText   = "«Зеркальная Темница» — карман реальности у Ущелья. Нужен проводник во снах: Сновидица — Рынок Теней, за часами.",
            dayEndTextEn = "The Mirror Dungeon is a pocket of reality near the Gorge. You need a dream guide: the Dreamweaver — Shadow Market, behind the clock.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.ShadowMerchant, stickmanIndex = 2,
                    order = Order(CoffeeType.Americano),
                    greetingLines = L(
                        ("Мира",              "(Ставит флакон, полный серебристого света.) Я принесла воспоминание.",
                                              "(Sets down a vial full of silvery light.) I brought the memory."),
                        ("Теневой торговец",   "(Берёт флакон, взвешивает на руке.) Искренне. Хорошо. Пока ты готовишь — расскажу.",
                                              "(Takes the vial, weighs it in his hand.) Sincere. Good. I'll talk while you brew.")
                    ),
                    wrongOrderLines = L(
                        ("Теневой торговец",   "Тот же кофе, что и вчера.",
                                              "The same coffee as yesterday.")
                    ),
                    storyRevealLines = L(
                        ("Теневой торговец",   "Орден держит своих пленников в «Зеркальной Темнице». Это не место, а состояние — карман реальности рядом с Ущельем. Чтобы войти, нужен проводник, который знает дорогу во снах. Ищи Сновидицу.",
                                              "The Order keeps its prisoners in the Mirror Dungeon. It is not a place but a state — a pocket of reality near the Gorge. To enter, you need a guide who knows the way through dreams. Seek the Dreamweaver."),
                        ("Теневой торговец",   "(Даёт смятую карту с отметкой: «Сновидица — Рынок Теней, за часами»)",
                                              "(Hands over a crumpled map with a mark: 'Dreamweaver — Shadow Market, behind the clock')")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 5
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 5, coinsPerCorrectOrder = 15,
            dayEndText   = "«Разрыв Покрова» — ритуал в следующее лунное затмение. Кая похитили чтобы он не помешал ИЛИ чтобы заставить работать на них.",
            dayEndTextEn = "The Veil Rending — a ritual at the next lunar eclipse. Kai was taken either to keep him from interfering OR to force him to work for them.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.FireAlchemist, stickmanIndex = 3,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Мира",                "Эспрессо?",
                                                "Espresso?"),
                        ("Огненный алхимик",     "Да, крепкий. Орден сжёг мою мастерскую. Они крадут артефакты, связанные со стихиями, для своего ритуала.",
                                                "Yes, strong. The Order burned down my workshop. They steal artifacts bound to the elements for their ritual.")
                    ),
                    wrongOrderLines = L(
                        ("Огненный алхимик",     "Крепкий эспрессо. Маленький стакан.",
                                                "A strong espresso. Small cup.")
                    ),
                    storyRevealLines = L(
                        ("Мира",                "Какого ритуала?",
                                                "What ritual?"),
                        ("Огненный алхимик",     "«Разрыва Покрова». В следующее лунное затмение. Твой муж, должно быть, узнал детали. Его похитили, чтобы он не помешал ИЛИ чтобы заставить работать на них. Будь готова к обоим вариантам.",
                                                "The Veil Rending. At the next lunar eclipse. Your husband must have learned the details. He was taken so he couldn't interfere — OR to be forced to work for them. Be ready for either.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 6
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 6, coinsPerCorrectOrder = 15,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.CameraShake,
            vignetteText   = "Поздно ночью ты слышишь звук у двери. Никого нет. Только маленький камень с тремя выгравированными кругами.",
            vignetteTextEn = "Late at night you hear a sound at the door. No one is there. Only a small stone engraved with three circles.",
            dayEndText   = "«Скрижали Границ» украдены. В книге не только точки разрыва, но и «Якоря» — артефакты, стабилизирующие границы навсегда.",
            dayEndTextEn = "The Border Tablets have been stolen. The book describes not only rupture points but also the Anchors — artifacts that stabilize the borders forever.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.BookKeeper, stickmanIndex = 4,
                    order = Order(CoffeeType.BlackCoffee),
                    greetingLines = L(
                        ("Мира",           "Чёрный кофе?",
                                           "Black coffee?"),
                        ("Хранитель книг", "Да. В архивах пропала книга «Скрижали Границ». Её украли люди с клеймом трёх кругов.",
                                           "Yes. A book has vanished from the archives — the Border Tablets. It was stolen by people branded with three circles.")
                    ),
                    wrongOrderLines = L(
                        ("Хранитель книг", "Чёрный кофе, без ничего.",
                                           "Black coffee, nothing in it.")
                    ),
                    storyRevealLines = L(
                        ("Мира",           "Зачем она им?",
                                           "Why do they want it?"),
                        ("Хранитель книг", "В ней описаны не только точки разрыва, но и «Якоря» — артефакты, которые могут стабилизировать границы навсегда. Орден, скорее всего, уничтожит её. Если найдёшь хоть страницу — я обменяю на полезное знание.",
                                           "It describes not only the rupture points but also the Anchors — artifacts that can stabilize the borders forever. The Order will most likely destroy it. If you find even a page — I will trade it for useful knowledge.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 7
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 7, coinsPerCorrectOrder = 20,
            dayEndText   = "Зеркальный вор дал тебе треснувшее зеркало. Если увидишь женщину с шрамом-молнией — скажи «эхо простило её».",
            dayEndTextEn = "The Mirror Thief gave you a cracked mirror. If you see a woman with a lightning-shaped scar — tell her 'the echo has forgiven her'.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.MirrorThief, stickmanIndex = 5,
                    order = Order(CoffeeType.Mocha, Volume.Medium, SweetnessLevel.None, Topping.None),
                    greetingLines = L(
                        ("Мира",            "Здравствуйте. Что-то новое?",
                                            "Hello. Something new today?"),
                        ("Зеркальный вор",   "Мокко. Я слышал, ты ищешь способ увидеть невидимое. (Достаёт маленькое треснувшее зеркальце.) Это показывает то, что скрыто за пеленой. Но только раз. Используй с умом.",
                                            "Mocha. I heard you seek a way to see the unseen. (Takes out a small cracked mirror.) This shows what is hidden behind the veil. But only once. Use it wisely.")
                    ),
                    wrongOrderLines = L(
                        ("Зеркальный вор",   "Мокко. Именно мокко.",
                                            "Mocha. Mocha exactly.")
                    ),
                    storyRevealLines = L(
                        ("Мира",            "Что вы хотите взамен?",
                                            "What do you want in return?"),
                        ("Зеркальный вор",   "Ничего. Просто... если увидишь среди них женщину с шрамом в форме молнии на шее — скажи, что «эхо простило её». (Быстро уходит)",
                                            "Nothing. Just... if you see among them a woman with a lightning-shaped scar on her neck — tell her 'the echo has forgiven her'. (Leaves quickly)")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 8 — Зеркало
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 8, coinsPerCorrectOrder = 20,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.VisionLoss,
            vignetteText   = "Мира смотрит в треснувшее зеркало, думая о Кае. В трещине — он, привязан в каменной комнате. Перед ним фигура в капюшоне. Женщина с шрамом-молнией. Внезапно из зеркала смотрит незнакомец: «Ты любопытна. Перестань смотреть, а то придём в гости.» Зеркальце чернеет и рассыпается в пыль.",
            vignetteTextEn = "Mira looks into the cracked mirror, thinking of Kai. In the crack — he is bound in a stone room. Before him stands a hooded figure. A woman with a lightning scar. Suddenly a stranger stares back from the mirror: 'You are curious. Stop watching, or we will pay you a visit.' The mirror turns black and crumbles to dust.",
            dayEndText   = "Кай жив! Его держит женщина со шрамом-молнией. Они видели тебя через зеркало.",
            dayEndTextEn = "Kai is alive! He is held by the woman with the lightning scar. They saw you through the mirror.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler, stickmanIndex = 0,
                    order = Order(CoffeeType.HerbalTea, Volume.Small, SweetnessLevel.Low),
                    greetingLines = L(
                        ("Странник", "Снова травяной, но сегодня маленький и чуть слаще. День тяжёлый.",
                                     "Herbal again, but today a small one and a little sweeter. It's been a hard day.")
                    ),
                    wrongOrderLines = L(
                        ("Странник", "Маленький стакан. И немного сахара.",
                                     "A small cup. And a bit of sugar.")
                    ),
                    storyRevealLines = L(
                        ("Странник", "Ты выглядишь усталой. Зеркала опасны. Некоторые смотрят в обе стороны.",
                                     "You look tired. Mirrors are dangerous. Some of them look both ways.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 9
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 9, coinsPerCorrectOrder = 20,
            dayEndText   = "Письмо от Кая через разрыв во времени. Он жив. «Не верь первому, кто предложит помощь. Ищи ключ в мелодии, которую пел тебе отец.»",
            dayEndTextEn = "A letter from Kai through a rift in time. He is alive. 'Don't trust the first one who offers help. Seek the key in the melody your father used to sing to you.'",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.TimeCourier, stickmanIndex = 6,
                    order = Order(CoffeeType.Americano),
                    greetingLines = L(
                        ("Мира",               "(Волнуясь.) Американо?",
                                               "(Nervously.) Americano?"),
                        ("Временной курьер",    "Да. У меня для тебя... странная посылка. (Достаёт обгоревший конверт.) Её передали через разрыв во времени. Адресовано тебе.",
                                               "Yes. I have a... strange delivery for you. (Takes out a scorched envelope.) It was passed through a rift in time. Addressed to you.")
                    ),
                    wrongOrderLines = L(
                        ("Временной курьер",    "Американо. У меня мало времени.",
                                               "Americano. I'm short on time.")
                    ),
                    storyRevealLines = L(
                        ("Мира",               "(Читает.) «Мира. Я жив. Они хотят мой дар. Не верь первому, кто предложит помощь. Ищи ключ в мелодии, которую пел тебе отец. Я люблю тебя.»",
                                               "(Reads.) 'Mira. I am alive. They want my gift. Don't trust the first one who offers help. Seek the key in the melody your father used to sing to you. I love you.'"),
                        ("Мира",               "Откуда это? Когда?",
                                               "Where did this come from? When?"),
                        ("Временной курьер",    "Конверт пахнет пеплом и... страхом. Будь осторожнее. Они могут использовать время против тебя.",
                                               "The envelope smells of ash and... fear. Be careful. They can use time against you.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 10
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 10, coinsPerCorrectOrder = 20,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.RedPulse,
            vignetteText   = "Звёзды красные этой ночью. Луна скоро станет алой. Ритуал «Разрыва Покрова» всё ближе.",
            vignetteTextEn = "The stars are red tonight. The moon will soon turn crimson. The Veil Rending ritual draws nearer.",
            dayEndText   = "Алтарь Ордена — место трёх теней. Ищи там, где падает свет от Чёрного Фонаря. Он светит только в безлунную ночь.",
            dayEndTextEn = "The Order's altar is the place of three shadows. Look where the light of the Black Lantern falls. It shines only on a moonless night.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.StarShepherd, stickmanIndex = 7,
                    order = Order(CoffeeType.HotChocolate, Volume.Medium, SweetnessLevel.High),
                    greetingLines = L(
                        ("Мира",            "Что-то тёплое?",
                                            "Something warm?"),
                        ("Звёздный пастух",  "Какао, пожалуй. Звёзды шепчут о надвигающемся разрыве. Луна скоро станет красной — это знак.",
                                            "Cocoa, perhaps. The stars whisper of a coming rupture. The moon will soon turn red — it is a sign.")
                    ),
                    wrongOrderLines = L(
                        ("Звёздный пастух",  "Горячий шоколад. Побольше сладкого — звёзды плохих вестей требуют этого.",
                                            "Hot chocolate. Extra sweet — stars of bad news demand it.")
                    ),
                    storyRevealLines = L(
                        ("Мира",            "Ритуал Ордена?",
                                            "The Order's ritual?"),
                        ("Звёздный пастух",  "Да. Их алтарь находится в месте, где сходятся три тени. Ищи там, где падает свет от Чёрного Фонаря. Он светит только в безлунную ночь.",
                                            "Yes. Their altar lies where three shadows meet. Look where the light of the Black Lantern falls. It shines only on a moonless night.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 11 — Незнакомка (ЛИРА)
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 11, coinsPerCorrectOrder = 20,
            dayEndText   = "Незнакомка со шрамом-молнией предлагала забвение. Монета ледяная, три круга на обороте.",
            dayEndTextEn = "The stranger with the lightning scar offered oblivion. The coin is ice-cold, three circles on its back.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Lira, stickmanIndex = 8,
                    order = Order(CoffeeType.GreenTea),
                    greetingLines = L(
                        ("Незнакомка", "Зелёный чай. Вы — хозяйка?",
                                       "Green tea. Are you the owner?"),
                        ("Мира",       "Да. Я Мира.",
                                       "Yes. I am Mira."),
                        ("Незнакомка", "Я слышала, вы задаёте вопросы об Ордене. Это небезопасно. Иногда лучше отпустить прошлое. (Ставит на стойку золотую монету.) На это можно купить забвение.",
                                       "I hear you've been asking questions about the Order. That is not safe. Sometimes it is better to let the past go. (Places a gold coin on the counter.) This can buy oblivion.")
                    ),
                    wrongOrderLines = L(
                        ("Незнакомка", "Зелёный чай. (Ледяной взгляд)",
                                       "Green tea. (An icy stare)")
                    ),
                    storyRevealLines = L(
                        ("Мира",       "Я не продаю память.",
                                       "I don't sell memory."),
                        ("Незнакомка", "Жаль. (Уходит, оставив монету)",
                                       "A pity. (Leaves, the coin still on the counter)"),
                        ("Мира",       "(Касается монеты — она ледяная. На обратной стороне выгравированы три круга.)",
                                       "(Touches the coin — it is ice-cold. Three circles are engraved on its back.)")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 12
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 12, coinsPerCorrectOrder = 25,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.CameraShake,
            vignetteText   = "Ночью кто-то пытался взломать дверь «Междумирья». Замок выдержал — но это было предупреждением.",
            vignetteTextEn = "At night someone tried to break into the Inbetween. The lock held — but it was a warning.",
            dayEndText   = "Чёрный Фонарь украден. Внутреннее предательство в рядах союзников Хранителя.",
            dayEndTextEn = "The Black Lantern has been stolen. A betrayal from within the Keeper's allies.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.FogHunter, stickmanIndex = 3,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Мира",              "Вы знаете, что такое Чёрный Фонарь?",
                                              "Do you know what the Black Lantern is?"),
                        ("Туманный охотник",   "Это не фонарь, а артефакт. Он освещает путь к местам силы Ордена. Он хранится у Хранителя Книг. Но его, кажется, украли.",
                                              "It is not a lantern but an artifact. It lights the way to the Order's places of power. The Book Keeper kept it. But it seems to have been stolen.")
                    ),
                    wrongOrderLines = L(
                        ("Туманный охотник",   "Маленький эспрессо. Крепкий.",
                                              "A small espresso. Strong.")
                    ),
                    storyRevealLines = L(
                        ("Хранитель книг",     "(Кивнув с другого столика.) Верно. Неделю назад. Вместе с той самой книгой. Я подозреваю внутреннее предательство.",
                                              "(Nodding from another table.) True. A week ago. Together with that very book. I suspect a betrayal from within.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 13 — Кофе Правды
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 13, coinsPerCorrectOrder = 25,
            dayEndText   = "Паровой инженер может заглушить чары Ордена. Ему нужна шестерня из бронзового «глаза».",
            dayEndTextEn = "The Steam Engineer can muffle the Order's wards. He needs a gear from a bronze 'eye'.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.SteamEngineer, stickmanIndex = 4,
                    order = Order(CoffeeType.TruthBrew, Volume.Small),
                    greetingLines = L(
                        ("Мира",               "(Готовит «Кофе Правды» — редкий сорт, заставляющий говорить искренне.) Сегодня особый напиток.",
                                               "(Brews the Truth Brew — a rare blend that makes one speak sincerely.) A special drink today.")
                    ),
                    wrongOrderLines = L(
                        ("Паровой инженер",     "«Кофе Правды» — особый сорт. Маленькая порция.",
                                               "The Truth Brew — a special blend. A small serving.")
                    ),
                    storyRevealLines = L(
                        ("Паровой инженер",     "(Пьёт.) Интересный вкус... Говорит, ты в беде. Я мог бы сделать устройство, которое заглушит защитные чары Ордена. Но мне нужна шестерёнка из их механизма.",
                                               "(Drinks.) Interesting taste... It says you are in trouble. I could build a device to muffle the Order's protective wards. But I need a gear from their mechanism."),
                        ("Мира",               "Где её взять?",
                                               "Where do I get one?"),
                        ("Паровой инженер",     "У них везде есть смотровые «глаза» из бронзы и хрусталя. Принеси одно.",
                                               "They have watching 'eyes' of bronze and crystal everywhere. Bring me one.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 14 — Лунный кузнец
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 14, coinsPerCorrectOrder = 25,
            dayEndText   = "Лунное серебро выявляет ложь. Если подозрительный гость солжёт — его напиток потемнеет.",
            dayEndTextEn = "Moon silver reveals lies. If a suspicious guest lies — their drink will darken.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.MoonSmith, stickmanIndex = 5,
                    order = Order(CoffeeType.HerbalTea, Volume.Large, SweetnessLevel.Low),
                    greetingLines = L(
                        ("Лунный кузнец", "(Сидит в углу, молчит)",
                                          "(Sits in the corner, silent)")
                    ),
                    wrongOrderLines = L(
                        ("Лунный кузнец", "...Травяной чай. Большой. Слегка сладкий.",
                                          "...Herbal tea. Large. Lightly sweet.")
                    ),
                    storyRevealLines = L(
                        ("Мира",          "(К лунному кузнецу, который обычно молчит в углу.) Вы работаете с металлом. Не делали ли вы таких шестерён? (Показывает рисунок.)",
                                          "(To the Moon Smith, who usually sits silent in the corner.) You work with metal. Have you ever made gears like this? (Shows a drawing.)"),
                        ("Лунный кузнец", "Да. По заказу женщины с шрамом. Я ковал много для Ордена. Раньше не знал, для чего. Теперь жалею. Возьми эту песчинку лунного серебра. Она притягивает ложь. Положи в напиток тому, кому не доверяешь. Если он солжёт — напиток потемнеет.",
                                          "Yes. Commissioned by a woman with a scar. I forged much for the Order. I didn't know what for, back then. Now I regret it. Take this grain of moon silver. It draws out lies. Put it in the drink of someone you don't trust. If they lie — the drink will darken.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 15 — Кристаллическая певица, Песня Якоря
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 15, coinsPerCorrectOrder = 25,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.RedPulse,
            vignetteText   = "Мира напевает колыбельную. Стёкла в кофейне зазвенели. В мелодии что-то живое — глубокое, из самой земли.",
            vignetteTextEn = "Mira hums a lullaby. The glass in the coffee house rings. There is something alive in the melody — deep, from the earth itself.",
            dayEndText   = "«Песня Якоря» — это колыбельная отца. Орден боится её. Дверь в обсерваторию на Граничном Утёсе открывается только под неё.",
            dayEndTextEn = "The Anchor Song is your father's lullaby. The Order fears it. The observatory door on the Border Cliff opens only to it.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.CrystalSinger, stickmanIndex = 6,
                    order = Order(CoffeeType.Latte, Volume.Medium, SweetnessLevel.Medium, Topping.Cinnamon),
                    greetingLines = L(
                        ("Мира",                    "(К Кристаллической певице.) Вы чувствуете вибрации миров. Где находится «место трёх теней»?",
                                                    "(To the Crystal Singer.) You feel the vibrations of the worlds. Where is the 'place of three shadows'?"),
                        ("Кристаллическая певица",   "Я слышала, как о нём поют камни. Это старая обсерватория на Граничном Утёсе. Но дверь туда открывается только под звук забытой мелодии. Той, что передаётся в семье.",
                                                    "I have heard the stones sing of it. It is the old observatory on the Border Cliff. But its door opens only to the sound of a forgotten melody. One passed down within a family.")
                    ),
                    wrongOrderLines = L(
                        ("Кристаллическая певица",   "Латте с корицей. Камни любят тепло и пряности.",
                                                    "A latte with cinnamon. Stones love warmth and spice.")
                    ),
                    storyRevealLines = L(
                        ("Мира",                    "(Вспоминает колыбельную, которую пел отец. Напевает её.)",
                                                    "(Remembers the lullaby her father used to sing. Hums it.)"),
                        ("Кристаллическая певица",   "Да... это она. «Песня Якоря». Орден боится её. Твой муж, наверное, знал её. Запомни хорошенько.",
                                                    "Yes... that is it. The Anchor Song. The Order fears it. Your husband must have known it. Remember it well.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 16 — Ловушка с лунным серебром
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 16, coinsPerCorrectOrder = 30,
            dayEndText   = "Странник был под контролем Ордена. Чай почернел от лунного серебра — ложь. Тебя пытаются выманить.",
            dayEndTextEn = "The Wanderer was under the Order's control. The tea turned black from the moon silver — a lie. They are trying to lure you out.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler, stickmanIndex = 0,
                    order = Order(CoffeeType.HerbalTea),
                    greetingLines = L(
                        ("Странник", "(Входит, но ведёт себя иначе. Глаза бесцветны.) Травяной чай. Я принёс весть. Твой муж хочет, чтобы ты пришла к Старому Дубу на окраине города в полночь. Один.",
                                     "(Enters, but acts differently. His eyes are colorless.) Herbal tea. I bring word. Your husband wants you to come to the Old Oak on the edge of town at midnight. Alone."),
                        ("Мира",     "(Подозрительно.) Почему не через зеркало или сновидицу?",
                                     "(Suspicious.) Why not through the mirror or the Dreamweaver?"),
                        ("Странник", "(Голос становится металлическим.) Он ждёт. (Уходит)",
                                     "(His voice turns metallic.) He is waiting. (Leaves)")
                    ),
                    wrongOrderLines = L(
                        ("Странник", "Травяной чай. (Пусто смотрит)",
                                     "Herbal tea. (Stares blankly)")
                    ),
                    storyRevealLines = L(
                        ("Мира",     "(Кладёт песчинку лунного серебра в его недопитый чай. Чай чернеет и испаряется.)",
                                     "(Drops the grain of moon silver into his unfinished tea. The tea turns black and evaporates.)"),
                        ("Мира",     "Ловушка. Орден пытается заманить меня.",
                                     "A trap. The Order is trying to lure me out.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 17 — Союзники собираются
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 17, coinsPerCorrectOrder = 30,
            dayEndText   = "Союзники готовы. Но лунное затмение — ещё через много дней. Нужно держаться, копить силы и не выдать себя.",
            dayEndTextEn = "The allies are ready. But the lunar eclipse is still many days away. You must hold on, gather strength, and not give yourself away.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.FogHunter, stickmanIndex = 3,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Мира",              "Орден пытается меня выманить. Но у меня есть песня и план. Помогите мне устроить ловушку.",
                                              "The Order is trying to lure me out. But I have the song and a plan. Help me set a trap."),
                        ("Туманный охотник",   "Я скрою место туманом иллюзий.",
                                              "I will hide the place in a fog of illusions."),
                        ("Паровой инженер",    "Я установлю звуковые репелленты от их артефактов.",
                                              "I will set up sonic repellents against their artifacts."),
                        ("Зеркальный вор",     "Я поставлю зеркала-ловушки. Они увидят себя.",
                                              "I will place trap mirrors. They will see themselves."),
                        ("Водяной страж",      "Вода покажет, если кто-то приблизится с дурными намерениями.",
                                              "The water will reveal anyone approaching with ill intent.")
                    ),
                    wrongOrderLines = L(
                        ("Туманный охотник",   "Маленький эспрессо. Нам нужны силы.",
                                              "A small espresso. We need our strength.")
                    ),
                    storyRevealLines = L(
                        ("Мира",              "(Разносит кофе союзникам.) Ждём затмения. До него ещё долгие недели — будьте начеку и держите план в тайне.",
                                              "(Serves coffee to the allies.) We wait for the eclipse. It is still weeks away — stay alert and keep the plan secret.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 18 — Картограф: карты «тонких мест»
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 18, coinsPerCorrectOrder = 30,
            dayEndText   = "Картограф отметил три «тонких места». Алтарь Ордена — на Граничном Утёсе, у старой обсерватории.",
            dayEndTextEn = "The Cartographer marked three 'thin places'. The Order's altar is on the Border Cliff, by the old observatory.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Cartographer, stickmanIndex = 1,
                    order = Order(CoffeeType.Latte, Volume.Medium, SweetnessLevel.Low),
                    greetingLines = L(
                        ("Мира",       "Латте? У вас руки в чернилах и пыли дорог.",
                                       "A latte? Your hands are stained with ink and road dust."),
                        ("Картограф",  "Я рисую карты мест, которых нет на картах. Ты ищешь разрыв — я знаю, где истончается мир.",
                                       "I draw maps of places that aren't on maps. You're looking for a rift — I know where the world thins.")
                    ),
                    wrongOrderLines = L(
                        ("Картограф",  "Латте, чуть сладкий. Чернила любят молоко.",
                                       "A latte, lightly sweet. Ink loves milk.")
                    ),
                    storyRevealLines = L(
                        ("Картограф",  "(Разворачивает карту.) Три тонких места. Два — ловушки. Настоящий алтарь — у обсерватории на Граничном Утёсе. Там Орден разорвёт Покров.",
                                       "(Unrolls a map.) Three thin places. Two are decoys. The true altar is by the observatory on the Border Cliff. There the Order will rend the Veil."),
                        ("Мира",       "Значит, туда я и приду.",
                                       "Then that is where I'll go.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 19 — Травница: зелье снов
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 19, coinsPerCorrectOrder = 30,
            dayEndText   = "Травница дала зелье снов — теперь ты сможешь искать Кая во сне сама, без проводника.",
            dayEndTextEn = "The Herbalist gave you a dream draught — now you can search for Kai in dreams yourself, without a guide.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Herbalist, stickmanIndex = 2,
                    order = Order(CoffeeType.HerbalTea, Volume.Small, SweetnessLevel.Medium),
                    greetingLines = L(
                        ("Травница",   "Травяной, маленький, послаще. И добавь то, что я положу. (Кладёт щепоть сухих цветов.)",
                                       "Herbal, small, a bit sweet. And add what I give you. (Drops in a pinch of dried flowers.)"),
                        ("Мира",       "Что это?",
                                       "What is this?")
                    ),
                    wrongOrderLines = L(
                        ("Травница",   "Маленький травяной. Средне сладкий. С моими цветами.",
                                       "A small herbal. Medium sweet. With my flowers.")
                    ),
                    storyRevealLines = L(
                        ("Травница",   "Сон-трава. Выпьешь перед сном — пойдёшь во сне туда, куда зовёт сердце. Найди Кая. Но не задерживайся: во сне Орден тоже не спит.",
                                       "Dreamgrass. Drink it before sleep — and you'll walk in dream to where your heart calls. Find Kai. But don't linger: in dreams the Order doesn't sleep either.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 20 — Часовщик: дата затмения
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 20, coinsPerCorrectOrder = 30,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.VisionLoss,
            vignetteText   = "Во сне ты идёшь по коридору из часов. Все они показывают одно время — миг затмения. В конце коридора — дверь, за ней голос Кая: «Считай дни, Мира».",
            vignetteTextEn = "In a dream you walk a corridor of clocks. All show the same time — the moment of the eclipse. At its end a door, and behind it Kai's voice: 'Count the days, Mira.'",
            dayEndText   = "Часовщик вычислил: лунное затмение — ровно через три недели. У тебя есть срок.",
            dayEndTextEn = "The Clockkeeper calculated it: the lunar eclipse is exactly three weeks away. Now you have a deadline.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.ClockKeeper, stickmanIndex = 3,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Часовщик",   "Эспрессо. Маленький. Время — единственное, чего мне не хватает.",
                                       "Espresso. Small. Time is the one thing I never have enough of."),
                        ("Мира",       "Вы знаете, когда затмение?",
                                       "Do you know when the eclipse comes?")
                    ),
                    wrongOrderLines = L(
                        ("Часовщик",   "Маленький эспрессо. И поскорее — часы не ждут.",
                                       "A small espresso. And quickly — the clocks don't wait.")
                    ),
                    storyRevealLines = L(
                        ("Часовщик",   "Ровно через три недели. Орден будет копить силу до последней ночи. Готовься медленно, но верно — и не выдай спешки.",
                                       "In exactly three weeks. The Order will hoard its power until the final night. Prepare slowly but surely — and don't betray any haste.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 21 — Смотритель погоста: мёртвые помнят
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 21, coinsPerCorrectOrder = 30,
            dayEndText   = "Мёртвые у Ущелья помнят имя главы Ордена. Это женщина со шрамом-молнией. Та самая Лира.",
            dayEndTextEn = "The dead by the Gorge remember the name of the Order's head. It is the woman with the lightning scar. The very same Lira.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.GraveWarden, stickmanIndex = 4,
                    order = Order(CoffeeType.BlackCoffee, Volume.Large),
                    greetingLines = L(
                        ("Смотритель погоста", "Чёрный. Большой. Чёрный как земля, в которой я работаю.",
                                               "Black. Large. Black as the earth I work in."),
                        ("Мира",               "Вы со старого погоста у Ущелья?",
                                               "You're from the old graveyard by the Gorge?")
                    ),
                    wrongOrderLines = L(
                        ("Смотритель погоста", "Большой чёрный кофе. Без всего.",
                                               "A large black coffee. Nothing in it.")
                    ),
                    storyRevealLines = L(
                        ("Смотритель погоста", "Мёртвые шепчут по ночам. Они помнят, кто привёл к ним столько новых соседей. Имя главы Ордена — на устах у праха: женщина со шрамом-молнией.",
                                               "The dead whisper at night. They remember who brought them so many new neighbours. The name of the Order's head is on the lips of the dust: the woman with the lightning scar.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 22 — Пасечник: мёд правды
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 22, coinsPerCorrectOrder = 30,
            dayEndText   = "Мёд правды слаще лунного серебра и не так заметен. Им можно тихо проверить, нет ли среди завсегдатаев шпиона Ордена.",
            dayEndTextEn = "Truth honey is sweeter than moon silver and far less obvious. With it you can quietly check whether a regular is an Order spy.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Beekeeper, stickmanIndex = 5,
                    order = Order(CoffeeType.GreenTea, Volume.Medium, SweetnessLevel.High, Topping.Caramel),
                    greetingLines = L(
                        ("Пасечник",   "Зелёный, послаще, и с карамелью. Сладкое к сладкому.",
                                       "Green tea, sweeter, with caramel. Sweet to sweet."),
                        ("Мира",       "У вас руки пахнут воском.",
                                       "Your hands smell of beeswax.")
                    ),
                    wrongOrderLines = L(
                        ("Пасечник",   "Зелёный чай, очень сладкий, с карамелью.",
                                       "Green tea, very sweet, with caramel.")
                    ),
                    storyRevealLines = L(
                        ("Пасечник",   "(Ставит баночку мёда.) Капни в чай тому, кому не веришь. Солжёт — мёд загустеет до камня. Тише лунного серебра, а правды в нём не меньше.",
                                       "(Sets down a jar of honey.) Drop it into the tea of one you don't trust. If they lie — the honey thickens to stone. Quieter than moon silver, and no less truthful.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 23 — Эхо: близнец из зеркального мира
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 23, coinsPerCorrectOrder = 30,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.CameraShake,
            vignetteText   = "Зеркала в кофейне вздрогнули все разом. На миг в каждом отразился Кай — он машет тебе и беззвучно повторяет одно слово: «Скоро».",
            vignetteTextEn = "Every mirror in the coffee house shuddered at once. For an instant Kai was reflected in each — waving to you, silently mouthing one word: 'Soon.'",
            dayEndText   = "Эхо-близнец предупреждает: Орден научился смотреть сквозь твои зеркала. Завесь их на ночь ритуала.",
            dayEndTextEn = "The Echo twin warns: the Order has learned to look through your mirrors. Cover them on the night of the ritual.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.EchoTwin, stickmanIndex = 6,
                    order = Order(CoffeeType.Cappuccino),
                    greetingLines = L(
                        ("Эхо",   "Капучино. Я пью его и по ту сторону зеркала — в один и тот же миг.",
                                  "A cappuccino. I drink it on the other side of the mirror too — at the very same moment."),
                        ("Мира",  "Вы… отражение?",
                                  "Are you… a reflection?")
                    ),
                    wrongOrderLines = L(
                        ("Эхо",   "Капучино. Средний. Как всегда — по обе стороны.",
                                  "A cappuccino. Medium. As always — on both sides.")
                    ),
                    storyRevealLines = L(
                        ("Эхо",   "Орден подсматривает сквозь твои зеркала. Я вижу их глаза с изнанки. В ночь ритуала завесь зеркала — или они увидят твою ловушку заранее.",
                                  "The Order is peering through your mirrors. I see their eyes from the underside. On the night of the ritual, cover the mirrors — or they'll see your trap in advance.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 24 — Бард: забытый куплет Песни Якоря
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 24, coinsPerCorrectOrder = 35,
            dayEndText   = "Бард вспомнил забытый куплет «Песни Якоря». Теперь ты знаешь песню целиком — её сила выросла втрое.",
            dayEndTextEn = "The Bard recalled a forgotten verse of the Anchor Song. Now you know the whole song — its power has trebled.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Bard, stickmanIndex = 7,
                    order = Order(CoffeeType.Mocha, Volume.Large, SweetnessLevel.Medium),
                    greetingLines = L(
                        ("Бард",  "Мокко, большой, в меру сладкий. Голосу нужна и горечь, и сладость.",
                                  "A mocha, large, moderately sweet. A voice needs both bitterness and sweetness."),
                        ("Мира",  "Вы поёте старые песни. Знаете колыбельную про якорь?",
                                  "You sing old songs. Do you know the lullaby about an anchor?")
                    ),
                    wrongOrderLines = L(
                        ("Бард",  "Большой мокко, средне сладкий. Для горла.",
                                  "A large mocha, medium sweet. For the throat.")
                    ),
                    storyRevealLines = L(
                        ("Бард",  "(Напевает.) У твоей колыбельной был третий куплет — его пели только Хранители Границ. Слушай и запомни: он скрепляет то, что первые два лишь успокаивают.",
                                  "(Hums.) Your lullaby had a third verse — only the Border Keepers ever sang it. Listen and remember: it binds what the first two only soothe.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 25 — Гадалка: предсказание предательства
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 25, coinsPerCorrectOrder = 35,
            dayEndText   = "Карты легли дурно: среди тех, кому ты доверяешь, есть лазутчик. Пора проверить завсегдатаев мёдом правды.",
            dayEndTextEn = "The cards fell ill: among those you trust there is an informant. Time to test the regulars with the truth honey.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Cartomancer, stickmanIndex = 8,
                    order = Order(CoffeeType.Americano, Volume.Medium),
                    greetingLines = L(
                        ("Гадалка", "Американо. Чёрный, как закрытая карта. Сядь, я раскину для тебя.",
                                    "Americano. Black as a face-down card. Sit, I'll lay them out for you."),
                        ("Мира",    "Я не люблю гаданий.",
                                    "I don't care for fortunes.")
                    ),
                    wrongOrderLines = L(
                        ("Гадалка", "Средний американо. И не спорь с картами.",
                                    "A medium americano. And don't argue with the cards.")
                    ),
                    storyRevealLines = L(
                        ("Гадалка", "(Переворачивает карту с тремя кругами.) Близкий стол. Знакомое лицо. Кто-то из твоих завсегдатаев носит весть Ордену. Найди его прежде, чем он найдёт твою тайну.",
                                    "(Turns over a card with three circles.) A nearby table. A familiar face. One of your regulars carries word to the Order. Find them before they find your secret.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 26 — Фонарщик: новый Чёрный Фонарь
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 26, coinsPerCorrectOrder = 35,
            dayEndText   = "Фонарщик может выковать новый Чёрный Фонарь взамен украденного — но ему нужно негасимое пламя огненного алхимика.",
            dayEndTextEn = "The Lamplighter can forge a new Black Lantern to replace the stolen one — but he needs the Fire Alchemist's undying flame.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Lamplighter, stickmanIndex = 0,
                    order = Order(CoffeeType.HotChocolate, Volume.Medium, SweetnessLevel.High),
                    greetingLines = L(
                        ("Фонарщик", "Горячий шоколад, сладкий. Я грею ладони перед работой с огнём.",
                                     "Hot chocolate, sweet. I warm my palms before working with fire."),
                        ("Мира",     "Вы зажигаете фонари. А Чёрный Фонарь сможете?",
                                     "You light lanterns. Could you make a Black one?")
                    ),
                    wrongOrderLines = L(
                        ("Фонарщик", "Сладкий горячий шоколад, средний.",
                                     "A sweet hot chocolate, medium.")
                    ),
                    storyRevealLines = L(
                        ("Фонарщик", "Чёрный Фонарь показывает места силы Ордена. Я выкую новый — но нужно негасимое пламя. Попроси огненного алхимика; он у тебя в долгу.",
                                     "The Black Lantern reveals the Order's places of power. I'll forge a new one — but I need an undying flame. Ask the Fire Alchemist; he is in your debt.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 27 — Контрабандист: поставки Ордена
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 27, coinsPerCorrectOrder = 35,
            dayEndText   = "Контрабандист сдал маршрут поставок Ордена. Их «якорные камни» везут к Утёсу — можно перехватить и ослабить ритуал.",
            dayEndTextEn = "The Smuggler gave up the Order's supply route. Their 'anchor stones' are being hauled to the Cliff — you can intercept them and weaken the ritual.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Smuggler, stickmanIndex = 1,
                    order = Order(CoffeeType.Americano, Volume.Large, SweetnessLevel.Low),
                    greetingLines = L(
                        ("Контрабандист", "Большой американо, чуть сладкий. И тихий угол, если можно.",
                                          "A large americano, a touch sweet. And a quiet corner, if you can."),
                        ("Мира",          "Вы возите грузы по ту сторону границ. Возили и для Ордена?",
                                          "You haul cargo across the borders. Did you haul for the Order too?")
                    ),
                    wrongOrderLines = L(
                        ("Контрабандист", "Большой американо, слегка сладкий.",
                                          "A large americano, lightly sweet.")
                    ),
                    storyRevealLines = L(
                        ("Контрабандист", "Возил. Каюсь. Их «якорные камни» идут к Утёсу через брод у трёх ив. Перехвати обоз — и у них не хватит силы на полный разрыв.",
                                          "I did. I confess. Their anchor stones move to the Cliff through the ford by the three willows. Intercept the convoy — and they won't have the strength for a full rending.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 28 — Перебежчик Ордена: взгляд изнутри
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 28, coinsPerCorrectOrder = 35,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.DarknessFlash,
            vignetteText   = "Ночью к двери приколот кинжалом лист: круг, перечёркнутый трещиной, и слова «Мы знаем про твой стол союзников». Орден показывает, что следит.",
            vignetteTextEn = "At night a sheet is pinned to the door with a dagger: a circle crossed by a crack, and the words 'We know about your table of allies.' The Order is showing it watches.",
            dayEndText   = "Перебежчик раскрыл слабое место ритуала: в миг затмения нужно оборвать «Песнь Разрыва» Лиры своей «Песней Якоря».",
            dayEndTextEn = "The Defector revealed the ritual's weak point: at the moment of eclipse you must cut off Lira's 'Song of Rending' with your 'Anchor Song'.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Defector, stickmanIndex = 2,
                    order = Order(CoffeeType.TruthBrew, Volume.Small),
                    greetingLines = L(
                        ("Перебежчик", "(Прячет ладони с тремя кругами.) Сделай мне «Кофе Правды». Я хочу, чтобы ты поверила каждому моему слову.",
                                       "(Hides palms marked with three circles.) Make me the Truth Brew. I want you to believe every word I say."),
                        ("Мира",       "Вы из Ордена.",
                                       "You're one of the Order.")
                    ),
                    wrongOrderLines = L(
                        ("Перебежчик", "Маленький «Кофе Правды». И слушай внимательно.",
                                       "A small Truth Brew. And listen carefully.")
                    ),
                    storyRevealLines = L(
                        ("Перебежчик", "(Пьёт. Голос ровный — не лжёт.) Был из Ордена. Лира поёт «Песнь Разрыва» в миг затмения. Оборви её своей песней — и ритуал обратится против них. Но петь надо точно вовремя.",
                                       "(Drinks. His voice is steady — no lie.) I was. Lira sings the Song of Rending at the moment of eclipse. Cut it off with your song — and the ritual turns against them. But you must sing at the exact moment.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 29 — Вдова: решимость
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 29, coinsPerCorrectOrder = 35,
            dayEndText   = "Вдова потеряла мужа из-за Ордена много лет назад. Она научила тебя одному: страх — это просто туман. Сквозь него можно идти.",
            dayEndTextEn = "The Widow lost her husband to the Order years ago. She taught you one thing: fear is just fog. You can walk through it.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Widow, stickmanIndex = 3,
                    order = Order(CoffeeType.HerbalTea, Volume.Medium),
                    greetingLines = L(
                        ("Вдова", "Травяной. Средний. Как тот, что я заваривала ему каждое утро. Двадцать лет назад.",
                                  "Herbal. Medium. Like the one I brewed him every morning. Twenty years ago."),
                        ("Мира",  "Его забрал Орден?",
                                  "The Order took him?")
                    ),
                    wrongOrderLines = L(
                        ("Вдова", "Средний травяной чай. Просто травяной.",
                                  "A medium herbal tea. Just herbal.")
                    ),
                    storyRevealLines = L(
                        ("Вдова", "Да. Я не успела. А ты ещё можешь. Не дай страху тебя остановить, девочка. Страх — это туман. Сквозь туман идут.",
                                  "Yes. I was too late. But you still have time. Don't let fear stop you, girl. Fear is fog. You walk through fog.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 30 — Стеклодув: резонансные сосуды
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 30, coinsPerCorrectOrder = 35,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.CameraShake,
            vignetteText   = "Полнолуние пошло на убыль — до затмения считаные ночи. В кофейне сами собой зазвенели стёкла, будто репетируя.",
            vignetteTextEn = "The full moon is waning — only a few nights until the eclipse. The glass in the coffee house rang of its own accord, as if rehearsing.",
            dayEndText   = "Стеклодув выдул сосуды, что поют в лад с «Песней Якоря». Расставь их в кофейне — и песня станет в десять раз сильнее.",
            dayEndTextEn = "The Glassblower blew vessels that sing in tune with the Anchor Song. Place them around the coffee house — and the song will be tenfold stronger.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Glassblower, stickmanIndex = 4,
                    order = Order(CoffeeType.Water, Volume.Large),
                    greetingLines = L(
                        ("Стеклодув", "Большую воду, чистую. Стеклу нужна вода, а мне — после печи.",
                                      "A large water, pure. Glass needs water, and so do I after the furnace."),
                        ("Мира",      "Вы делаете поющее стекло?",
                                      "You make singing glass?")
                    ),
                    wrongOrderLines = L(
                        ("Стеклодув", "Большой стакан воды. Просто воды.",
                                      "A large glass of water. Just water.")
                    ),
                    storyRevealLines = L(
                        ("Стеклодув", "(Ставит на стойку тонкие сосуды.) Они поют в лад с твоей песней. Расставь их по залу — и в ночь затмения голос твой умножится стократ.",
                                      "(Sets thin vessels on the counter.) They sing in tune with your song. Place them around the room — and on the eclipse night your voice will be multiplied a hundredfold.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 31 — Бабушка: откуда колыбельная
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 31, coinsPerCorrectOrder = 40,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.BrightRestore,
            vignetteText   = "Тебе снится детство: отец поёт колыбельную, а его ладони светятся тёплым светом, и границы мира послушно смыкаются. Ты из рода Хранителей.",
            vignetteTextEn = "You dream of childhood: your father sings the lullaby, his palms glowing with warm light, and the borders of the world obediently close. You are of the Keepers' blood.",
            dayEndText   = "Бабушка открыла правду: твой род — Хранители Границ. «Песня Якоря» — твоё наследство. Орден боялся не Кая, а тебя.",
            dayEndTextEn = "Grandmother revealed the truth: your family are Border Keepers. The Anchor Song is your inheritance. The Order feared not Kai, but you.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Grandmother, stickmanIndex = 5,
                    order = Order(CoffeeType.HerbalTea, Volume.Small, SweetnessLevel.Low, Topping.Cinnamon),
                    greetingLines = L(
                        ("Бабушка", "Маленький травяной, чуть сладкий, с корицей. Как варила твоя мать. Я узнала бы этот запах где угодно.",
                                    "A small herbal, a touch sweet, with cinnamon. The way your mother made it. I'd know that scent anywhere."),
                        ("Мира",    "Вы… знали мою мать?",
                                    "You… knew my mother?")
                    ),
                    wrongOrderLines = L(
                        ("Бабушка", "Маленький травяной, слегка сладкий, с корицей. Дитя, ты должна это помнить.",
                                    "A small herbal, lightly sweet, with cinnamon. Child, you ought to remember this.")
                    ),
                    storyRevealLines = L(
                        ("Бабушка", "Твой род — Хранители Границ. Песня — ваша кровь, не выученное ремесло. Орден забрал Кая, чтобы выманить тебя: ты сильнее, чем думаешь. Пой смело.",
                                    "Your line are Border Keepers. The song is your blood, not a learned craft. The Order took Kai to lure you out: you are stronger than you know. Sing without fear.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 32 — Огненный алхимик: негасимое пламя
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 32, coinsPerCorrectOrder = 40,
            dayEndText   = "Алхимик дал негасимое пламя для нового Чёрного Фонаря. И предупредил: Орден ускорил ритуал — у тебя меньше времени, чем думал Часовщик.",
            dayEndTextEn = "The Alchemist gave the undying flame for the new Black Lantern. And warned: the Order has hastened the ritual — you have less time than the Clockkeeper thought.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.FireAlchemist, stickmanIndex = 6,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Огненный алхимик", "Снова крепкий эспрессо, маленький. Я принёс то, что просил Фонарщик.",
                                             "A strong espresso again, small. I've brought what the Lamplighter asked for."),
                        ("Мира",             "Негасимое пламя?",
                                             "The undying flame?")
                    ),
                    wrongOrderLines = L(
                        ("Огненный алхимик", "Маленький крепкий эспрессо.",
                                             "A small, strong espresso.")
                    ),
                    storyRevealLines = L(
                        ("Огненный алхимик", "(Ставит фонарик с живым огнём.) Держи. И поспеши: Орден ускорил ритуал — затмение они «приблизят» зеркалами. Готовь ловушку раньше.",
                                             "(Sets down a lantern with a living flame.) Take it. And hurry: the Order has hastened the ritual — they'll 'bring the eclipse closer' with mirrors. Set your trap sooner.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 33 — Хранитель книг: страница «Скрижалей»
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 33, coinsPerCorrectOrder = 40,
            dayEndText   = "На уцелевшей странице «Скрижалей» — рецепт «Якоря»: напиток-обряд, что запечатывает границы. Его варят под «Песню Якоря».",
            dayEndTextEn = "On the surviving page of the Tablets — the recipe for the Anchor: a ritual drink that seals the borders. It is brewed to the Anchor Song.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.BookKeeper, stickmanIndex = 7,
                    order = Order(CoffeeType.BlackCoffee, Volume.Medium),
                    greetingLines = L(
                        ("Хранитель книг", "Чёрный кофе, средний. И тише — то, что я несу, стоит библиотеки.",
                                           "Black coffee, medium. And softly — what I carry is worth a library."),
                        ("Мира",           "Вы нашли страницу?",
                                           "You found a page?")
                    ),
                    wrongOrderLines = L(
                        ("Хранитель книг", "Средний чёрный кофе, без ничего.",
                                           "A medium black coffee, nothing in it.")
                    ),
                    storyRevealLines = L(
                        ("Хранитель книг", "(Разглаживает обугленный лист.) Рецепт «Якоря». Его не пьют — им запечатывают мир. Сваришь его на месте трёх теней, под свою песню — и разрыв станет швом.",
                                           "(Smooths a charred sheet.) The recipe for the Anchor. You don't drink it — you seal the world with it. Brew it at the place of three shadows, to your song — and the rift becomes a seam.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 34 — Теневой торговец: последняя цена
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 34, coinsPerCorrectOrder = 40,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.VisionLoss,
            vignetteText   = "Отдав воспоминание о первом дне с Каем, ты на миг слепнешь от горя — а потом видишь ясно: обсерватория, три тени, и Кай в цепях ждёт рассвета.",
            vignetteTextEn = "Giving up the memory of your first day with Kai, you go blind with grief for a moment — then see clearly: the observatory, three shadows, and Kai in chains awaiting dawn.",
            dayEndText   = "Торговец назвал точное место: водонапорная башня под обсерваторией. Цена — ещё одно драгоценное воспоминание. Ты заплатила.",
            dayEndTextEn = "The Merchant named the exact place: the water tower beneath the observatory. The price — another precious memory. You paid it.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.ShadowMerchant, stickmanIndex = 8,
                    order = Order(CoffeeType.Americano, Volume.Small),
                    greetingLines = L(
                        ("Теневой торговец", "Маленький американо. Ты близко, я чувствую. И снова пришла за ценой.",
                                             "A small americano. You're close, I can feel it. And you've come for a price again."),
                        ("Мира",             "Назовите место. Точно.",
                                             "Name the place. Exactly.")
                    ),
                    wrongOrderLines = L(
                        ("Теневой торговец", "Маленький американо. Чёрный.",
                                             "A small americano. Black.")
                    ),
                    storyRevealLines = L(
                        ("Теневой торговец", "Водонапорная башня под обсерваторией. Там держат Кая. Цена — самое тёплое твоё воспоминание о нём. (Берёт флакон света.) Больно? Это и значит, что оно настоящее.",
                                             "The water tower beneath the observatory. That's where Kai is held. The price — your warmest memory of him. (Takes a vial of light.) Does it hurt? That's how you know it was real.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 35 — Водяной страж и лунный кузнец: шпион пойман
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 35, coinsPerCorrectOrder = 40,
            dayEndText   = "Мёд правды и лунное серебро выдали лазутчика среди завсегдатаев. Орден ослеп — больше они не знают твоих планов.",
            dayEndTextEn = "The truth honey and moon silver exposed the informant among the regulars. The Order is blind now — they no longer know your plans.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.WaterGuard, stickmanIndex = 0,
                    order = Order(CoffeeType.Water, Volume.Medium),
                    greetingLines = L(
                        ("Водяной страж", "Воды, средней. Сегодня вода тревожна — кто-то в зале лжёт.",
                                          "Water, medium. The water is uneasy today — someone in the room is lying."),
                        ("Мира",          "Я знаю, кого проверить.",
                                          "I know who to test.")
                    ),
                    wrongOrderLines = L(
                        ("Водяной страж", "Средний стакан воды. Чистой.",
                                          "A medium glass of water. Pure.")
                    ),
                    storyRevealLines = L(
                        ("Мира",          "(Капает мёд правды в чай тихого завсегдатая. Мёд каменеет.) Вот и шпион.",
                                          "(Drips truth honey into a quiet regular's tea. The honey turns to stone.) There's the spy."),
                        ("Водяной страж", "Теперь Орден слеп. Они не узнают, что мы готовим, пока не станет поздно.",
                                          "Now the Order is blind. They won't learn what we're preparing until it's too late.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 36 — Странник: Орден ударит первым
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 36, coinsPerCorrectOrder = 40,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.DarknessFlash,
            vignetteText   = "Среди ночи свет мигает трижды. На стойке — снова камень с тремя кругами, ещё тёплый. Они были здесь. Они придут не к Утёсу. Они придут сюда.",
            vignetteTextEn = "In the dead of night the lights blink thrice. On the counter — again a stone with three circles, still warm. They were here. They won't go to the Cliff. They'll come here.",
            dayEndText   = "Орден не станет ждать затмения у Утёса — он придёт в кофейню, чтобы забрать тебя. Значит, ловушку ставим дома.",
            dayEndTextEn = "The Order won't wait for the eclipse at the Cliff — it will come to the coffee house to take you. So the trap will be set at home.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler, stickmanIndex = 1,
                    order = Order(CoffeeType.HerbalTea),
                    greetingLines = L(
                        ("Странник", "(Глаза снова ясные, голос тихий.) Травяной. Я вырвался из-под их власти и пришёл предупредить.",
                                     "(His eyes clear again, his voice low.) Herbal tea. I broke free of their hold and came to warn you."),
                        ("Мира",     "Что вы знаете?",
                                     "What do you know?")
                    ),
                    wrongOrderLines = L(
                        ("Странник", "Травяной чай. Слушай быстро.",
                                     "Herbal tea. Listen quickly.")
                    ),
                    storyRevealLines = L(
                        ("Странник", "Они не пойдут к Утёсу. Лира приведёт Орден прямо в твою кофейню — в ночь затмения. Хотят забрать Хранительницу живой. Готовь ловушку здесь.",
                                     "They won't go to the Cliff. Lira will bring the Order straight to your coffee house — on the eclipse night. They want to take the Keeper alive. Set your trap here.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 37 — Союзники собираются: ночь близко
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 37, coinsPerCorrectOrder = 45,
            dayEndText   = "Поющее стекло расставлено, зеркала завешены, Фонарь горит. Союзники здесь. Затмение — этой ночью. Орден уже в пути.",
            dayEndTextEn = "The singing glass is placed, the mirrors covered, the Lantern lit. The allies are here. The eclipse is tonight. The Order is already on its way.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.CrystalSinger, stickmanIndex = 2,
                    order = Order(CoffeeType.Latte, Volume.Medium, SweetnessLevel.Medium, Topping.Cinnamon),
                    greetingLines = L(
                        ("Мира",                    "(Расставляет поющие сосуды.) Латте с корицей — тебе, чтобы голос звенел чисто. Сегодня ты ведёшь хор.",
                                                    "(Sets out the singing vessels.) A latte with cinnamon — for you, so your voice rings clear. Tonight you lead the choir."),
                        ("Кристаллическая певица",   "Стекло уже дрожит в ожидании. Затмение на пороге. Я слышу, как сходятся три тени.",
                                                    "The glass is already trembling in anticipation. The eclipse is at the threshold. I hear the three shadows gathering.")
                    ),
                    wrongOrderLines = L(
                        ("Кристаллическая певица",   "Латте, средне сладкий, с корицей. Для голоса.",
                                                    "A latte, medium sweet, with cinnamon. For the voice.")
                    ),
                    storyRevealLines = L(
                        ("Мира",                    "(Разносит кофе союзникам.) Зеркала завешены, стекло поёт, Фонарь зажжён. Сегодня в полночь они придут. Мы готовы.",
                                                    "(Serves coffee to the allies.) The mirrors are covered, the glass sings, the Lantern is lit. They come tonight at midnight. We are ready.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 38 — НОЧНОЙ БОЙ В КОФЕЙНЕ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 38, coinsPerCorrectOrder = 0,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.DarknessFlash,
            vignetteText   = "Полночь. Гаснет свет. В дверях — три фигуры. Лира с шрамом-молнией. Союзники активируют ловушки. Мира напевает «Песню Якоря». Посуда резонирует. Люди Ордена корчатся. Лира падает. «В старой водонапорной башне. На Граничном Утёсе. Ключ у меня.»",
            vignetteTextEn = "Midnight. The lights go out. Three figures in the doorway. Lira with the lightning scar. The allies trigger the traps. Mira hums the Anchor Song. The glassware resonates. The Order's men writhe. Lira falls. 'In the old water tower. On the Border Cliff. I have the key.'",
            dayEndText   = "Орден обезврежен. Ключ-амулет получен. Кай — в водонапорной башне на Граничном Утёсе.",
            dayEndTextEn = "The Order is defeated. The key amulet is yours. Kai is in the water tower on the Border Cliff.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Lira, stickmanIndex = 8,
                    order = Order(CoffeeType.TruthBrew, Volume.Large),
                    greetingLines = L(
                        ("Лира", "Мира. Ты не послушалась. Теперь мы заберём и тебя. Твой дар предвидения в напитках тоже полезен.",
                                 "Mira. You did not listen. Now we will take you too. Your gift of foresight in drinks is also useful.")
                    ),
                    wrongOrderLines = L(
                        ("Лира", "(Молча ждёт)",
                                 "(Waits in silence)")
                    ),
                    storyRevealLines = L(
                        ("Мира",  "Мой дар только подсказывает, какой кофе вам подойдёт. А вам подойдёт «Кофе Правды». (Ставит большой сервиз)",
                                  "My gift only tells me which coffee suits you. And what suits you is the Truth Brew. (Sets down a large serving)"),
                        ("Лира",  "Довольно! (Разбивает зеркало рукой)",
                                  "Enough! (Smashes a mirror with her hand)"),
                        ("Мира",  "(Напевает «Песню Якоря». Стеклянная посуда резонирует. Люди Ордена корчатся от боли.)",
                                  "(Hums the Anchor Song. The glassware resonates. The Order's men writhe in pain.)"),
                        ("Лира",  "(Сдавленно.) Ты... не знаешь, что делаешь...",
                                  "(Strained.) You... don't know what you're doing..."),
                        ("Мира",  "Знаю. Где Кай?",
                                  "I do. Where is Kai?"),
                        ("Лира",  "В... в старой водонапорной башне. На Граничном Утёсе. Ключ... у меня. (Падает)",
                                  "In... in the old water tower. On the Border Cliff. The key... I have it. (Falls)")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 39 — СПАСЕНИЕ КАЯ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 39, coinsPerCorrectOrder = 0,
            hasVignette = true,
            vignetteEffect = VignetteEffectType.BrightRestore,
            vignetteText   = "Мира находит башню. Поёт «Песню Якоря». Замок открывается. Кай внутри, прикован магическими цепями. «Мира... я знал, что ты найдёшь мелодию.» Ключ-амулет рассыпает цепи в пыль. «Ритуал можно обратить вспять. Используй амулет и песню в месте трёх теней.»",
            vignetteTextEn = "Mira finds the tower. She sings the Anchor Song. The lock opens. Kai is inside, bound in magic chains. 'Mira... I knew you would find the melody.' The key amulet turns the chains to dust. 'The ritual can be reversed. Use the amulet and the song at the place of three shadows.'",
            dayEndText   = "Кай свободен! Нужно обратить ритуал — в обсерватории. Завтра — последний день.",
            dayEndTextEn = "Kai is free! The ritual must be reversed — at the observatory. Tomorrow is the final day.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler, stickmanIndex = 0,
                    order = Order(CoffeeType.HerbalTea),
                    greetingLines = L(
                        ("Странник", "(Приходит. Глаза снова ясные.) Прости. Мной управляли. Я проведу тебя безопасной тропой к башне.",
                                     "(Arrives. His eyes are clear again.) Forgive me. I was being controlled. I will lead you to the tower by a safe path.")
                    ),
                    wrongOrderLines = L(
                        ("Странник", "Нам нужно идти. Но сначала... чай.",
                                     "We need to go. But first... tea.")
                    ),
                    storyRevealLines = L(
                        ("Странник", "Дорога свободна. Пора за Каем.",
                                     "The road is clear. Time to go get Kai.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 40 — ЭПИЛОГ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 40, coinsPerCorrectOrder = 50,
            dayEndText   = "«Якорь» приготовлен, границы запечатаны. Но за стойкой пусто, а касса пуста. Кая больше нет — было лишь эхо.",
            dayEndTextEn = "The Anchor is brewed, the borders sealed. But the counter is empty, and so is the till. Kai is gone — there was only an echo.",
            customers = new List<DayCustomerEntry>
            {
                new DayCustomerEntry
                {
                    characterType = CharacterType.Andrei, stickmanIndex = 7,
                    order = Order(CoffeeType.Espresso, Volume.Small),
                    greetingLines = L(
                        ("Мира",          "Добро пожаловать! Что для вас?",
                                          "Welcome! What can I get you?"),
                        ("Кай",           "(Стоит за стойкой, помогает. На нём фартук, медальона нет.) Сегодня фирменный напиток — «Якорь». Крепкий, сладкий и дающий чувство защищённости.",
                                          "(Stands behind the counter, helping. He wears an apron; the medallion is gone.) Today's signature drink is the Anchor. Strong, sweet, and it makes you feel safe."),
                        ("Водяной страж", "Границы успокоились. Вода снова чиста.",
                                          "The borders have calmed. The water is pure again."),
                        ("Хранитель книг","Книги возвращаются. Знание сохранено.",
                                          "The books are returning. Knowledge is preserved."),
                        ("Зеркальный вор","Отражения стали честнее. (Кивнув Лире.) Некоторые нашли покой.",
                                          "Reflections have become more honest. (Nods to Lira.) Some have found peace.")
                    ),
                    wrongOrderLines = L(
                        ("Кай", "Эспрессо. Маленький. Я помню, ты умеешь его готовить.",
                                "Espresso. Small. I remember you know how to make it.")
                    ),
                    storyRevealLines = L(
                        ("Мира",     "(Поднимает чашку.) За «Междумирье». За дом.",
                                     "(Raises her cup.) To the Inbetween. To home."),
                        ("Все",      "За дом!",
                                     "To home!"),
                        ("...",      "(В чашках у всех на секунду отражается спокойный пейзаж их родного мира. Потом всё возвращается в норму.)",
                                     "(For a second, every cup reflects the calm landscape of its owner's home world. Then everything returns to normal.)"),
                        // ── Мрачная кода (пункт 2.1): спасение было, но цена — горькая ──
                        ("Теневой торговец", "(У самой двери, тихо.) Мира… пока ты не привыкла к счастью. Тот, кто стоит за твоей стойкой, — не Кай. Настоящий Кай погиб ещё в ту первую ночь у Ущелья.",
                                             "(At the door, quietly.) Mira… before you grow used to happiness. The one standing behind your counter is not Kai. The real Kai died that very first night by the Gorge."),
                        ("Мира",     "Нет. Я слышала его голос. Читала его письма. Я его спасла.",
                                     "No. I heard his voice. I read his letters. I saved him."),
                        ("Теневой торговец", "Эхо. Орден оставил тебе эхо, чтобы ты перестала искать. А десять тысяч, что ты копила на дорогу за ним, — он забрал их этой ночью и ушёл.",
                                             "An echo. The Order left you an echo so you'd stop searching. And the ten thousand you saved for the road after him — he took it all this night and left."),
                        ("Мира",     "(Оборачивается — за стойкой пусто. Касса пуста.) …Значит, всё это время я варила кофе призраку.",
                                     "(She turns — the counter is empty. The till is empty.) …So all this time I was brewing coffee for a ghost.")
                    )
                }
            }
        });

        // ── Пункты 6,7: гарантируем минимум 3 посетителя в день ──────────────
        PadDaysToMinimumCustomers(3);
    }

    // Добивает каждый день до minCount гостей «завсегдатаями» со своим заказом.
    private void PadDaysToMinimumCustomers(int minCount)
    {
        // Имена-завсегдатаи (флавор; механика — случайный заказ)
        (string ru, string en)[] regulars =
        {
            ("Завсегдатай", "Regular"),
            ("Путник",      "Traveller"),
            ("Горожанка",   "Townswoman"),
            ("Старый маг",  "Old Mage"),
            ("Подмастерье", "Apprentice"),
        };
        CoffeeType[] types     = (CoffeeType[])System.Enum.GetValues(typeof(CoffeeType));
        Volume[]     volumes   = (Volume[])System.Enum.GetValues(typeof(Volume));
        SweetnessLevel[] sweets= (SweetnessLevel[])System.Enum.GetValues(typeof(SweetnessLevel));
        Topping[]    toppings  = (Topping[])System.Enum.GetValues(typeof(Topping));

        // Пул шуток-мемов про кофейни (пункт 6): тасуем и выдаём без повторов,
        // часть гостей оставляем без шуток. Шутки — только завсегдатаям, чтобы не
        // ломать серьёзные сюжетные сцены с именованными персонажами.
        var jokePool = BuildJokePool();
        var jokeBag  = new List<int>();
        var jokeRng  = new System.Random(20240614);
        System.Func<List<DialogueLine>> NextJoke = () =>
        {
            if (jokeBag.Count == 0)
            {
                for (int i = 0; i < jokePool.Count; i++) jokeBag.Add(i);
                for (int i = jokeBag.Count - 1; i > 0; i--)
                {
                    int j = jokeRng.Next(i + 1);
                    int tmp = jokeBag[i]; jokeBag[i] = jokeBag[j]; jokeBag[j] = tmp;
                }
            }
            int idx = jokeBag[0]; jokeBag.RemoveAt(0);
            // Возвращаем КОПИЮ строк шутки (свой экземпляр для каждого гостя).
            var src = jokePool[idx];
            var copy = new List<DialogueLine>(src.Count);
            foreach (var l in src)
                copy.Add(new DialogueLine { speakerName = l.speakerName, speakerNameEn = l.speakerNameEn, text = l.text, textEn = l.textEn });
            return copy;
        };

        foreach (var day in days)
        {
            int seed = day.dayNumber * 7919;
            var rng = new System.Random(seed);

            while (day.customers.Count < minCount)
            {
                var reg = regulars[rng.Next(regulars.Length)];
                var order = new CoffeeOrder
                {
                    type    = types[rng.Next(types.Length)],
                    volume  = volumes[rng.Next(volumes.Length)],
                    sweet   = sweets[rng.Next(sweets.Length)],
                    topping = rng.Next(2) == 0 ? Topping.None : toppings[rng.Next(toppings.Length)]
                };

                // ~60% завсегдатаев приходят с шуткой (остальные — без, пункт 6).
                // Шутка САМА служит приветствием (заканчивается заказом), поэтому
                // обычную фразу «Доброго дня…» к ней НЕ добавляем — иначе гость
                // здоровался бы дважды (пункт 3).
                List<DialogueLine> greeting;
                if (rng.Next(100) < 60)
                    greeting = NextJoke();
                else
                    greeting = L(
                        (reg.ru, "Доброго дня! Налей мне что-нибудь по вкусу.",
                                 "Good day! Pour me something to my taste."));

                day.customers.Add(new DayCustomerEntry
                {
                    characterType = CharacterType.Traveler,
                    // Разные модели подряд (пункт 3): чередуем индекс по позиции и дню
                    stickmanIndex = (day.customers.Count * 4 + day.dayNumber) % 9,
                    order = order,
                    greetingLines = greeting,
                    wrongOrderLines = L(
                        (reg.ru, "Хм, это не совсем то, что я хотел...",
                                 "Hmm, that's not quite what I wanted...")),
                    storyRevealLines = L(
                        (reg.ru, "Вот это другое дело. Спасибо, хозяйка!",
                                 "Now that's better. Thank you, keeper!"))
                });
            }
        }
    }

    // Пул шуточных завязок (пункт 6): гость шутит → Мира не понимает → гость говорит
    // нормально. Реплики не повторяются подряд (раздаются из перетасованного пула).
    private static List<List<DialogueLine>> BuildJokePool()
    {
        return new List<List<DialogueLine>>
        {
            // 1. «Без кофе»
            L(
                ("Гость", "Можно мне эспрессо, но без кофе?",
                          "Can I get an espresso, but without coffee?"),
                ("Мира",  "Эспрессо… без кофе? Это будет просто горячая вода.",
                          "An espresso… without coffee? That would just be hot water."),
                ("Гость", "Хах, ладно. Тогда сделай по-нормальному.",
                          "Ha, fine. Make it the normal way then.")),

            // 2. Философ про молоко
            L(
                ("Гость", "А молоко у вас обычное? А коровье кокосовое есть?",
                          "Is your milk regular? Do you have cow's-coconut milk?"),
                ("Мира",  "Коровье… кокосовое? Такого, боюсь, не бывает.",
                          "Cow's… coconut? I'm afraid there's no such thing."),
                ("Гость", "Жаль. Ну давай как обычно делают.",
                          "A pity. Well, make it the usual way.")),

            // 3. «Как всегда»
            L(
                ("Гость", "Мне как всегда!",
                          "I'll have my usual!"),
                ("Мира",  "Но вы у нас впервые.",
                          "But it's your first time here."),
                ("Гость", "Тогда удивите меня… ладно, ладно, вот мой заказ.",
                          "Then surprise me… okay, okay, here's my order.")),

            // 4. Путаница с заведениями
            L(
                ("Гость", "Девушка, это ведь не пункт выдачи? А зажигалку можно?",
                          "Miss, this isn't a parcel pickup, is it? Can I get a lighter?"),
                ("Мира",  "Это кофейня «Междумирье».",
                          "This is the Inbetween coffee house."),
                ("Гость", "А, ну тогда, наверное, кофе.",
                          "Oh, then I suppose… coffee.")),

            // 5. Экстрасенсы и детективы
            L(
                ("Гость", "Девушка, я же просил без корицы!",
                          "Miss, I asked for no cinnamon!"),
                ("Мира",  "Вы ещё ничего не заказали.",
                          "You haven't ordered anything yet."),
                ("Гость", "А… ну тогда сейчас закажу. Без корицы.",
                          "Ah… then let me order now. No cinnamon.")),

            // 6. «Света нет»
            L(
                ("Гость", "У вас тут свет отключили?",
                          "Did your lights go out in here?"),
                ("Мира",  "Нет, это просто уютный полумрак.",
                          "No, it's just cosy dim lighting."),
                ("Гость", "А капучино хотя бы сделаете?",
                          "Could you at least still make a cappuccino?")),

            // 7. Растворимый бунтарь
            L(
                ("Гость", "А есть просто горячая вода? А кофе в ней растворить можно? У вас же кофейня.",
                          "Got just hot water? Can I dissolve coffee in it? This is a coffee house, right?"),
                ("Мира",  "Так я могу сразу сварить вам кофе.",
                          "I could just brew you the coffee directly."),
                ("Гость", "Гениально! Тогда давайте сразу.",
                          "Genius! Let's do that then.")),
        };
    }
}
