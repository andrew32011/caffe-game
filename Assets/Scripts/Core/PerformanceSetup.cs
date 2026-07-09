/// <summary>
/// Перф: применяет облегчённые настройки качества на старте — под слабые устройства и WebGL.
/// Ставится билдером на объект в GAME SYSTEMS. Все параметры — в инспекторе, легко откатить.
/// Сцена: MainScene (и подходит для любой). Зависимости: нет. SDK: нет.
/// </summary>
using UnityEngine;

public class PerformanceSetup : MonoBehaviour
{
    [Header("Кадр")]
    [Tooltip("Верхний предел FPS. На WebGL сглаживает нагрузку слабых устройств.")]
    [SerializeField] private int _targetFrameRate = 60;

    [Header("Качество (чем меньше — тем легче)")]
    [SerializeField] private bool _disableShadows   = true;  // тени — самый дорогой пункт
    [SerializeField] private bool _disableAntiAlias  = true;  // MSAA дорогой на слабом GPU
    [SerializeField] private int  _pixelLightCount   = 1;     // сколько пиксельных источников света
    [SerializeField] private bool _disableVSync      = true;  // не ограничиваемся vSync, рулим targetFrameRate

    private void Awake()
    {
        Application.targetFrameRate = _targetFrameRate;

        if (_disableVSync)      QualitySettings.vSyncCount = 0;
        if (_disableShadows)  { QualitySettings.shadows = ShadowQuality.Disable; QualitySettings.shadowDistance = 0f; }
        if (_disableAntiAlias)  QualitySettings.antiAliasing = 0;

        QualitySettings.pixelLightCount           = Mathf.Max(0, _pixelLightCount);
        QualitySettings.softParticles             = false;
        QualitySettings.realtimeReflectionProbes  = false;
        QualitySettings.billboardsFaceCameraPosition = false;
        QualitySettings.skinWeights               = SkinWeights.TwoBones;
    }
}
