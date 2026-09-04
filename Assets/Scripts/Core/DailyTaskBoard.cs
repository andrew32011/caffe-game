/// <summary>
/// Батч 15: ежедневная доска задач — 3 задачи на игровой день + бонус за «все три».
/// Заменяет одиночный «Заказ дня» как видимую петлю возврата (Cooking Madness). Задачи
/// детерминированы по дню (у всех игроков одинаковы). Награда за задачу — монеты СРАЗУ по
/// клику (кормит обустройство), бонус за все три — крупнее (+кристаллы). Прогресс копится
/// по ходу дня (DayController.Report), клейм — из доски (UI). Состояние — в YG2.saves.
/// Сцена: MainScene. Зависимости: GameManager, Loc, Analytics, YG2. SDK: нет.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using YG;

public class DailyTask
{
    public DailyChallenge.Kind Kind;
    public int Target, Progress, Reward;
    public bool Complete => Progress >= Target;

    public void Report(int payment, int stars, int toppingsSold, bool perfect, float result)
    {
        switch (Kind)
        {
            case DailyChallenge.Kind.EarnCoins:        Progress += Mathf.Max(0, payment);      break;
            case DailyChallenge.Kind.PerfectHits:      if (perfect)         Progress++;         break;
            case DailyChallenge.Kind.ThreeStars:       if (stars >= 3)      Progress++;         break;
            case DailyChallenge.Kind.SellToppings:     Progress += Mathf.Max(0, toppingsSold);  break;
            case DailyChallenge.Kind.GoodDrinks:       if (result >= 0.8f)  Progress++;         break;
            case DailyChallenge.Kind.ServeDrinks:      Progress++;                              break;
            case DailyChallenge.Kind.StarCollector:    Progress += Mathf.Max(0, stars);         break;
            case DailyChallenge.Kind.HighSatisfaction: if (result >= 0.95f) Progress++;         break;
            case DailyChallenge.Kind.TwoStarsPlus:     if (stars >= 2)      Progress++;         break;
            case DailyChallenge.Kind.BigEarnings:      Progress += Mathf.Max(0, payment);       break;
        }
    }

    public string GoalText()
    {
        switch (Kind)
        {
            case DailyChallenge.Kind.EarnCoins:        return Loc.T($"Заработать {Target} монет",  $"Earn {Target} coins");
            case DailyChallenge.Kind.PerfectHits:      return Loc.T($"{Target}x «Идеально»",        $"{Target}x Perfect");
            case DailyChallenge.Kind.ThreeStars:       return Loc.T($"{Target}x три звезды",        $"{Target}x three stars");
            case DailyChallenge.Kind.SellToppings:     return Loc.T($"Продать {Target} топпинга",   $"Sell {Target} toppings");
            case DailyChallenge.Kind.GoodDrinks:       return Loc.T($"{Target}x напиток на 80%",    $"{Target}x drink at 80%");
            case DailyChallenge.Kind.ServeDrinks:      return Loc.T($"Обслужить {Target} гостей",   $"Serve {Target} guests");
            case DailyChallenge.Kind.StarCollector:    return Loc.T($"Собрать {Target} звёзд",      $"Collect {Target} stars");
            case DailyChallenge.Kind.HighSatisfaction: return Loc.T($"{Target}x напиток на 95%",    $"{Target}x drink at 95%");
            case DailyChallenge.Kind.TwoStarsPlus:     return Loc.T($"{Target}x две звезды и выше", $"{Target}x two stars+");
            default:                                   return Loc.T($"Заработать {Target} за смену", $"Earn {Target} in a shift");
        }
    }

    public string ProgressText() => $"{Mathf.Min(Progress, Target)}/{Target}";
}

public static class DailyTaskBoard
{
    private const int TaskCount = 3;
    public const int BonusReward = 120;   // монеты за «все три»
    public const int BonusGems   = 3;     // + кристаллы за «все три»

    private static readonly List<DailyTask> _tasks = new List<DailyTask>();
    private static int _day = -1;

    public static IReadOnlyList<DailyTask> Tasks => _tasks;

    /// <summary>Готовит доску на игровой день (детерминированно). Клейм-состояние — из сейва.</summary>
    public static void BeginDay(int day)
    {
        _day = day;
        _tasks.Clear();

        // 3 различных задачи по seed=день.
        var rng = new System.Random(day * 100003 + 7);
        int kindCount = System.Enum.GetValues(typeof(DailyChallenge.Kind)).Length;
        var used = new HashSet<int>();
        while (_tasks.Count < TaskCount && used.Count < kindCount)
        {
            int k = rng.Next(0, kindCount);
            if (!used.Add(k)) continue;
            _tasks.Add(Make((DailyChallenge.Kind)k, day));
        }

        // Новый день → сбрасываем клейм-состояние в сейве.
        if (YG2.isSDKEnabled && YG2.saves.dailyTasksDate != day.ToString())
        {
            YG2.saves.dailyTasksDate = day.ToString();
            YG2.saves.dailyTasksClaimed = new List<int>();
            YG2.saves.dailyTasksBonusClaimed = false;
            GameManager.Instance?.SaveGame();
        }
    }

    private static DailyTask Make(DailyChallenge.Kind kind, int day)
    {
        var t = new DailyTask { Kind = kind, Progress = 0 };
        switch (kind)
        {
            case DailyChallenge.Kind.EarnCoins:        t.Target = 150 + day * 15;  t.Reward = 50 + day * 4; break;
            case DailyChallenge.Kind.PerfectHits:      t.Target = day > 15 ? 2 : 1; t.Reward = 70 + day * 3; break;
            case DailyChallenge.Kind.ThreeStars:       t.Target = 1;               t.Reward = 60 + day * 3; break;
            case DailyChallenge.Kind.SellToppings:     t.Target = 2;               t.Reward = 55 + day * 3; break;
            case DailyChallenge.Kind.GoodDrinks:       t.Target = day > 15 ? 2 : 1; t.Reward = 60 + day * 3; break;
            case DailyChallenge.Kind.ServeDrinks:      t.Target = 3 + day / 6;     t.Reward = 50 + day * 3; break;
            case DailyChallenge.Kind.StarCollector:    t.Target = 5 + day / 4;     t.Reward = 65 + day * 3; break;
            case DailyChallenge.Kind.HighSatisfaction: t.Target = 1;               t.Reward = 80 + day * 4; break;
            case DailyChallenge.Kind.TwoStarsPlus:     t.Target = 3 + day / 6;     t.Reward = 55 + day * 3; break;
            default:                                   t.Target = 300 + day * 25;  t.Reward = 80 + day * 5; break;
        }
        return t;
    }

    public static void Report(int payment, int stars, int toppingsSold, bool perfect, float result)
    {
        for (int i = 0; i < _tasks.Count; i++) _tasks[i].Report(payment, stars, toppingsSold, perfect, result);
    }

    public static bool IsClaimed(int i) =>
        YG2.isSDKEnabled && YG2.saves.dailyTasksClaimed != null && YG2.saves.dailyTasksClaimed.Contains(i);

    /// <summary>Забрать награду за задачу i (если выполнена и не забрана). Возвращает монеты.</summary>
    public static int Claim(int i)
    {
        if (i < 0 || i >= _tasks.Count || !_tasks[i].Complete || IsClaimed(i)) return 0;
        YG2.saves.dailyTasksClaimed.Add(i);
        int reward = _tasks[i].Reward;
        GameManager.Instance?.AddCoins(reward);
        GameManager.Instance?.SaveGame();
        Analytics.Send("task_claim", "kind", _tasks[i].Kind.ToString());
        return reward;
    }

    public static bool AllComplete
    {
        get { if (_tasks.Count == 0) return false; foreach (var t in _tasks) if (!t.Complete) return false; return true; }
    }
    public static bool BonusClaimed => YG2.isSDKEnabled && YG2.saves.dailyTasksBonusClaimed;
    public static bool AllClaimed
    {
        get { for (int i = 0; i < _tasks.Count; i++) if (!IsClaimed(i)) return false; return _tasks.Count > 0; }
    }

    /// <summary>Забрать бонус «все три» (требует, что все три задачи забраны). Возвращает монеты (гемы начисляет сам).</summary>
    public static int ClaimBonus()
    {
        if (BonusClaimed || !AllClaimed) return 0;
        YG2.saves.dailyTasksBonusClaimed = true;
        GameManager.Instance?.AddCoins(BonusReward);
        GameManager.Instance?.AddGems(BonusGems);
        GameManager.Instance?.SaveGame();
        Analytics.Send("task_allthree");
        return BonusReward;
    }

    /// <summary>Сколько наград доступно к получению (для бейджа на иконке доски).</summary>
    public static int Claimable()
    {
        int n = 0;
        for (int i = 0; i < _tasks.Count; i++) if (_tasks[i].Complete && !IsClaimed(i)) n++;
        if (AllClaimed && !BonusClaimed) n++;
        return n;
    }
}
