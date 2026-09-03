/// <summary>
/// Батч 12-D: достижения-майлстоуны. Переменные крупные цели, награждающие КРИСТАЛЛАМИ —
/// главный не-платный источник премиум-валюты (по исследованиям: серотониновые «социальные»/
/// достиженческие награды дают долгое удержание). Оцениваются в конце дня по уже имеющимся
/// данным сейва (знакомства, дни, богатство, рекорд endless) — новых трекеров не требуют.
///
/// Выданные достижения хранятся в сейве (achievementsClaimed) — каждое единожды. Несколько
/// закрытых за раз суммируются в один поп-ап (без спама).
/// Сцена: MainScene. Зависимости: GameManager, RewardPopupUI, Analytics, Loc, YG2. SDK: нет.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using YG;

public static class Achievements
{
    private struct Ach
    {
        public string Id;
        public int Gems;
        public System.Func<SavesYG, bool> Done;
        public Ach(string id, int gems, System.Func<SavesYG, bool> done) { Id = id; Gems = gems; Done = done; }
    }

    private static readonly Ach[] All =
    {
        new Ach("guests5",   20, s => s.journalKeys.Count >= 5),
        new Ach("guests10",  40, s => s.journalKeys.Count >= 10),
        new Ach("guests20",  80, s => s.journalKeys.Count >= 20),
        new Ach("day10",     30, s => s.currentDay >= 10 || s.endlessMode),
        new Ach("day20",     50, s => s.currentDay >= 20 || s.endlessMode),
        new Ach("finale",   100, s => s.endlessMode),
        new Ach("rich5000",  30, s => s.totalCoins >= 5000),
        new Ach("rich10000", 60, s => s.totalCoins >= 10000),
        new Ach("endless5",  40, s => s.endlessBestDay >= 5),
        new Ach("endless10", 80, s => s.endlessBestDay >= 10),
    };

    /// <summary>Проверяет все достижения; выдаёт кристаллы за вновь закрытые (один раз каждое).
    /// Вызывать в конце дня. До разблокировки кристаллов ничего не делает.</summary>
    public static void CheckAll()
    {
        if (!YG2.isSDKEnabled) return;
        if (!ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems)) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var save = YG2.saves;
        int gainedGems = 0;
        int gainedCount = 0;

        foreach (var a in All)
        {
            if (save.achievementsClaimed.Contains(a.Id)) continue;
            if (!a.Done(save)) continue;

            save.achievementsClaimed.Add(a.Id);
            gainedGems += a.Gems;
            gainedCount++;
            Analytics.Send("achievement", "id", a.Id);
        }

        if (gainedCount > 0)
        {
            // Перф: одно облачное сохранение на все закрытые за раз достижения (а не N).
            gm.AddGems(gainedGems); // AddGems сохраняет и обновляет HUD один раз

            string title = gainedCount == 1
                ? Loc.T("Достижение!", "Achievement!")
                : Loc.T($"Достижений: {gainedCount}!", $"{gainedCount} achievements!");
            RewardPopupUI.Ensure().Show(title,
                Loc.T($"+{gainedGems} кристаллов", $"+{gainedGems} gems"),
                new Color(0.35f, 0.7f, 0.95f), 3.8f);
        }
    }
}
