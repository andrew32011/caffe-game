/// <summary>
/// Выбор языка прямо в игре (кнопка в настройках). Циклирует по языкам, поддержанным
/// плагином Яндекса и шрифтом. Вызывает YG2.SwitchLanguage — это меняет YG2.lang и дёргает
/// onSwitchLang (обновляет все LocalizeYG вживую) и сохраняет выбор. Даёт игроку ручной
/// выбор языка и служит проверкой пайплайна локализации.
/// Сцена: MainScene (UI). Зависимости: TMPro, YG2 (модуль Localization). SDK: YG2.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if Localization_yg
using YG;
#endif

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _label;

    // Коды языков (список плагина) и их самоназвания для кнопки.
    private static readonly string[] Codes =
        { "ru","en","uk","be","kk","tr","az","de","fr","es","pt","it","ro","id","et","lv","lt","ky","tg","tk","uz" };
    private static readonly string[] Names =
        { "Русский","English","Українська","Беларуская","Қазақша","Türkçe","Azərbaycan","Deutsch","Français","Español",
          "Português","Italiano","Română","Bahasa Indonesia","Eesti","Latviešu","Lietuvių","Кыргызча","Тоҷикӣ","Türkmençe","O‘zbek" };

    private void Awake()
    {
        if (_button != null) _button.onClick.AddListener(Next);
    }

    private void OnEnable()
    {
#if Localization_yg
        YG2.onSwitchLang += Refresh;
        Refresh(YG2.lang);
#else
        Refresh("ru");
#endif
    }

    private void OnDisable()
    {
#if Localization_yg
        YG2.onSwitchLang -= Refresh;
#endif
    }

    private void Refresh(string lang)
    {
        int i = System.Array.IndexOf(Codes, lang);
        if (i < 0) i = 1; // неизвестный → показываем English
        if (_label != null) _label.text = Names[i];
    }

    private void Next()
    {
#if Localization_yg
        int i = System.Array.IndexOf(Codes, YG2.lang);
        if (i < 0) i = 0;
        i = (i + 1) % Codes.Length;
        YG2.SwitchLanguage(Codes[i]); // меняет язык, дёргает onSwitchLang, сохраняет
#endif
    }
}
