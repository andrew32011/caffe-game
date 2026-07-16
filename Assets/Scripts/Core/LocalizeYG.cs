/// <summary>
/// Локализация статической TMP-надписи. Язык берёт из Loc.Lang (сырой язык платформы
/// YG2.envir.language). Применяет текст: сразу при включении, ПОВТОРНО после готовности SDK
/// (иначе на старте язык ещё не определён), и на событие смены языка. Вешается билдером на
/// статические подписи (кнопки, заголовки). Динамический текст идёт через Loc.T.
/// Сцена: MainScene (UI). Зависимости: TMPro, UiTranslations, Loc, YG2. SDK: YG2.
/// </summary>
using System.Collections;
using UnityEngine;
using TMPro;
#if EnvirData_yg || Localization_yg
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
        Apply();                              // сразу (может быть дефолт, если SDK ещё не готов)
        StartCoroutine(ApplyWhenReady());     // повтор после готовности SDK — язык уже определён
#if Localization_yg
        YG2.onSwitchLang += OnSwitch;         // на случай смены языка в рантайме
#endif
    }

    private void OnDisable()
    {
#if Localization_yg
        YG2.onSwitchLang -= OnSwitch;
#endif
    }

    private void OnSwitch(string _) => Apply();

    private IEnumerator ApplyWhenReady()
    {
#if EnvirData_yg || Localization_yg
        float t = 0f;
        while (!YG2.isSDKEnabled && t < 6f) { t += Time.unscaledDeltaTime; yield return null; }
#else
        yield return null;
#endif
        Apply();
    }

    /// <summary>Применяет текст под текущий язык (Loc.Lang).</summary>
    public void Apply()
    {
        if (_text == null) _text = GetComponent<TextMeshProUGUI>();
        if (_text == null) return;

        string lang = Loc.Lang;

        // 1) Централизованная таблица UI-переводов (все языки) по ключу = русский текст.
        if (!string.IsNullOrEmpty(ru) && UiTranslations.TryGet(ru, lang, out string t))
        {
            _text.text = t;
            return;
        }

        // 2) Иначе — встроенные ru/en/tr (для текста вне таблицы, напр. названия-бренда).
        if (lang == "tr" && !string.IsNullOrEmpty(tr)) { _text.text = tr; return; }
        _text.text = Loc.IsRu ? ru : en;
    }

    /// <summary>Задать двуязычный текст из билдера и сразу применить.</summary>
    public void Set(string ruText, string enText)
    {
        ru = ruText;
        en = enText;
        Apply();
    }
}
