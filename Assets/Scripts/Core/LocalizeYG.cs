/// <summary>
/// Локализация статической TMP-надписи через встроенную систему плагина Яндекса.
/// Хранит текст на языках (ru/en/tr) и меняет его вживую по событию YG2.onSwitchLang —
/// как плагиновый пример LanguageExample, но под TextMeshProUGUI и с русскоязычной группой.
/// Вешается билдером на статические подписи (кнопки, заголовки). Динамический текст
/// (диалоги, суммы) по-прежнему идёт через Loc.T — там язык тоже берётся из YG2.lang.
/// Сцена: MainScene (UI). Зависимости: TMPro, YG2 (модуль Localization). SDK: YG2.
/// </summary>
using UnityEngine;
using TMPro;
#if Localization_yg
using YG;
#endif

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizeYG : MonoBehaviour
{
    [TextArea] public string ru;
    [TextArea] public string en;
    [TextArea] public string tr; // необязательно; пусто → используется en

    private TextMeshProUGUI _text;

    private void Awake()
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
#if Localization_yg
        YG2.onSwitchLang += Apply;
        Apply(YG2.lang);
#else
        Apply("ru");
#endif
    }

    private void OnDisable()
    {
#if Localization_yg
        YG2.onSwitchLang -= Apply;
#endif
    }

    /// <summary>Применяет текст под код языка Яндекса ("ru","en","tr",...).</summary>
    public void Apply(string lang)
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
        if (_text == null) return;

        switch (lang)
        {
            case "ru": case "be": case "kk": case "uk": case "uz":
                _text.text = ru;
                break;
            case "tr":
                _text.text = string.IsNullOrEmpty(tr) ? en : tr;
                break;
            default:
                _text.text = en;
                break;
        }
    }

    /// <summary>Задать двуязычный текст из билдера и сразу применить текущий язык.</summary>
    public void Set(string ruText, string enText)
    {
        ru = ruText;
        en = enText;
#if Localization_yg
        Apply(YG2.lang);
#else
        Apply("ru");
#endif
    }
}
