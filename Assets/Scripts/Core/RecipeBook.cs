/// <summary>
/// Батч 15 (Фаза B): книга рецептов + мастерство. За каждую ХОРОШУЮ подачу напитка растёт
/// счётчик по его типу; на порогах — уровень мастерства, дающий небольшой перманентный бонус
/// к оплате именно этого напитка (коллекция + новизна + «играть ради мастерства»).
/// Данные в YG2.saves (recipeKeys/recipeServed). Читает DayController (ReportServe/MasteryBonus).
/// Сцена: MainScene. Зависимости: GameManager, Analytics, YG2. SDK: нет.
/// </summary>
using UnityEngine;
using YG;

public static class RecipeBook
{
    public const int ServesPerLevel = 8;   // хороших подач на уровень мастерства
    public const int MaxLevel       = 3;

    /// <summary>Отметить подачу напитка. Хорошая (result≥0.6) двигает мастерство.</summary>
    public static void ReportServe(CoffeeType type, float result)
    {
        if (!YG2.isSDKEnabled || result < 0.6f) return;
        int key = (int)type;
        int i = YG2.saves.recipeKeys.IndexOf(key);
        int before;
        if (i < 0) { YG2.saves.recipeKeys.Add(key); YG2.saves.recipeServed.Add(1); before = 0; }
        else { before = YG2.saves.recipeServed[i]; YG2.saves.recipeServed[i] = before + 1; }

        int i2 = i < 0 ? YG2.saves.recipeServed.Count - 1 : i;
        int after = YG2.saves.recipeServed[i2];
        int lvlBefore = Mathf.Clamp(before / ServesPerLevel, 0, MaxLevel);
        int lvlAfter  = Mathf.Clamp(after / ServesPerLevel, 0, MaxLevel);
        if (lvlAfter > lvlBefore)
        {
            Analytics.Send("recipe_master", "type", type.ToString());
            UiEffects.Instance?.FloatingText(Loc.T($"Мастерство: {Name(type)} ур.{lvlAfter}",
                                                   $"Mastery: {Name(type)} lv.{lvlAfter}"), new Color(0.6f, 0.9f, 1f));
        }
        GameManager.Instance?.SaveGame();
    }

    public static int Served(CoffeeType type)
    {
        if (!YG2.isSDKEnabled) return 0;
        int i = YG2.saves.recipeKeys.IndexOf((int)type);
        return i >= 0 ? YG2.saves.recipeServed[i] : 0;
    }

    public static int Level(CoffeeType type) => Mathf.Clamp(Served(type) / ServesPerLevel, 0, MaxLevel);

    /// <summary>Множитель оплаты за мастерство этого напитка (+3%/уровень).</summary>
    public static float MasteryBonus(CoffeeType type) => 1f + Level(type) * 0.03f;

    /// <summary>Сколько напитков хоть раз подано (для альбома/коллекции).</summary>
    public static int Discovered()
    {
        if (!YG2.isSDKEnabled) return 0;
        int n = 0;
        foreach (var v in YG2.saves.recipeServed) if (v > 0) n++;
        return n;
    }

    public static string Name(CoffeeType t)
    {
        switch (t)
        {
            case CoffeeType.Espresso:     return Loc.T("Эспрессо", "Espresso");
            case CoffeeType.Americano:    return Loc.T("Американо", "Americano");
            case CoffeeType.Cappuccino:   return Loc.T("Капучино", "Cappuccino");
            case CoffeeType.Latte:        return Loc.T("Латте", "Latte");
            case CoffeeType.Mocha:        return Loc.T("Мокко", "Mocha");
            case CoffeeType.HerbalTea:    return Loc.T("Травяной чай", "Herbal tea");
            case CoffeeType.GreenTea:     return Loc.T("Зелёный чай", "Green tea");
            case CoffeeType.HotChocolate: return Loc.T("Горячий шоколад", "Hot chocolate");
            case CoffeeType.BlackCoffee:  return Loc.T("Чёрный кофе", "Black coffee");
            default:                      return t.ToString();
        }
    }
}
