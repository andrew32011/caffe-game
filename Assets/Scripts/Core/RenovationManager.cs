/// <summary>
/// Батч 15 (центр вовлечения): обустройство кофейни — ГЛАВНЫЙ сток монет и вектор прогресса.
/// Игрок всегда видит «на что копит»: лестница проектов, каждый за монеты, при завершении даёт
/// геймплей-выгоду (оплата/чаевые/допуск), сюжетный бит (реставрация = метафора надежды) и
/// зримое изменение зала (мебель ставит RenovationVisualizer по префабам/якорям от билдера).
///
/// Данные/логика — здесь (static, читает/пишет YG2.saves). Визуал — RenovationVisualizer.
/// Экономические бонусы читают DayController/CoffeeCraftingSystem (как PriceMultiplier и т.п.).
/// Сцена: MainScene. Зависимости: YG2 (saves), GameManager, Loc, Analytics. SDK: нет.
/// </summary>
using UnityEngine;
using YG;

public static class RenovationManager
{
    public enum Benefit { Price, Tip, Tolerance }

    public struct Project
    {
        public string Ru, En, StoryRu, StoryEn;
        public int Cost;
        public Benefit Benefit;
        public float Value; // прибавка за этот проект
        public Project(string ru, string en, int cost, Benefit b, float val, string storyRu, string storyEn)
        { Ru = ru; En = en; Cost = cost; Benefit = b; Value = val; StoryRu = storyRu; StoryEn = storyEn; }
    }

    // Лестница проектов (порядок = стадии). Цена растёт, выгоды скромные и складываются.
    public static readonly Project[] Projects =
    {
        new Project("Починить кофемашину", "Fix the coffee machine", 400, Benefit.Price, 0.06f,
            "Машина снова шумит по-доброму. Первый шаг — кофейня оживает.",
            "The machine hums warmly again. First step — the café comes alive."),
        new Project("Удобные стулья", "Cozy chairs", 700, Benefit.Tip, 0.05f,
            "Гости задерживаются подольше — и охотнее оставляют на чай.",
            "Guests linger a little longer — and tip more willingly."),
        new Project("Тёплый ковёр", "Warm carpet", 1100, Benefit.Tip, 0.05f,
            "Пол больше не холодит. В зале стало по-домашнему.",
            "The floor no longer chills. The room feels like home."),
        new Project("Витрина с выпечкой", "Pastry display", 1600, Benefit.Price, 0.07f,
            "Аромат свежей выпечки тянет прохожих с самого порога.",
            "The scent of fresh pastries pulls passers-by from the doorstep."),
        new Project("Люстра и свечи", "Chandelier and candles", 2300, Benefit.Tolerance, 0.03f,
            "Мягкий свет — руки работают увереннее.",
            "Soft light — steadier hands at work."),
        new Project("Книжная полка", "Bookshelf", 3100, Benefit.Tip, 0.06f,
            "Кто-то оставил книгу с пометками Кая. Он точно был здесь.",
            "Someone left a book with Kai's notes. He was surely here."),
        new Project("Знамёна миров", "Banners of the worlds", 4100, Benefit.Price, 0.08f,
            "Флаги дальних краёв. Гостям из-за грани теперь уютнее.",
            "Flags of far realms. Guests from beyond feel more at home."),
        new Project("Профи-бариста-стол", "Pro barista station", 5300, Benefit.Tolerance, 0.03f,
            "Всё под рукой — темп работы вырос.",
            "Everything within reach — the pace picks up."),
        new Project("Тёплая витражная вывеска", "Warm stained sign", 6800, Benefit.Price, 0.08f,
            "«Междумирье» снова светится в ночи — как маяк.",
            "\"The Inbetween\" glows in the night again — like a beacon."),
        new Project("Уголок с кристаллом", "Crystal nook", 8500, Benefit.Tip, 0.07f,
            "Кристалл шепчет забытые имена. Одно из них — твоё.",
            "The crystal whispers forgotten names. One of them is yours."),
        new Project("Терраса", "Terrace", 10500, Benefit.Price, 0.10f,
            "Столики под открытым небом меж миров. Гостей стало больше.",
            "Tables under the open sky between worlds. More guests come."),
        new Project("Второй этаж", "Second floor", 13000, Benefit.Tip, 0.08f,
            "Наверху — комната, где ждал бы Кай. Почти как дом.",
            "Upstairs — a room where Kai would wait. Almost like home."),
    };

    public static int  Stage        => YG2.isSDKEnabled ? YG2.saves.renovationStage : 0;
    public static bool AllDone       => Stage >= Projects.Length;
    public static Project Current    => Projects[Mathf.Clamp(Stage, 0, Projects.Length - 1)];
    public static int  CurrentCost   => AllDone ? 0 : Current.Cost;

    /// <summary>Хватает ли монет на текущий проект.</summary>
    public static bool CanAfford()
    {
        var gm = GameManager.Instance;
        return gm != null && !AllDone && gm.TotalCoins >= CurrentCost;
    }

    /// <summary>Оплатить и завершить текущий проект (списывает монеты). true — успех.
    /// Возвращает завершённый проект через out. Визуал/сюжет показывает вызывающий (RenovationUI).</summary>
    public static bool Complete(out Project done)
    {
        done = default;
        var gm = GameManager.Instance;
        if (gm == null || AllDone || gm.TotalCoins < CurrentCost) return false;
        done = Current;
        gm.AddCoins(-CurrentCost);
        YG2.saves.renovationStage++;
        YG2.saves.renovationBanked = 0;
        gm.SaveGame();
        Analytics.Send("reno_stage", "stage", YG2.saves.renovationStage.ToString());
        return true;
    }

    /// <summary>Мгновенно завершить за кристаллы (нетерпеливость — сток премиума).</summary>
    public const int GemInstantCost = 30;
    public static bool CompleteWithGems(out Project done)
    {
        done = default;
        var gm = GameManager.Instance;
        if (gm == null || AllDone || gm.Gems < GemInstantCost) return false;
        done = Current;
        gm.SpendGems(GemInstantCost);
        YG2.saves.renovationStage++;
        YG2.saves.renovationBanked = 0;
        gm.SaveGame();
        Analytics.Send("reno_stage", "stage", YG2.saves.renovationStage.ToString() + "_gem");
        return true;
    }

    // ─── Экономические бонусы от завершённых проектов (складываются) ─────────────
    public static float PriceBonus     => Sum(Benefit.Price);
    public static float TipBonus       => Sum(Benefit.Tip);
    public static float ToleranceBonus => Sum(Benefit.Tolerance);

    private static float Sum(Benefit b)
    {
        float s = 0f;
        int done = Mathf.Clamp(Stage, 0, Projects.Length);
        for (int i = 0; i < done; i++)
            if (Projects[i].Benefit == b) s += Projects[i].Value;
        return s;
    }
}
