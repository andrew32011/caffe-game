/// <summary>
/// Батч 12-C: переменное подкрепление (variable-ratio) — «постоянно что-то происходит».
/// Каждый обслуженный гость имеет ШАНС дропа (монеты/жетоны/ключ/редко кристалл), плюс
/// гарантированный «сундук дня» в конце. Непредсказуемость даёт дофаминовый отклик (по
/// исследованиям), а pity-счётчик защищает от полосы невезения (не фрустрируем).
///
/// Ранние дни — выше шанс (частые ранние награды по FTUE-исследованиям). Мелкий дроп —
/// ненавязчивый FloatingText; крупный (ключ/кристалл/мистери) — праздничный RewardPopup.
/// Гейтится ProgressionManager (LootChests, с дня 2).
/// Сцена: MainScene. Зависимости: GameManager, UiEffects, RewardPopupUI, Analytics, Loc. SDK: нет.
/// </summary>
using UnityEngine;

public static class LootSystem
{
    // Pity: сколько подач подряд без дропа — после порога дроп гарантирован.
    private static int _dryStreak;
    private const int PityThreshold = 3;

    /// <summary>Шанс-дроп за одного обслуженного гостя. day — номер дня, stars — 1..3.</summary>
    public static void RollDrop(int day, int stars)
    {
        if (!ProgressionManager.IsUnlocked(ProgressionManager.Feature.LootChests)) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Базовый шанс + ранний бонус (первые дни щедрее) + бонус за качество подачи.
        float chance = 0.35f + Mathf.Max(0, 6 - day) * 0.05f + (stars - 1) * 0.06f;
        bool forced = _dryStreak >= PityThreshold;

        if (!forced && Random.value > chance)
        {
            _dryStreak++;
            return;
        }
        _dryStreak = 0;

        GiveWeightedDrop(day, big: false);
    }

    /// <summary>Гарантированный «сундук дня» в конце дня — всегда что-то, шанс на редкое.</summary>
    public static void GrantDayChest(int day)
    {
        if (!ProgressionManager.IsUnlocked(ProgressionManager.Feature.LootChests)) return;
        if (GameManager.Instance == null) return;

        Analytics.Send("chest_open", "source", "day");
        GiveWeightedDrop(day, big: true);
    }

    // ─── Выдача взвешенного дропа ──────────────────────────────────────────────

    private static void GiveWeightedDrop(int day, bool big)
    {
        bool gemsOn = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);

        // Батч 14: две валюты — монеты(частые) / кристаллы(редко) / мистери(крупный, монеты+кристаллы).
        int wCoins   = big ? 40 : 62;
        int wGem     = gemsOn ? (big ? 12 : 6) : 0;
        int wMystery = big ? 24 : 8;
        int total = wCoins + wGem + wMystery;
        int r = Random.Range(0, total);

        if ((r -= wCoins) < 0) { DropCoins(day, big); return; }
        if ((r -= wGem) < 0)   { DropGems(big); return; }
        DropMystery(day);
    }

    private static void DropCoins(int day, bool big)
    {
        int amount = big ? Random.Range(day * 8, day * 15 + 30) : Random.Range(day * 2, day * 5 + 6);
        GameManager.Instance.AddCoins(amount);
        Analytics.Send("loot_drop", "type", "coins");
        if (big)
            RewardPopupUI.Ensure().Show(Loc.T("Сундук дня!", "Daily chest!"),
                Loc.T($"+{amount} монет", $"+{amount} coins"), new Color(0.95f, 0.8f, 0.3f), 3.2f);
        else
            UiEffects.Instance?.FloatingText("+" + amount, new Color(1f, 0.85f, 0.25f));
    }

    private static void DropGems(bool big)
    {
        int amount = big ? Random.Range(2, 5) : 1;
        GameManager.Instance.AddGems(amount);
        Analytics.Send("loot_drop", "type", "gem");
        RewardPopupUI.Ensure().Show(Loc.T("Кристалл!", "A gem!"),
            Loc.T($"+{amount} кристаллов — редкая удача!", $"+{amount} gems — rare luck!"),
            new Color(0.35f, 0.7f, 0.95f), 3.5f);
    }

    private static void DropMystery(int day)
    {
        int coins = Random.Range(day * 6, day * 12 + 25);
        bool gemsOn = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);
        int gems = gemsOn ? Random.Range(1, 4) : 0;
        GameManager.Instance.AddCoins(coins);
        if (gems > 0) GameManager.Instance.AddGems(gems);
        Analytics.Send("loot_drop", "type", "mystery");
        string body = gems > 0
            ? Loc.T($"+{coins} монет и +{gems} кристаллов", $"+{coins} coins and +{gems} gems")
            : Loc.T($"+{coins} монет", $"+{coins} coins");
        RewardPopupUI.Ensure().Show(Loc.T("Таинственный подарок!", "Mystery gift!"),
            body, new Color(0.6f, 0.4f, 0.85f), 3.5f);
    }
}
