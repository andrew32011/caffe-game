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
        var gm = GameManager.Instance;
        bool gemsOn = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);

        // Веса: монеты(частые) / жетоны / ключ / кристалл(редко) / мистери(крупный).
        int wCoins   = big ? 30 : 50;
        int wTokens  = 28;
        int wKey     = big ? 16 : 12;
        int wGem     = gemsOn ? (big ? 8 : 4) : 0;
        int wMystery = big ? 18 : 6;
        int total = wCoins + wTokens + wKey + wGem + wMystery;
        int r = Random.Range(0, total);

        if ((r -= wCoins) < 0)      { DropCoins(day, big); return; }
        if ((r -= wTokens) < 0)     { DropTokens(big); return; }
        if ((r -= wKey) < 0)        { DropKey(big); return; }
        if ((r -= wGem) < 0)        { DropGems(big); return; }
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

    private static void DropTokens(bool big)
    {
        int amount = big ? Random.Range(3, 7) : Random.Range(1, 4);
        GameManager.Instance.AddTokens(amount);
        Analytics.Send("loot_drop", "type", "tokens");
        if (big)
            RewardPopupUI.Ensure().Show(Loc.T("Сундук дня!", "Daily chest!"),
                Loc.T($"+{amount} жетонов", $"+{amount} tokens"), new Color(0.85f, 0.7f, 0.35f), 3.2f);
        else
            UiEffects.Instance?.FloatingText(Loc.T($"+{amount} жетон.", $"+{amount} tok."), new Color(0.9f, 0.75f, 0.4f));
    }

    private static void DropKey(bool big)
    {
        GameManager.Instance.AddKeys(1);
        Analytics.Send("loot_drop", "type", "key");
        RewardPopupUI.Ensure().Show(Loc.T("Ключ!", "A key!"),
            Loc.T("+1 ключ — пригодится для сундуков.", "+1 key — handy for chests."),
            new Color(0.75f, 0.55f, 0.25f), 3.2f);
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
        int tokens = Random.Range(2, 5);
        GameManager.Instance.AddCoins(coins);
        GameManager.Instance.AddTokens(tokens);
        Analytics.Send("loot_drop", "type", "mystery");
        RewardPopupUI.Ensure().Show(Loc.T("Таинственный подарок!", "Mystery gift!"),
            Loc.T($"+{coins} монет и +{tokens} жетонов", $"+{coins} coins and +{tokens} tokens"),
            new Color(0.6f, 0.4f, 0.85f), 3.5f);
    }
}
