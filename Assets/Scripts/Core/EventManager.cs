/// <summary>
/// Батч 15 (Фаза C): еженедельное событие-турнир. Клиентское, детерминированное по ISO-неделе
/// (без сервера). Игрок копит очки события (звёзды за подачи) в течение недели; на вехах —
/// награды-кристаллы. Неделя сменилась → прогресс и забранные вехи сбрасываются. Ранг можно
/// показать через существующий лидерборд монет.
/// Данные в YG2.saves (eventWeek/eventProgress/eventTierClaimed). SDK: нет (кроме saves).
/// </summary>
using System;
using System.Globalization;
using UnityEngine;
using YG;

public static class EventManager
{
    public static readonly int[] Tiers    = { 25, 60, 120 }; // очки-пороги вех
    public static readonly int[] TierGems = { 3, 6, 12 };    // награда-кристаллы за веху

    public static string CurrentWeek()
    {
        var now = DateTime.Now;
        var cal = CultureInfo.InvariantCulture.Calendar;
        int week = cal.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{now.Year}-W{week:00}";
    }

    private static void EnsureWeek()
    {
        if (!YG2.isSDKEnabled) return;
        string wk = CurrentWeek();
        if (YG2.saves.eventWeek != wk)
        {
            YG2.saves.eventWeek = wk;
            YG2.saves.eventProgress = 0;
            YG2.saves.eventTierClaimed = 0;
            GameManager.Instance?.SaveGame();
        }
    }

    public static void AddStars(int n)
    {
        if (!YG2.isSDKEnabled || n <= 0) return;
        EnsureWeek();
        YG2.saves.eventProgress += n;
        GameManager.Instance?.SaveGame();
    }

    public static int Progress { get { EnsureWeek(); return YG2.isSDKEnabled ? YG2.saves.eventProgress : 0; } }
    public static int TierClaimed => YG2.isSDKEnabled ? YG2.saves.eventTierClaimed : 0;
    public static int MaxTierPoints => Tiers[Tiers.Length - 1];

    /// <summary>Следующая невзятая веха доступна к получению?</summary>
    public static bool CanClaim()
    {
        EnsureWeek();
        int tc = TierClaimed;
        return tc < Tiers.Length && Progress >= Tiers[tc];
    }

    /// <summary>Забрать следующую доступную веху. Возвращает выданные кристаллы (0 — нельзя).</summary>
    public static int ClaimTier()
    {
        if (!CanClaim()) return 0;
        int tc = YG2.saves.eventTierClaimed;
        int gems = TierGems[tc];
        YG2.saves.eventTierClaimed = tc + 1;
        GameManager.Instance?.AddGems(gems);
        GameManager.Instance?.SaveGame();
        Analytics.Send("event_progress", "tier", (tc + 1).ToString());
        return gems;
    }

    /// <summary>Порог следующей вехи (или последней, если всё взято).</summary>
    public static int NextThreshold()
    {
        int tc = Mathf.Clamp(TierClaimed, 0, Tiers.Length - 1);
        return Tiers[tc];
    }
}
