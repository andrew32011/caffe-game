/// <summary>
/// Батч 11: «Час пик» — скилл-механика темпа поверх обычной последовательной подачи
/// (без перестройки ядра ServeCustomer). В час-пиковый день у каждого гостя есть мягкий
/// таймер: успел подать напиток быстро — прибавка к оплате за темп; НЕ успел — без штрафа
/// (только теряешь бонус). Это добавляет драйва и потолок мастерства, не создавая фрустрации
/// провалом. Визуал темпа/очереди — RushHudUI; экономику применяет DayController.
///
/// Сцена: MainScene (чистая логика). Зависимости: UnityEngine (Mathf). SDK: нет.
/// </summary>
using UnityEngine;

public static class RushController
{
    /// <summary>Сколько секунд «в темпе» на один напиток. До истечения — бонус за скорость,
    /// после — множитель просто равен 1 (никакого наказания).</summary>
    public const float RushSeconds = 22f;

    /// <summary>Максимальная прибавка к оплате за мгновенную подачу (×1.30 при полном запасе).</summary>
    public const float MaxSpeedBonus = 0.30f;

    /// <summary>«Час пик» этого дня: в бесконечном режиме — всегда (там гости идут плотнее);
    /// в сюжете — периодически начиная с 6-го дня (после раннего разгона), чтобы это был
    /// заметный «пик», а не постоянный режим.</summary>
    public static bool IsRushDay(int displayDay, bool endless) =>
        endless || (displayDay >= 6 && displayDay % 3 == 0);

    /// <summary>Множитель оплаты за темп: 1 (не успел) … 1+MaxSpeedBonus (подал мгновенно).
    /// <paramref name="elapsed"/> — время приготовления напитка в секундах.</summary>
    public static float SpeedMultiplier(float elapsed)
    {
        float frac = Mathf.Clamp01(1f - elapsed / RushSeconds); // 1 = мгновенно, 0 = вышло время
        return 1f + MaxSpeedBonus * frac;
    }

    /// <summary>Успел ли игрок в темп (для похвалы «Быстро!»).</summary>
    public static bool InTime(float elapsed) => elapsed < RushSeconds;
}
