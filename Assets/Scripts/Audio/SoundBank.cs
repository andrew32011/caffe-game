/// <summary>
/// Банк звуков (ScriptableObject): именованные события → AudioClip. Удобно назначать
/// 50 клипов пакета в инспекторе и легко перевыбирать (все клипы — в массиве all).
/// Билдер создаёт/наполняет ассет; AudioController играет по именам.
/// Сцена: Глобально (ассет). Зависимости: нет. SDK: нет.
/// </summary>
using UnityEngine;

[CreateAssetMenu(fileName = "SoundBank", menuName = "CoffeGame/Sound Bank")]
public class SoundBank : ScriptableObject
{
    [Header("Музыка (фон)")]
    public AudioClip music;

    [Header("UI")]
    public AudioClip click;     // клик по кнопке
    public AudioClip uiOpen;    // открыть панель
    public AudioClip uiClose;   // закрыть панель

    [Header("Готовка / гость")]
    public AudioClip pour;      // налив ингредиента
    public AudioClip ding;      // подача напитка
    public AudioClip perfect;   // «Идеально» / 3
    public AudioClip star;      // звезда / топпинг
    public AudioClip customerIn;// приход гостя

    [Header("Экономика / итоги")]
    public AudioClip coin;      // монета
    public AudioClip combo;     // комбо
    public AudioClip bonus;     // бонус/награда
    public AudioClip correct;   // верный заказ
    public AudioClip wrong;     // неверный заказ
    public AudioClip dayClear;  // день завершён (успех)
    public AudioClip dayFail;   // рестарт дня

    [Header("Все клипы пакета (для быстрого перевыбора в инспекторе)")]
    public AudioClip[] all;
}
