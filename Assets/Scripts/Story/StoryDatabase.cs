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
                        ("Странник",   "Три круга... Это знак Ордена. Они ходят у Зеркального Ущелья. Будь осторожна с вопросами. (Оставляет чай недопитым, уходит)",
                                       "Three circles... That is the sign of the Order. They roam near the Mirror Gorge. Be careful with your questions. (Leaves the tea unfinished and walks out)")
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
            dayEndText   = "Союзники готовы. Туман, зеркала, звук и вода — ловушка расставлена. Завтра — полночь.",
            dayEndTextEn = "The allies are ready. Fog, mirrors, sound and water — the trap is set. Tomorrow — midnight.",
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
                        ("Мира",              "(Разносит кофе всем союзникам.) Завтра в полночь. Здесь, в кофейне.",
                                              "(Serves coffee to all the allies.) Tomorrow at midnight. Here, in the coffee house.")
                    )
                }
            }
        });

        // ═══════════════════════════════════════════════════════════════
        // ДЕНЬ 18 — НОЧНОЙ БОЙ В КОФЕЙНЕ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 18, coinsPerCorrectOrder = 0,
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
        // ДЕНЬ 19 — СПАСЕНИЕ КАЯ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 19, coinsPerCorrectOrder = 0,
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
        // ДЕНЬ 20 — ЭПИЛОГ
        // ═══════════════════════════════════════════════════════════════
        days.Add(new DayData
        {
            dayNumber = 20, coinsPerCorrectOrder = 50,
            dayEndText   = "«Якорь» приготовлен. Границы запечатаны навсегда. «Междумирье» — снова дом.",
            dayEndTextEn = "The Anchor has been brewed. The borders are sealed forever. The Inbetween is home again.",
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
                                     "(For a second, every cup reflects the calm landscape of its owner's home world. Then everything returns to normal.)")
                    )
                }
            }
        });
    }
}
