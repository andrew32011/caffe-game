/// <summary>
/// Батч 16: УСТАРЕЛ. Раньше рисовал отдельную плоскую плашку кристаллов. Теперь валюты
/// показывает единый CurrencyWidget на готовых префабах Mini UI (Coin Count / Gem Count),
/// поэтому монеты и кристаллы в одном стиле. Класс сохранён как безопасная заглушка, чтобы
/// не ломать существующие вызовы Ensure()/Instance.Refresh() из GameManager.
/// Сцена: MainScene. Зависимости: нет. SDK: нет.
/// </summary>
using UnityEngine;

public class CurrencyHudUI : MonoBehaviour
{
    public static CurrencyHudUI Instance { get; private set; }

    // Оставлено для совместимости с билдером (.Ref("_gemIcon", ...)); визуально не используется.
    [SerializeField] private Sprite _gemIcon;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public static CurrencyHudUI Ensure() => Instance;

    /// <summary>Ничего не делает — число обновляет CurrencyWidget в своём Update.</summary>
    public void Refresh() { }
}
