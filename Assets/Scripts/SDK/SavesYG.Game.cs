/// <summary>
/// Расширение облачного сейва Яндекс Игр (partial class SavesYG) полями нашей игры.
/// Эти поля автоматически сохраняются/загружаются плагином YG2 (локально + облако):
///   YG2.saves.<поле>  +  YG2.SaveProgress().
/// Прогресс доступен с разных устройств одного пользователя (требование 1.13.3).
/// Зависимости: PluginYG2 (модуль Storage)
/// </summary>
using System.Collections.Generic;

namespace YG
{
    public partial class SavesYG
    {
        // Деньги кофейни (старт — 100, хватает на первые ингредиенты).
        public int totalCoins = 100;

        // Текущий день (0 = обучение ещё не пройдено).
        public int currentDay = 0;

        // Пройдено ли обучение (показываем только при первом запуске).
        public bool tutorialDone = false;

        // Показана ли вступительная история (интро-канвас в SampleScene) — только 1-й вход.
        public bool introSeen = false;

        // Память удовлетворённости по уникальным клиентам (параллельные списки —
        // JsonUtility не умеет Dictionary).
        public List<int>   clientKeys = new List<int>();
        public List<float> clientSats = new List<float>();

        // ─── Батч 2: ежедневный бонус ───────────────────────────────────────
        public string dailyBonusLastDate = ""; // последняя выдача, формат yyyyMMdd
        public int    dailyBonusStreak   = 0;  // сколько дней подряд заходили

        // ─── Батч 2: продолжение посреди дня ────────────────────────────────
        public int currentCustomerIndex = 0;   // с какого гостя продолжать день

        // ─── Батч 3: апгрейды кофейни (постоянные улучшения за монеты) ───────
        public int upgBeans   = 0;  // зёрна высшего сорта (повышают оплату)
        public int upgMachine = 0;  // профи-кофемашина (шире допуск минигейма)
        public int upgLoyalty = 0;  // программа лояльности (щедрее чаевые)

        // ─── Батч 4: настройки громкости (сохраняются между сессиями) ───────
        public float musicVolume = 0.5f; // громкость музыки 0..1
        public float sfxVolume   = 0.8f; // громкость эффектов 0..1 (фактически ×SfxGain — эффекты заметно тише музыки/голосов)
        public float voiceVolume = 0.9f; // громкость «бубнёжа» героев (отдельно от эффектов) 0..1

        // ─── Батч 6: журнал гостей (коллекция «Завсегдатаи») ────────────────
        // Параллельные списки по ключу = (int)CharacterType. Независимы от clientKeys.
        public List<int> journalKeys      = new List<int>(); // какие типы гостей встречены
        public List<int> journalVisits    = new List<int>(); // сколько раз обслужен
        public List<int> journalBestStars = new List<int>(); // лучшая оценка 1..3

        // Одноразовые обучающие подсказки новых механик (id уже показанных).
        public List<string> shownTips = new List<string>();

        // ─── Сцена сна: какой день только что завершён (для текста/эффекта сна). ──
        public int sleepFromDay = 0;

        // ─── Отключение рекламы за донат (YG2 Payments, навсегда). ───────────────
        public bool adsDisabled = false;

        // ─── Бейдж журнала: сколько записей игрок уже открывал (для «новых» гостей). ─
        public int journalSeenCount = 0;

        // ─── Бесконечный режим (после дня 40, финала истории) ───────────────────
        public bool endlessMode    = false; // включён ли бесконечный режим (после финала)
        public int  endlessDay     = 0;     // текущий день бесконечного режима (1, 2, 3…)
        public int  endlessBestDay = 0;     // рекорд: самый дальний достигнутый бесконечный день

        // ─── Промпты вовлечения (показываем один раз) ───────────────────────────
        public bool shortcutAsked = false; // предложили добавить ярлык на рабочий стол (после дня 1)
        public bool reviewAsked   = false; // предложили оценить игру (после дня 3)

        // ═══ Батч 12 (удержание/экономика) ═══════════════════════════════════════

        // A: какие фичи уже открыты (по расписанию дней) — чтобы объявить разблокировку 1 раз.
        public List<string> unlockedFeatures = new List<string>();

        // B: тройная экономика — премиум-валюта и собираемые жетоны (монеты = totalCoins).
        public int gems   = 0; // кристаллы (премиум): IAP/rewarded/майлстоуны
        public int tokens = 0; // жетоны (лут/ивент): собираются с дропа, тратятся на декор/ключи

        // C: лут — ключи и таймер бесплатного сундука (unix-время следующего открытия).
        public int  keys            = 0;
        public long freeChestReady  = 0; // unix-время, когда бесплатный сундук снова готов

        // D: кастомизация (владение) — выбранный аватар и тема кофейни; купленные наборы.
        public int          avatarId = 0;
        public int          themeId  = 0;
        public List<string> ownedCustomizations = new List<string>(); // ключи купленных аватаров/тем
        public List<string> achievementsClaimed = new List<string>(); // выданные достижения

        // E: возвратный крючок — время последнего выхода (для оффлайн-дохода).
        public long lastSeenUnix = 0;

        // ═══ Батч 15: обустройство кофейни (главный сток монет + мета-прогресс) ═══
        public int renovationStage  = 0; // сколько проектов обустройства завершено
        public int renovationBanked = 0; // монет уже вложено в текущий проект (копим до цены)

        // Батч 15: ежедневная доска задач — какие из 3 задач дня уже забраны (индексы 0..2),
        // дата активной доски (yyyyMMdd) и забран ли бонус «все три».
        public string dailyTasksDate  = "";
        public List<int> dailyTasksClaimed = new List<int>();
        public bool dailyTasksBonusClaimed = false;

        // ─── Батч 15 (Фаза B): отношения / рецепты / альбом ─────────────────────
        // Опыт отношений с гостем, параллельно journalKeys (индекс = тот же гость).
        public List<int> journalRelXp = new List<int>();
        // Мастерство рецептов: параллельные списки ключ=(int)CoffeeType → сколько раз хорошо подан.
        public List<int> recipeKeys   = new List<int>();
        public List<int> recipeServed = new List<int>();
        // Забранные награды за завершённые коллекции альбома (ключи наборов).
        public List<string> albumSetsClaimed = new List<string>();

        // ─── Батч 15 (Фаза C): событие-турнир / колесо / сезонный пасс ───────────
        public string eventWeek     = "";  // ISO-неделя активного события (yyyy-Www)
        public int    eventProgress = 0;   // очки события за неделю (собранные звёзды)
        public int    eventTierClaimed = 0; // сколько наградных вех события забрано
        public string wheelLastSpin = "";  // дата последнего бесплатного спина (yyyyMMdd)
        public int    passXp        = 0;   // опыт сезонного пасса
        public List<int> passClaimed = new List<int>(); // забранные уровни пасса
        public bool   passPremium   = false; // куплен ли премиум-трек
    }
}
