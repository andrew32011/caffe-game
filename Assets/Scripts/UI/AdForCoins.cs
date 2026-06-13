/// <summary>
/// Экран «не хватает денег»: кнопка смотрит рекламу за монеты (пункт 5).
/// Если модуль RewardedAdv установлен — показываем рекламу и выдаём награду по
/// событию; иначе (в редакторе / без модуля) выдаём монеты сразу.
/// Сцена: MainScene (UI)
/// Зависимости: GameManager; YG2 (опц. модуль RewardedAdv)
/// SDK: YG2
/// </summary>
using UnityEngine;
using YG;

public class AdForCoins : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private int _reward = 60;

#if RewardedAdv_yg
    private void OnEnable()  { YG2.onRewardAdv += OnReward; }
    private void OnDisable() { YG2.onRewardAdv -= OnReward; }
    private void OnReward(string id) { if (id == "coins") Grant(); }
#endif

    /// <summary>Вешается на кнопку «Смотреть рекламу».</summary>
    public void WatchAd()
    {
#if RewardedAdv_yg
        YG2.RewardedAdvShow("coins");
#else
        Grant(); // без модуля рекламы — просто выдаём монеты (заглушка для теста)
#endif
    }

    private void Grant()
    {
        GameManager.Instance?.AddCoins(_reward);
        if (_panel != null) _panel.SetActive(false);
    }

    /// <summary>Закрыть без награды.</summary>
    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);
    }
}
