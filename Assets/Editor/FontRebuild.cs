// <summary>
// Достраивает SDF-шрифт UI недостающими символами (русские буквы Ё Г Ж З К Х Ъ Э Я ё ъ э,
// кавычки «», тире — и т.п.), беря их из ИСХОДНОГО шрифта Nunito — поэтому стиль совпадает
// с остальными буквами (это тот же шрифт).
//
// Почему так: у ассета 'ofont.ru_Nunito SDF' нет исходного .ttf в проекте, поэтому в атлас
// попала только часть глифов. Нарисовать новые буквы «в стиле» нельзя — нужен сам шрифт Nunito.
//
// КАК ПОЛЬЗОВАТЬСЯ:
//   1. Положи файл шрифта Nunito с кириллицей в папку Assets (например, Nunito-Regular.ttf).
//      Скачать: Google Fonts → Nunito, или тот же ofont.ru. Имя файла должно содержать "nunito".
//   2. Меню: Tools → CoffeGame → Rebuild Font (Full Cyrillic).
//   3. Готово — недостающие буквы появятся во всех UI-текстах в едином стиле.
// </summary>
#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEditor;
using TMPro;

public static class FontRebuild
{
    const string SdfPath = "Assets/ofont.ru_Nunito SDF.asset";

    [MenuItem("Tools/CoffeGame/Rebuild Font (Full Cyrillic)")]
    public static void Rebuild()
    {
        var sdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
        if (sdf == null) { Debug.LogError("FontRebuild: не найден " + SdfPath); return; }

        Font ttf = FindSourceFont();
        if (ttf == null)
        {
            Debug.LogError("FontRebuild: исходный шрифт Nunito (.ttf/.otf с кириллицей) не найден в проекте.\n" +
                           "Положи файл Nunito (имя должно содержать \"nunito\", например Nunito-Regular.ttf) в папку Assets и запусти меню снова.");
            return;
        }

        // Привязываем исходный шрифт и включаем динамический атлас, чтобы можно было
        // допечатать недостающие глифы из того же шрифта.
        var so = new SerializedObject(sdf);
        SetRef(so, "m_SourceFontFile", ttf);
        SetRef(so, "m_SourceFontFile_EditorRef", ttf);
        var pMode = so.FindProperty("m_AtlasPopulationMode");
        if (pMode != null) pMode.enumValueIndex = 1; // Dynamic
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sdf);
        AssetDatabase.SaveAssets();

        // Перезагружаем, чтобы исходный шрифт точно привязался, и обновляем таблицы.
        sdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
        sdf.ReadFontAssetDefinition();

        string charset = BuildCharset();
        bool ok = sdf.TryAddCharacters(charset, out string missing);

        sdf.ReadFontAssetDefinition();
        EditorUtility.SetDirty(sdf);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!ok && !string.IsNullOrEmpty(missing))
            Debug.LogWarning("FontRebuild: эти символы отсутствуют даже в исходном шрифте и не добавлены: " + missing +
                             "\nВозьми полную версию Nunito (с кириллицей).");
        else
            Debug.Log("FontRebuild: готово. Недостающие символы добавлены в '" + sdf.name +
                      "' из '" + ttf.name + "' — стиль совпадает.");
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
    }

    // Ищем в проекте шрифт Nunito (по имени файла).
    static Font FindSourceFont()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Font"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string n = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
            if (n.Contains("nunito"))
            {
                var f = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (f != null) return f;
            }
        }
        return null;
    }

    // Полный нужный набор: латиница/цифры/знаки + вся русская азбука + типографика.
    static string BuildCharset()
    {
        var sb = new StringBuilder();
        for (char c = ' '; c <= '~'; c++) sb.Append(c);                 // ASCII
        sb.Append('Ё');                                            // Ё
        for (int cp = 0x0410; cp <= 0x044F; cp++) sb.Append((char)cp);  // А..я
        sb.Append('ё');                                            // ё
        sb.Append("«»—–…„“”‘’№€");                                       // кавычки, тире и пр.
        return sb.ToString();
    }
}
#endif
