/// <summary>
/// Локализация. Язык берём из СЫРОГО окружения платформы Яндекса — YG2.envir.language
/// (модуль EnvirData, = ysdk.environment.i18n.lang). Заполняется на InitYG_0 (раньше модуля
/// Localization), не зависит от режима setLanguageMod, сохранённого ключа и порядка init —
/// поэтому определяется корректно при каждом старте по языку платформы. Запасной путь —
/// YG2.lang, затем "ru". В редакторе = InfoYG Simulation language.
/// Сцена: Глобально. Зависимости: YG2 (EnvirData / Localization). SDK: YG2.
/// </summary>
#if EnvirData_yg || Localization_yg
using YG;
#endif

public static class Loc
{
    /// <summary>Код языка ("ru","en","tr",...), нормализованный до 2 букв.</summary>
    public static string Lang
    {
        get
        {
            string l = null;
#if EnvirData_yg
            l = YG2.envir.language;      // сырой язык платформы — приоритет
#endif
#if Localization_yg
            if (string.IsNullOrEmpty(l)) l = YG2.lang;
#endif
            if (string.IsNullOrEmpty(l)) return "ru";

            l = l.ToLowerInvariant();
            if (l.Length > 2) l = l.Substring(0, 2); // "en-US" → "en"
            if (l == "us" || l == "as" || l == "ai") l = "en";
            return l;
        }
    }

    /// <summary>Русскоязычные аудитории Яндекса получают русский текст, остальные — английский.</summary>
    public static bool IsRu
    {
        get
        {
            switch (Lang)
            {
                case "ru": case "be": case "kk": case "uk": case "uz": return true;
                default: return false;
            }
        }
    }

    /// <summary>Выбор строки по текущему языку.</summary>
    public static string T(string ru, string en) => IsRu ? ru : en;
}
