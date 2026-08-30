/// <summary>
/// Батч 13: глобальная тема оформления UI. Хранит выбранный 9-slice спрайт панелей и
/// применяет его ко всем активным ThemedPanel; неактивные панели подхватят тему на своём
/// OnEnable. Спрайт-варианты грузит билдер (папка «Dark Theme RoundEdge Panels»), выбор
/// делает CustomizationUI, значение персистится в сейве (themeId).
/// Сцена: глобально (static). Зависимости: UnityEngine.UI. SDK: нет.
/// </summary>
using UnityEngine;

public static class UiTheme
{
    /// <summary>Текущий спрайт панели (null — тема по умолчанию, что задал билдер).</summary>
    public static Sprite PanelSprite { get; private set; }

    /// <summary>Задать тему и применить ко всем активным панелям сцены.</summary>
    public static void SetPanelSprite(Sprite sprite)
    {
        PanelSprite = sprite;
        foreach (var tp in Object.FindObjectsOfType<ThemedPanel>())
            tp.ApplyTheme();
    }
}
