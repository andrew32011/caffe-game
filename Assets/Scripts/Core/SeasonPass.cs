/// <summary>
/// Батч 15 (Фаза C): сезонный пасс «Дневник Миры» — прогресс-рельса удержания. Опыт копится за
/// игру (звёзды подач); на уровнях — награды (free-трек: монеты; premium-трек: кристаллы, если
/// куплен). Премиум покупается за кристаллы (сток) — не обязателен. 30 уровней.
/// Данные в YG2.saves (passXp/passClaimed/passPremium). SDK: нет (кроме saves/Payments опц.).
/// </summary>
using UnityEngine;
using YG;

public static class SeasonPass
{
    public const int XpPerLevel = 35;
    public const int MaxLevel   = 30;
    public const int PremiumGemCost = 150; // купить premium-трек за кристаллы

    public static void AddXp(int n)
    {
        if (!YG2.isSDKEnabled || n <= 0) return;
        int before = Level;
        YG2.saves.passXp += n;
        int after = Level;
        if (after > before)
        {
            Analytics.Send("pass_level", "lvl", after.ToString());
            UiEffects.Instance?.FloatingText(Loc.T($"Сезон: уровень {after}!", $"Season: level {after}!"), new Color(0.9f, 0.8f, 1f));
        }
        GameManager.Instance?.SaveGame();
    }

    public static int Xp    => YG2.isSDKEnabled ? YG2.saves.passXp : 0;
    public static int Level => Mathf.Clamp(Xp / XpPerLevel, 0, MaxLevel);
    public static int XpInLevel => Xp - Level * XpPerLevel;
    public static bool Premium => YG2.isSDKEnabled && YG2.saves.passPremium;

    /// <summary>Награда free-трека на уровне (монеты).</summary>
    public static int FreeCoins(int level) => 60 + level * 15;
    /// <summary>Награда premium-трека на уровне (кристаллы).</summary>
    public static int PremiumGems(int level) => (level % 3 == 0) ? 5 : 2;

    public static bool IsClaimed(int level) =>
        YG2.isSDKEnabled && YG2.saves.passClaimed != null && YG2.saves.passClaimed.Contains(level);

    /// <summary>Забрать награду уровня (если достигнут и не забран). Возвращает монеты.</summary>
    public static int Claim(int level)
    {
        if (!YG2.isSDKEnabled || level < 1 || level > Level || IsClaimed(level)) return 0;
        YG2.saves.passClaimed.Add(level);
        int coins = FreeCoins(level);
        GameManager.Instance?.AddCoins(coins);
        if (Premium) GameManager.Instance?.AddGems(PremiumGems(level));
        GameManager.Instance?.SaveGame();
        return coins;
    }

    /// <summary>Купить premium-трек за кристаллы (сток премиума). true — успех.</summary>
    public static bool BuyPremiumWithGems()
    {
        var gm = GameManager.Instance;
        if (gm == null || Premium || gm.Gems < PremiumGemCost) return false;
        gm.SpendGems(PremiumGemCost);
        YG2.saves.passPremium = true;
        gm.SaveGame();
        Analytics.Send("pass_premium");
        return true;
    }

    /// <summary>Сколько уровней доступно к получению (для бейджа).</summary>
    public static int Claimable()
    {
        int n = 0;
        for (int l = 1; l <= Level; l++) if (!IsClaimed(l)) n++;
        return n;
    }
}
