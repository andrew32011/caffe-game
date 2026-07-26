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
        public float musicVolume = 0.4f; // громкость музыки 0..1
        public float sfxVolume   = 0.8f; // громкость эффектов 0..1
        public float voiceVolume = 0.8f; // громкость «бубнёжа» героев (отдельно от эффектов) 0..1

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
    }
}
