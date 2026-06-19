/// <summary>
/// Батч 5: единый источник кривой сложности и экономики по номеру дня (1..40).
/// Раньше сложность была плоской: допуск кофемашины постоянный (0.15), а доход
/// имел резкий обрыв (×3 первые 3 дня → ×1 с 4-го). Здесь — гладкие кривые:
///   • Tolerance(day) — допуск шире в начале, у́же к финалу (растёт спрос на точность,
///     это «топливо» для комбо из Батча 3);
///   • EarlyEase(day) — мягкий ранний разгон дохода вместо обрыва.
/// База оплаты теперь берётся из авторской кривой дня (`DayData.coinsPerCorrectOrder`,
/// 10→50), которая раньше игнорировалась. Все числа тюнятся здесь.
/// Сцена: Глобально (читают DayController, CoffeeCraftingSystem)
/// Зависимости: Нет
/// SDK: Нет
/// </summary>
using UnityEngine;

public static class Difficulty
{
    public const int FinalDay = 40;

    /// <summary>Допуск совпадения ползунков кофемашины: 0.22 (день 1) → 0.10 (день 40).
    /// Чем больше — тем легче «попасть» в температуру/объём.</summary>
    public static float Tolerance(int day)
    {
        float k = Mathf.InverseLerp(1f, FinalDay, Mathf.Clamp(day, 1, FinalDay));
        return Mathf.Lerp(0.22f, 0.10f, k);
    }

    /// <summary>Плавный ранний бонус к доходу (без обрыва): ×1.5 в день 1 → ×1.0 к дню 6.
    /// Сглаживает старт, пока авторская база (coinsPerCorrectOrder) ещё низкая.</summary>
    public static float EarlyEase(int day)
    {
        float k = Mathf.InverseLerp(1f, 6f, Mathf.Clamp(day, 1, 6));
        return Mathf.Lerp(1.5f, 1.0f, k);
    }
}
