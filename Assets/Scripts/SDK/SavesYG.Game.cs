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
    }
}
