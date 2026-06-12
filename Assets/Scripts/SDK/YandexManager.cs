/// <summary>
/// Обёртка над YG2 SDK. Централизует все вызовы к Яндекс Играм.
/// ВАЖНО: Модули InterstitialAdv и RewardedAdv нужно установить вручную через
///         меню YG2 → Modules в редакторе Unity, иначе #if-блоки не скомпилируются.
///
/// Как установить модули рекламы:
///  1. Window → Plugin YG2 → Modules
///  2. Установить RewardedAdv v1.011
///  3. Установить InterstitialAdv v1.01
///
/// Сцена: MainScene
/// Зависимости: PluginYG2 (уже в проекте)
/// SDK: YG2 полностью
/// </summary>
using System;
using UnityEngine;
using YG;

public class YandexManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    public static YandexManager Instance { get; private set; }

    // ─── События для игровых систем ───────────────────────────────────────────

    /// <summary>Запускается при получении награды за рекламу. Параметр — id.</summary>
    public static event Action<string> OnRewardReceived;

    /// <summary>Запускается при закрытии любой рекламы.</summary>
    public static event Action OnAnyAdClosed;

    // ─── Состояние ────────────────────────────────────────────────────────────

    private bool _sdkReady = false;

    // ─── Инициализация ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        YG2.onGetSDKData       += OnSDKDataReceived;
        YG2.onFocusWindowGame  += OnWindowFocus;
        YG2.onCloseAnyAdv      += OnAnyAdvClosed;

#if RewardedAdv_yg
        YG2.onRewardAdv += OnRewardedAdvReceived;
#endif
    }

    private void UnsubscribeFromEvents()
    {
        YG2.onGetSDKData       -= OnSDKDataReceived;
        YG2.onFocusWindowGame  -= OnWindowFocus;
        YG2.onCloseAnyAdv      -= OnAnyAdvClosed;

#if RewardedAdv_yg
        YG2.onRewardAdv -= OnRewardedAdvReceived;
#endif
    }

    // ─── Публичное API ────────────────────────────────────────────────────────

    /// <summary>Уведомляем Яндекс, что геймплей начался (обязательно).</summary>
    public void NotifyGameplayStart() => YG2.GameplayStart();

    /// <summary>Уведомляем Яндекс, что геймплей остановился (обязательно).</summary>
    public void NotifyGameplayStop()  => YG2.GameplayStop();

    /// <summary>Показывает межстраничную рекламу между уровнями.</summary>
    public void ShowInterstitialAd()
    {
#if InterstitialAdv_yg
        if (!YG2.nowAdsShow)
            YG2.InterstitialAdvShow();
#else
        Debug.Log("YandexManager: модуль InterstitialAdv не установлен. " +
                  "Установите через Window → Plugin YG2 → Modules.");
#endif
    }

    /// <summary>Показывает рекламу с вознаграждением.</summary>
    /// <param name="rewardId">Идентификатор награды (например, "hint_reward").</param>
    public void ShowRewardedAd(string rewardId)
    {
#if RewardedAdv_yg
        if (!YG2.nowAdsShow)
            YG2.RewardedAdvShow(rewardId);
#else
        Debug.Log("YandexManager: модуль RewardedAdv не установлен. " +
                  "Установите через Window → Plugin YG2 → Modules.");
#endif
    }

    /// <summary>True если сейчас показывается реклама.</summary>
    public bool IsAdShowing => YG2.nowAdsShow;

    /// <summary>True если SDK инициализирован и готов.</summary>
    public bool IsSDKReady  => _sdkReady;

    // ─── Обработчики событий ─────────────────────────────────────────────────

    private void OnSDKDataReceived()
    {
        _sdkReady = true;
        Debug.Log("YandexManager: SDK готов.");
    }

    private void OnWindowFocus(bool visible)
    {
        // AudioController подписывается сам через GameManager
        // Здесь — дополнительная логика при необходимости
    }

    private void OnAnyAdvClosed()
    {
        OnAnyAdClosed?.Invoke();
    }

#if RewardedAdv_yg
    private void OnRewardedAdvReceived(string id)
    {
        OnRewardReceived?.Invoke(id);
    }
#endif

    // ─── Диагностика ─────────────────────────────────────────────────────────

    [ContextMenu("DEBUG: Show Interstitial Ad")]
    public void DebugShowInterstitial() => ShowInterstitialAd();

    [ContextMenu("DEBUG: Show Rewarded Ad (test_reward)")]
    public void DebugShowRewarded() => ShowRewardedAd("test_reward");
}
