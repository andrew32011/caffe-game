/// <summary>
/// Батч 13: маркер «панель поддерживает смену темы». Билдер вешает его на каждую панель,
/// которой задаёт спрайт (ApplyPanelSprite). На OnEnable и по команде UiTheme применяет
/// текущий тематический 9-slice спрайт — так каждая панель перекрашивается под выбранную
/// тему при открытии, без хранения глобального списка ссылок.
/// Сцена: MainScene (UI). Зависимости: UnityEngine.UI, UiTheme. SDK: нет.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ThemedPanel : MonoBehaviour
{
    private Image _img;

    private void Awake() { if (_img == null) _img = GetComponent<Image>(); }
    private void OnEnable() { ApplyTheme(); }

    public void ApplyTheme()
    {
        if (_img == null) _img = GetComponent<Image>();
        if (_img == null || UiTheme.PanelSprite == null) return;
        _img.sprite = UiTheme.PanelSprite;
        _img.type   = Image.Type.Sliced;
        _img.color  = Color.white;
    }
}
