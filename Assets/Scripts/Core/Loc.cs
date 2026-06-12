/// <summary>
/// Локализация через плагин Яндекс Игр (YG2, модуль Localization).
/// Язык берётся из YG2.lang (язык платформы Яндекса / выбор игрока).
/// Если модуль Localization не установлен — игра работает на русском.
/// Сцена: Глобально
/// Зависимости: YG2 (модуль Localization — опционально)
/// SDK: YG2
/// </summary>
#if Localization_yg
using YG;
#endif

public static class Loc
{
    /// <summary>Код языка от Яндекса ("ru", "en", "tr", ...). Без модуля — "ru".</summary>
    public static string Lang
    {
        get
        {
#if Localization_yg
            string l = YG2.lang;
            return string.IsNullOrEmpty(l) ? "ru" : l;
#else
            return "ru";
#endif
        }
    }

    /// <summary>Русскоязычные аудитории Яндекс Игр получают русский текст, остальные — английский.</summary>
    public static bool IsRu
    {
        get
        {
            switch (Lang)
            {
                case "ru":
                case "be":
                case "kk":
                case "uk":
                case "uz":
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>Выбор строки по текущему языку.</summary>
    public static string T(string ru, string en) => IsRu ? ru : en;
}
