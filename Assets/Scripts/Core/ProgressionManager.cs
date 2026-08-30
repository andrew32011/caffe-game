/// <summary>
/// Батч 12 (A): движок прогрессивной разблокировки механик. Новизна дозируется — новые
/// системы открываются по дням, каждая с ярким «НОВОЕ ОТКРЫТО» (RewardPopupUI). Держит
/// первые 10 минут насыщенными и не перегружает новичка (feature-gating из исследований).
///
/// Гейтит ТОЛЬКО новые механики Батча 12 (лут/кристаллы/кастомизация), чтобы не ломать уже
/// работающий поток игры. Существующие системы (топпинги, квест, журнал, магазин) остаются
/// как есть. В бесконечном режиме всё считается открытым.
///
/// Расписание намеренно раннее — новинки падают в первые дни (ранние награды по исследованию).
/// Сцена: глобально. Зависимости: YG2 (saves), RewardPopupUI, Analytics, Loc. SDK: нет.
/// </summary>
using UnityEngine;
using YG;

public static class ProgressionManager
{
    public enum Feature { LootChests, Gems, Customization }

    /// <summary>На каком дне открывается фича (раннее расписание для насыщенного старта).</summary>
    public static int UnlockDay(Feature f)
    {
        switch (f)
        {
            case Feature.LootChests:    return 2;
            case Feature.Gems:          return 3;
            case Feature.Customization: return 4;
            default:                    return 1;
        }
    }

    /// <summary>Открыта ли фича (по текущему дню или уже объявлена; в endless — всё открыто).</summary>
    public static bool IsUnlocked(Feature f)
    {
        if (!YG2.isSDKEnabled) return false;
        if (YG2.saves.endlessMode) return true;
        if (YG2.saves.unlockedFeatures.Contains(f.ToString())) return true;
        return YG2.saves.currentDay >= UnlockDay(f);
    }

    /// <summary>Вызывается на старте дня: объявляет фичи, открывшиеся к этому дню (по одной,
    /// с поп-апом «НОВОЕ ОТКРЫТО»). Идемпотентно — каждая фича объявляется единожды.</summary>
    public static void CheckDayUnlocks(int day)
    {
        if (!YG2.isSDKEnabled || YG2.saves.endlessMode) return;

        foreach (Feature f in System.Enum.GetValues(typeof(Feature)))
        {
            if (day < UnlockDay(f)) continue;
            string key = f.ToString();
            if (YG2.saves.unlockedFeatures.Contains(key)) continue;

            YG2.saves.unlockedFeatures.Add(key);
            YG2.SaveProgress();
            Analytics.Send("feature_unlock", "feature", key);
            Announce(f);
            return; // максимум одна разблокировка за день — без перегруза
        }
    }

    private static void Announce(Feature f)
    {
        string title = Loc.T("Новое открыто!", "New unlocked!");
        string body;
        Color accent;
        switch (f)
        {
            case Feature.LootChests:
                body = Loc.T("Сундуки и подарки! Гости иногда оставляют награду — открывай сундук за день.",
                             "Chests & gifts! Guests sometimes leave a reward — open a chest each day.");
                accent = new Color(0.95f, 0.75f, 0.25f);
                break;
            case Feature.Gems:
                body = Loc.T("Кристаллы — редкая валюта. Копи их за достижения и трать на особое.",
                             "Gems — a rare currency. Earn them from achievements and spend on special things.");
                accent = new Color(0.35f, 0.7f, 0.95f);
                break;
            default: // Customization
                body = Loc.T("Кастомизация! Меняй аватар и оформление кофейни — сделай её своей.",
                             "Customization! Change your avatar and café look — make it yours.");
                accent = new Color(0.75f, 0.5f, 0.95f);
                break;
        }
        RewardPopupUI.Ensure().Show(title, body, accent, 4f);
    }
}
