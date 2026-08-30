/// <summary>
/// Централизованная отправка событий в Яндекс Метрику (через YG2.MetricaSend).
/// Одно место со всеми именами событий — чтобы имена совпадали с «Целями», заведёнными
/// в Метрике, и чтобы легко было аудировать воронку удержания.
///
/// Модуль Metrica у плагина ВКЛючается настройкой (infoYG.Metrica.enable + Counter ID),
/// а не скриптовым дефайном, поэтому YG2.MetricaSend доступен всегда, когда модуль
/// установлен. В редакторе события только логируются (см. Metrica_yg.Log).
///
/// Главная цель — понять, ГДЕ и ПОЧЕМУ уходят игроки: события расставлены по точкам
/// оттока (старт сессии, обучение, начало/конец/провал каждого дня, реклама, гейт цели,
/// вход в бесконечный режим, покупки, промпты вовлечения).
///
/// Сцена: Глобально. Зависимости: YG2 (модуль Metrica). SDK: Яндекс Метрика (WebGL).
/// </summary>
using System.Collections.Generic;
using YG;

public static class Analytics
{
    // ─── Имена событий (= идентификаторы «Целей» в Метрике, латиница без пробелов) ──
    public const string SessionStart   = "session_start";
    public const string TutorialStart  = "tutorial_start";
    public const string TutorialDone   = "tutorial_done";
    public const string DayStart       = "day_start";       // param: day, mode(story/endless)
    public const string DayComplete    = "day_complete";    // param: day, earned
    public const string DayFailed      = "day_failed";      // param: day
    public const string AdInterstitial = "ad_interstitial"; // показана межстраничная
    public const string AdRewarded     = "ad_rewarded";     // param: id
    public const string Purchase       = "purchase";        // param: product
    public const string JourneyGate    = "journey_gate";    // дошёл до гейта цели (день 40)
    public const string EndlessStart   = "endless_start";   // включён бесконечный режим
    public const string PromptShown    = "prompt_shown";    // param: which(review/shortcut)
    public const string PromptAccepted = "prompt_accepted"; // param: which(review/shortcut)
    public const string IntroStart     = "intro_start";     // показан пролёт/история (первый вход)
    public const string IntroSkip      = "intro_skip";      // игрок нажал «Пропустить»
    public const string IntroComplete  = "intro_complete";  // игрок дочитал и нажал «Продолжить»

    // ─── Обёртки (безопасно принимают отсутствие данных) ───────────────────────

    public static void Send(string ev) => YG2.MetricaSend(ev);

    public static void Send(string ev, string key, string value) =>
        YG2.MetricaSend(ev, key, value);

    public static void Send(string ev, Dictionary<string, string> data) =>
        YG2.MetricaSend(ev, data);

    // ─── Удобные хелперы под ключевые точки воронки ────────────────────────────

    public static void DayStarted(int day, bool endless) =>
        YG2.MetricaSend(DayStart, new Dictionary<string, string>
        {
            { "day", day.ToString() },
            { "mode", endless ? "endless" : "story" }
        });

    public static void DayCompleted(int day, int earned) =>
        YG2.MetricaSend(DayComplete, new Dictionary<string, string>
        {
            { "day", day.ToString() },
            { "earned", earned.ToString() }
        });

    public static void DayFail(int day) => YG2.MetricaSend(DayFailed, "day", day.ToString());

    public static void Rewarded(string id) => YG2.MetricaSend(AdRewarded, "id", id);

    public static void Bought(string product) => YG2.MetricaSend(Purchase, "product", product);

    public static void Prompt(string which, bool accepted) =>
        YG2.MetricaSend(accepted ? PromptAccepted : PromptShown, "which", which);
}
