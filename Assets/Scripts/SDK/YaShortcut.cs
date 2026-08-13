/// <summary>
/// C#-обёртка над кастомным jslib-мостом к Yandex SDK «Ярлык на рабочий стол»
/// (Assets/Plugins/YaShortcut.jslib → ysdk.shortcut). Плагин YG2 такого API не даёт.
///
/// ⚠️ Работает только в WebGL-сборке под Яндексом (где определён глобальный ysdk).
/// В редакторе/не-WebGL — безопасные заглушки. Все вызовы обёрнуты try/catch, поэтому
/// отсутствие ysdk не ломает игру (промпт просто не появится).
/// Требует проверки на реальной сборке Яндекса.
///
/// Сцена: Глобально. Зависимости: нет. SDK: Yandex Games (ysdk.shortcut).
/// </summary>
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public static class YaShortcut
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int  YaShortcut_Available();
    [DllImport("__Internal")] private static extern void YaShortcut_Prompt();

    /// <summary>Доступен ли API ярлыка (есть ysdk.shortcut).</summary>
    public static bool Available() { try { return YaShortcut_Available() == 1; } catch { return false; } }

    /// <summary>Показать системный промпт добавления ярлыка (если платформа разрешает).</summary>
    public static void Prompt() { try { YaShortcut_Prompt(); } catch { } }
#else
    public static bool Available() => false;
    public static void Prompt() =>
        Debug.Log("YaShortcut.Prompt(): доступно только в WebGL-сборке Яндекса.");
#endif
}
