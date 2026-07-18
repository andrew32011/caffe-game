/// <summary>
/// Батч 6: «Заказ дня» — ежедневный детерминированный микро-квест. Даёт повод вернуться
/// завтра, топливо для лидерборда (условие одинаково у всех в один игровой день) и апселл.
/// Тип квеста выбирается по seed = номер дня → у всех игроков совпадает. Прогресс копится
/// по ходу дня; награда начисляется в конце (DayController). UI-строка опциональна.
/// Сцена: MainScene. Зависимости: Loc, TMPro. SDK: Нет.
/// </summary>
using UnityEngine;
using TMPro;

public class DailyChallenge : MonoBehaviour
{
    [Tooltip("Опциональная строка на HUD «Заказ дня: …». Если не задана — квест работает молча.")]
    [SerializeField] private TextMeshProUGUI _hudText;

    public enum Kind
    {
        EarnCoins, PerfectHits, ThreeStars, SellToppings, GoodDrinks,
        ServeDrinks, StarCollector, HighSatisfaction, TwoStarsPlus, BigEarnings
    }
    private const int KindCount = 10;

    private Kind _kind;
    private int  _target;
    private int  _progress;
    private int  _reward;
    private bool _claimed;

    public bool   IsComplete  => _progress >= _target;
    public int    Reward      => _reward;
    public string Description  => Loc.T("Заказ дня: ", "Daily order: ") + GoalText()
                                  + $"  ({Mathf.Min(_progress, _target)}/{_target})";

    /// <summary>Выбирает квест дня (детерминированно по номеру дня) и сбрасывает прогресс.</summary>
    public void BeginDay(int day)
    {
        var rng   = new System.Random(day * 7919 + 13);
        _kind     = (Kind)rng.Next(0, KindCount);
        _progress = 0;
        _claimed  = false;

        switch (_kind)
        {
            case Kind.EarnCoins:       _target = 200 + day * 20;         _reward = 60 + day * 5; break;
            case Kind.PerfectHits:     _target = day > 15 ? 2 : 1;       _reward = 80 + day * 4; break;
            case Kind.ThreeStars:      _target = 1;                      _reward = 70 + day * 4; break;
            case Kind.SellToppings:    _target = 2;                      _reward = 60 + day * 4; break;
            case Kind.GoodDrinks:      _target = day > 15 ? 2 : 1;       _reward = 70 + day * 4; break;
            case Kind.ServeDrinks:     _target = 3 + day / 5;            _reward = 55 + day * 4; break;
            case Kind.StarCollector:   _target = 5 + day / 3;            _reward = 75 + day * 4; break;
            case Kind.HighSatisfaction:_target = 1;                      _reward = 90 + day * 5; break;
            case Kind.TwoStarsPlus:    _target = 3 + day / 6;            _reward = 65 + day * 4; break;
            default:                   _target = 350 + day * 30;         _reward = 90 + day * 6; break; // BigEarnings
        }
        UpdateHud();
    }

    /// <summary>Отчёт по одному поданному напитку — обновляет прогресс квеста.</summary>
    public void ReportDrink(int payment, int stars, int toppingsSold, bool perfect, float result)
    {
        switch (_kind)
        {
            case Kind.EarnCoins:        _progress += Mathf.Max(0, payment);      break;
            case Kind.PerfectHits:      if (perfect)        _progress++;          break;
            case Kind.ThreeStars:       if (stars >= 3)     _progress++;          break;
            case Kind.SellToppings:     _progress += Mathf.Max(0, toppingsSold);  break;
            case Kind.GoodDrinks:       if (result >= 0.8f) _progress++;          break;
            case Kind.ServeDrinks:      _progress++;                              break;
            case Kind.StarCollector:    _progress += Mathf.Max(0, stars);         break;
            case Kind.HighSatisfaction: if (result >= 0.95f) _progress++;         break;
            case Kind.TwoStarsPlus:     if (stars >= 2)     _progress++;          break;
            case Kind.BigEarnings:      _progress += Mathf.Max(0, payment);       break;
        }
        UpdateHud();
    }

    /// <summary>Если квест выполнен и награда ещё не выдана — вернуть её (и пометить выданной).</summary>
    public int Claim()
    {
        if (_claimed || !IsComplete) return 0;
        _claimed = true;
        return _reward;
    }

    private string GoalText()
    {
        switch (_kind)
        {
            case Kind.EarnCoins:        return Loc.T($"заработать {_target} монет",  $"earn {_target} coins");
            case Kind.PerfectHits:      return Loc.T($"{_target}x «Идеально»",        $"{_target}x Perfect");
            case Kind.ThreeStars:       return Loc.T($"{_target}x три звезды",        $"{_target}x three stars");
            case Kind.SellToppings:     return Loc.T($"продать {_target} топпинга",   $"sell {_target} toppings");
            case Kind.GoodDrinks:       return Loc.T($"{_target}x напиток на 80%",    $"{_target}x drink at 80%");
            case Kind.ServeDrinks:      return Loc.T($"обслужить {_target} гостей",   $"serve {_target} guests");
            case Kind.StarCollector:    return Loc.T($"собрать {_target} звёзд",      $"collect {_target} stars");
            case Kind.HighSatisfaction: return Loc.T($"{_target}x напиток на 95%",    $"{_target}x drink at 95%");
            case Kind.TwoStarsPlus:     return Loc.T($"{_target}x две звезды и выше", $"{_target}x two stars or more");
            default:                    return Loc.T($"заработать {_target} монет за смену", $"earn {_target} coins in a shift"); // BigEarnings
        }
    }

    private void UpdateHud()
    {
        if (_hudText != null) _hudText.text = Description;
    }
}
