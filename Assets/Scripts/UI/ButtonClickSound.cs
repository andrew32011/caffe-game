/// <summary>
/// Проигрывает звук клика на любой кнопке (через AudioController.PlayClick). Не трогает
/// transform — можно вешать на все кнопки, включая те, у кого свой пульс масштаба.
/// Билдер вешает в фабриках Btn/IconBtn. Сцена: UI. Зависимости: AudioController, UI.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => AudioController.Instance?.PlayClick());
    }
}
