/// <summary>
/// Батч 16: УСТАРЕЛ. Плашка-виджет «Копим на: X» заменена магазином-обустройством камерой
/// (RenovationShopUI): справа на HUD кнопка «Магазин» с «!»-значком, облёт точек кофейни,
/// стрелки, мигающий предмет и покупка на месте. Класс оставлен как безопасная заглушка,
/// чтобы не ломать возможные вызовы Ensure().
/// Сцена: MainScene. Зависимости: RenovationShopUI. SDK: нет.
/// </summary>
using UnityEngine;

public class RenovationGoalUI : MonoBehaviour
{
    public static RenovationGoalUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Совместимость: направляем на новый магазин-обустройство.</summary>
    public static RenovationShopUI Ensure() => RenovationShopUI.Ensure();

    public void OpenPanel() => RenovationShopUI.Ensure().Open();
}
